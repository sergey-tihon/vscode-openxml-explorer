#r @"paket:
source https://api.nuget.org/v3/index.json
framework:netstandard2.0
nuget FSharp.Core 4.7.2
nuget Fake.Core.Target
nuget Fake.Core.Process
nuget Fake.Core.ReleaseNotes
nuget Fake.Core.Environment
nuget Fake.Core.UserInput
nuget Fake.DotNet.Cli
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.Paket
nuget Fake.DotNet.MsBuild
nuget Fake.IO.FileSystem
nuget Fake.IO.Zip
nuget Fake.Api.GitHub
nuget Fake.Tools.Git
nuget Fake.JavaScript.Yarn //"

#if !FAKE
#load "./.fake/build.fsx/intellisense.fsx"
#r "netstandard" // Temp fix for https://github.com/fsharp/FAKE/issues/1985
#endif

open Fake.Core
open Fake.JavaScript
open Fake.DotNet
open Fake.IO
open Fake.Core.TargetOperators

// --------------------------------------------------------------------------------------
// Build the Generator project and run it
// --------------------------------------------------------------------------------------

Target.create "Clean" (fun _ ->
    Shell.copy "release" ["README.md"; "LICENSE.md"]
    Shell.copyFile "release/CHANGELOG.md" "RELEASE_NOTES.md"
)

Target.create "YarnInstall" (fun _ ->
    Yarn.install id
)

module Fable =
    type Command =
        | Build
        | Watch
        | Clean
    type Webpack =
        | WithoutWebpack
        | WithWebpack of args: string option
    type Args = {
        Command: Command
        Debug: bool
        ProjectPath: string
        OutDir: string option
        Defines: string list
        AdditionalFableArgs: string option
        Webpack: Webpack
    }

    let DefaultArgs = {
        Command = Build
        Debug = false
        ProjectPath = "./src/extension/Extension.fsproj"
        OutDir = Some "./out"
        Defines = []
        AdditionalFableArgs = None
        Webpack = WithoutWebpack
    }

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
        let fableDefines = args.Defines |> List.map (sprintf "--define %s") |> String.concat " "
        let fableAdditionalArgs = args.AdditionalFableArgs |> Option.defaultValue ""
        let webpackCmd =
            match args.Webpack with
            | WithoutWebpack -> ""
            | WithWebpack webpackArgs ->
                sprintf "--%s webpack %s %s"
                    (match args.Command with | Watch -> "runWatch" | _ -> "run")
                    (if args.Debug then "--mode=development" else "--mode=production")
                    (webpackArgs |> Option.defaultValue "")

        // $"{fableCmd} {fableProjPath} {fableOutDir} {fableDebug} {fableDefines} {fableAdditionalArgs} {webpackCmd}"
        sprintf "%s %s %s %s %s %s %s" fableCmd fableProjPath fableOutDir fableDebug fableDefines fableAdditionalArgs webpackCmd

    let run args =
        let cmd = mkArgs args
        let result = DotNet.exec id "fable" cmd
        if not result.OK then
            failwithf "Error while running 'dotnet fable' with args: %s" cmd

Target.create "RunScript" (fun _ ->
    Fable.run { Fable.DefaultArgs with Command = Fable.Build; Debug = false; Webpack = Fable.WithWebpack None }
)

Target.create "Watch" (fun _ ->
    Fable.run { Fable.DefaultArgs with Command = Fable.Watch; Debug = true; Webpack = Fable.WithWebpack None }
)


Target.create "BuildServer" (fun _ ->
    DotNet.exec id "publish" "src/Server/Server.fsproj -c Release -o release/bin" |> ignore
)


// --------------------------------------------------------------------------------------
// Run generator by default. Invoke 'build <Target>' to override
// --------------------------------------------------------------------------------------

Target.create "Default" ignore

"YarnInstall" ?=> "RunScript"

"Clean"
==> "RunScript"
==> "BuildServer"
==> "Default"

Target.runOrDefault "Default"
