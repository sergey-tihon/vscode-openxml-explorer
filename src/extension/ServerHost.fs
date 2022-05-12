module OpenXmlExplorer.ServerHost

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.PortFinder
open Node
open Node.ChildProcess

let private toStr =
    function
    | U2.Case2(x: Buffer.Buffer) -> x.toString Buffer.BufferEncoding.Utf8
    | U2.Case1(x: string) -> x

let startServer port extensionPath =
    let cb (e: ExecError option) stdout' stderr' =
        //stdout' |> toStr |> fun s -> if s <> "" then Log.line s
        stderr' |> toStr |> (fun s -> if s <> "" then Log.line s)

        if e.IsSome then
            Log.line($"ExecError: %s{e.Value.ToString()}")
            Log.show()

    let host = $"http://0.0.0.0:%d{port}"
    let opts = createEmpty<ExecOptions>
    opts.cwd <- Some(extensionPath + "/bin")
    childProcess.exec($"dotnet Server.dll %s{host}", opts, cb) |> ignore
    Log.line $"API server started at %s{host}"

    Remoting.getApiClient host

let getFreePort() =
    let opts = createEmpty<PortFinderOptions>
    opts.port <- Some(20489)
    Globals.portfinder.getPortPromise(opts) |> Async.AwaitPromise
