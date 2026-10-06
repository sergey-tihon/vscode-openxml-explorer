#r "nuget: Fun.Build, 1.2.0"

open System
open System.Diagnostics
open System.IO
open System.Runtime.CompilerServices
open Fun.Build

let repoRoot = __SOURCE_DIRECTORY__

let releaseHeading =
    File.ReadLines(Path.Combine(repoRoot, "RELEASE_NOTES.md"))
    |> Seq.tryFind(fun line -> line.StartsWith("### ", StringComparison.Ordinal))
    |> Option.defaultWith(fun () -> failwith "RELEASE_NOTES.md does not contain a release heading")

let releaseVersion =
    let versionText =
        releaseHeading.Substring(4).Split(' ', StringSplitOptions.RemoveEmptyEntries)
        |> Array.tryHead
        |> Option.defaultWith(fun () -> failwithf "Invalid release heading: %s" releaseHeading)

    Version.Parse versionText

let cleanDirectory path =
    if Directory.Exists path then
        Directory.Delete(path, true)

    Directory.CreateDirectory(path) |> ignore

let shellCommand command =
    if OperatingSystem.IsWindows() then
        "cmd /c " + command
    else
        command


let clean =
    stage "Clean" {
        run (fun _ ->
            cleanDirectory(Path.Combine(repoRoot, "temp"))
            cleanDirectory(Path.Combine(repoRoot, "out"))
            Directory.CreateDirectory(Path.Combine(repoRoot, "release")) |> ignore
            File.Copy(Path.Combine(repoRoot, "README.md"), Path.Combine(repoRoot, "release/README.md"), true)
            File.Copy(Path.Combine(repoRoot, "LICENSE.md"), Path.Combine(repoRoot, "release/LICENSE.md"), true)
            File.Copy(Path.Combine(repoRoot, "RELEASE_NOTES.md"), Path.Combine(repoRoot, "release/CHANGELOG.md"), true)

            let readmePath = Path.Combine(repoRoot, "release/README.md")

            File.ReadAllLines(readmePath)
            |> Array.filter(fun line -> line.Contains(".svg") |> not)
            |> fun lines -> File.WriteAllLines(readmePath, lines))
    }

let yarnInstall =
    stage "YarnInstall" {
        run (shellCommand "yarn install")
    }

let checkFormat =
    stage "CheckFormat" {
        run "dotnet fantomas src tests --check"
        echo "No files need formatting"
    }


let runScript =
    stage "RunScript" {
        run "dotnet fable ./src/extension/Extension.fsproj --outDir ./out --run webpack --mode=production"
    }

let watch =
    stage "Watch" {
        run "dotnet fable watch ./src/extension/Extension.fsproj --outDir ./out --define DEBUG --runWatch webpack --mode=development"
    }

let buildServer =
    stage "BuildServer" {
        run "dotnet publish src/Server/Server.fsproj -c Release -o release/bin"
    }

let runTests =
    stage "RunTests" {
        run "dotnet test"
    }

let buildProject =
    stage "BuildProject" {
        clean
        yarnInstall
        checkFormat
        runScript
        buildServer
    }

let setPackageJsonField name value releaseDirectory =
    let packageJson = Path.Combine(repoRoot, releaseDirectory, "package.json")
    let prefix = sprintf "\"%s\":" name
    let mutable found = false

    let updatedLines =
        File.ReadAllLines(packageJson)
        |> Array.map(fun line ->
            if line.TrimStart().StartsWith(prefix, StringComparison.Ordinal) then
                found <- true
                let indentLength = line.Length - line.TrimStart().Length
                sprintf "%s\"%s\": %s," (line.Substring(0, indentLength)) name value
            else
                line)

    if not found then
        failwithf "Could not find package.json field %s in %s" name packageJson

    File.WriteAllLines(packageJson, updatedLines)

let setVersion =
    stage "SetVersion" {
        run (fun _ -> setPackageJsonField "version" (sprintf "\"%O\"" releaseVersion) "release")
    }

let killAllByName processName =
    for proc in Process.GetProcessesByName(processName) do
        use proc = proc

        try
            if not proc.HasExited then
                proc.Kill(true)
        with _ -> ()

let buildPackage =
    stage "BuildPackage" {
        run (fun _ -> killAllByName "vsce")

        stage "Package extension" {
            workingDir (Path.Combine(repoRoot, "release"))
            run (shellCommand "vsce package")
        }

        run (fun _ ->
            let tempDirectory = Path.Combine(repoRoot, "temp")
            Directory.CreateDirectory(tempDirectory) |> ignore

            Directory.GetFiles(Path.Combine(repoRoot, "release"), "*.vsix")
            |> Array.iter(fun packagePath ->
                let destination = Path.Combine(tempDirectory, Path.GetFileName(packagePath))
                File.Move(packagePath, destination, true)))
    }

let readPassword () =
    if Console.IsInputRedirected then
        failwith "Set the vsce-token environment variable when no interactive terminal is available."

    Console.Write("VSCE Token: ")
    let token = ResizeArray<char>()
    let mutable reading = true

    while reading do
        let key = Console.ReadKey(true)

        match key.Key with
        | ConsoleKey.Enter -> reading <- false
        | ConsoleKey.Backspace when token.Count > 0 ->
            token.RemoveAt(token.Count - 1)
            Console.Write("\b \b")
        | _ when Char.IsControl(key.KeyChar) |> not ->
            token.Add(key.KeyChar)
            Console.Write('*')
        | _ -> ()

    Console.WriteLine()
    String(token.ToArray())

let publishToGallery =
    stage "PublishToGallery" {
        workingDir (Path.Combine(repoRoot, "release"))
        run (fun context -> async {
            let token = Environment.GetEnvironmentVariable("vsce-token")

            let token =
                if String.IsNullOrWhiteSpace token then
                    readPassword ()
                else
                    token

            let commandFormat =
                if OperatingSystem.IsWindows() then
                    "cmd /c vsce publish --pat {0}"
                else
                    "vsce publish --pat {0}"

            let command = FormattableStringFactory.Create(commandFormat, [| box token |])
            return! context.RunSensitiveCommand command
        })
    }


pipeline "Default" {
    workingDir repoRoot

    buildProject

    runIfOnlySpecified false
}

pipeline "Build" {
    workingDir repoRoot

    buildProject

    runTests
    runIfOnlySpecified
}

pipeline "Release" {
    workingDir repoRoot

    buildProject

    runTests
    setVersion
    buildPackage
    publishToGallery
    runIfOnlySpecified
}

pipeline "Watch" {
    workingDir repoRoot
    watch
    runIfOnlySpecified
}


tryPrintPipelineCommandHelp ()
