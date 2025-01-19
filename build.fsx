#r "nuget: Fake.Core.Target"
#r "nuget: Fake.Core.Process"
#r "nuget: Fake.Core.ReleaseNotes"
#r "nuget: Fake.Core.Environment"
#r "nuget: Fake.Core.UserInput"
#r "nuget: Fake.DotNet.Cli"
#r "nuget: Fake.DotNet.AssemblyInfoFile"
#r "nuget: Fake.DotNet.Paket"
#r "nuget: Fake.DotNet.MsBuild"
#r "nuget: Fake.IO.FileSystem"
#r "nuget: Fake.IO.Zip"
#r "nuget: Fake.Api.GitHub"
#r "nuget: Fake.Tools.Git"
#r "nuget: Fake.Javascript.Yarn"

// Boilerplate - https://github.com/fsprojects/FAKE/issues/2719#issuecomment-1470687052
System.Environment.GetCommandLineArgs()
|> Array.skip 2 // skip fsi.exe; build.fsx
|> Array.toList
|> Fake.Core.Context.FakeExecutionContext.Create false __SOURCE_FILE__
|> Fake.Core.Context.RuntimeContext.Fake
|> Fake.Core.Context.setExecutionContext

open Fake.Core
open Fake.Core.TargetOperators
open Fake.JavaScript
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open System.IO

Target.initEnvironment()

let releaseNotesData = File.ReadAllLines "RELEASE_NOTES.md" |> ReleaseNotes.parseAll

let release = List.head releaseNotesData

// --------------------------------------------------------------------------------------
// Build the Generator project and run it
// --------------------------------------------------------------------------------------

Target.create "Clean" (fun _ ->
    Shell.cleanDir "./temp"
    Shell.cleanDir "./out"
    Shell.copy "release" [ "README.md"; "LICENSE.md" ]
    Shell.copyFile "release/CHANGELOG.md" "RELEASE_NOTES.md"

    // Remove SVG files from the release
    // https://github.com/microsoft/vscode-vsce/issues/183
    let fileName = "release/README.md"

    let lines =
        File.ReadAllLines fileName
        |> Array.filter(fun s -> s.Contains(".svg") |> not)

    File.WriteAllLines(fileName, lines))

Target.create "YarnInstall" (fun _ -> Yarn.install id)

module Fable =
    type Command =
        | Build
        | Watch
        | Clean

    type Webpack =
        | WithoutWebpack
        | WithWebpack of args: string option

    type Args =
        { Command: Command
          Debug: bool
          ProjectPath: string
          OutDir: string option
          Defines: string list
          AdditionalFableArgs: string option
          Webpack: Webpack }

    let DefaultArgs =
        { Command = Build
          Debug = false
          ProjectPath = "./src/extension/Extension.fsproj"
          OutDir = Some "./out"
          Defines = []
          AdditionalFableArgs = None
          Webpack = WithoutWebpack }

    let private mkArgs args =
        let fableCmd =
            match args.Command with
            | Build -> ""
            | Watch -> "watch"
            | Clean -> "clean"

        let fableProjPath = args.ProjectPath
        let fableDebug = if args.Debug then "--define DEBUG" else ""

        let fableOutDir =
            match args.OutDir with
            | Some dir -> sprintf "--outDir %s" dir
            | None -> ""

        let fableDefines =
            args.Defines |> List.map(sprintf "--define %s") |> String.concat " "

        let fableAdditionalArgs = args.AdditionalFableArgs |> Option.defaultValue ""

        let webpackCmd =
            match args.Webpack with
            | WithoutWebpack -> ""
            | WithWebpack webpackArgs ->
                sprintf
                    "--%s webpack %s %s"
                    (match args.Command with
                     | Watch -> "runWatch"
                     | _ -> "run")
                    (if args.Debug then
                         "--mode=development"
                     else
                         "--mode=production")
                    (webpackArgs |> Option.defaultValue "")

        // $"{fableCmd} {fableProjPath} {fableOutDir} {fableDebug} {fableDefines} {fableAdditionalArgs} {webpackCmd}"
        sprintf "%s %s %s %s %s %s %s" fableCmd fableProjPath fableOutDir fableDebug fableDefines fableAdditionalArgs webpackCmd

    let run args =
        let cmd = mkArgs args
        let result = DotNet.exec id "fable" cmd

        if not result.OK then
            failwithf "Error while running 'dotnet fable' with args: %s" cmd

Target.create "RunScript" (fun _ ->
    Fable.run
        { Fable.DefaultArgs with
            Command = Fable.Build
            Debug = false
            Webpack = Fable.WithWebpack None })

Target.create "Watch" (fun _ ->
    Fable.run
        { Fable.DefaultArgs with
            Command = Fable.Watch
            Debug = true
            Webpack = Fable.WithWebpack None })


Target.create "BuildServer" (fun _ ->
    DotNet.exec id "publish" "src/Server/Server.fsproj -c Release -o release/bin"
    |> ignore)

// --------------------------------------------------------------------------------------
// Check code format & format code using Fantomas

let sourceFiles = !!"src/**/*.fs" ++ "tests/**/*.fs" -- "./**/*Assembly*.fs"

Target.create "CheckFormat" (fun _ ->
    let result =
        sourceFiles
        |> Seq.map(sprintf "\"%s\"")
        |> String.concat " "
        |> sprintf "%s --check"
        |> DotNet.exec id "fantomas"

    if result.ExitCode = 0 then
        Trace.log "No files need formatting"
    elif result.ExitCode = 99 then
        failwith "Some files need formatting, check output for more info"
    else
        Trace.logf "Errors while formatting: %A" result.Errors)

Target.create "Format" (fun _ ->
    let result =
        sourceFiles
        |> Seq.map(sprintf "\"%s\"")
        |> String.concat " "
        |> DotNet.exec id "fantomas"

    if not result.OK then
        printfn "Errors while formatting all files: %A" result.Messages)


// --------------------------------------------------------------------------------------
// Build and run test projects
// --------------------------------------------------------------------------------------

Target.create "RunTests" (fun _ ->
    let result = DotNet.exec id "test" ""

    if not result.OK then
        failwithf "Test failed: %A" result.Errors)

// --------------------------------------------------------------------------------------
// Packaging and release
// --------------------------------------------------------------------------------------

let run cmd args dir =
    let parms =
        { ExecParams.Empty with
            Program = cmd
            WorkingDir = dir
            CommandLine = args }

    if Process.shellExec parms <> 0 then
        failwithf "Error while running '%s' with args: %s" cmd args


let platformTool tool path =
    match Environment.isUnix with
    | true -> tool
    | _ ->
        match ProcessUtils.tryFindFileOnPath path with
        | None -> failwithf "can't find tool %s on PATH" tool
        | Some v -> v


let vsceTool = lazy (platformTool "vsce" "vsce.cmd")

let setPackageJsonField name value releaseDir =
    let fileName = sprintf "./%s/package.json" releaseDir

    let lines =
        File.ReadAllLines fileName
        |> Seq.map(fun line ->
            if line.TrimStart().StartsWith(sprintf "\"%s\":" name) then
                let indent = line.Substring(0, line.IndexOf("\""))
                sprintf "%s\"%s\": %s," indent name value
            else
                line)

    File.WriteAllLines(fileName, lines)

let setVersion (release: ReleaseNotes.ReleaseNotes) releaseDir =
    let versionString = sprintf "\"%O\"" release.NugetVersion
    setPackageJsonField "version" versionString releaseDir

let buildPackage dir =
    Process.killAllByName "vsce"
    run vsceTool.Value "package" dir
    !!(sprintf "%s/*.vsix" dir) |> Seq.iter(Shell.moveFile "./temp/")


Target.create "SetVersion" (fun _ -> setVersion release "release")

let publishToGallery releaseDir =
    let token =
        match Environment.environVarOrDefault "vsce-token" "" with
        | s when not(String.isNullOrWhiteSpace s) -> s
        | _ -> UserInput.getUserPassword "VSCE Token: "

    Process.killAllByName "vsce"
    run vsceTool.Value (sprintf "publish --pat %s" token) releaseDir

Target.create "BuildPackage" (fun _ -> buildPackage "release")

Target.create "PublishToGallery" (fun _ -> publishToGallery "release")

// Target.create "ReleaseGitHub" (fun _ ->
//     releaseGithub release
// )

// --------------------------------------------------------------------------------------
// Run generator by default. Invoke 'build <Target>' to override
// --------------------------------------------------------------------------------------

Target.create "Default" ignore
Target.create "Build" ignore
Target.create "Release" ignore

"Clean"
==> "YarnInstall"
==> "CheckFormat"
==> "RunScript"
==> "BuildServer"
==> "Default"
==> "RunTests"
==> "Build"

"Build"
==> "SetVersion"
==> "BuildPackage"
//==> "ReleaseGitHub"
==> "PublishToGallery"
==> "Release"

Target.runOrDefault "Default"
