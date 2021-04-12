module Remoting

open Fable.Core
open Fable.Import.Axios
open Fable.Import.Axios.Globals
open Shared

let inline private toAsync<'a> (resp: JS.Promise<AxiosXHR<'a>>) =
    resp
    |> Promise.map (fun x -> x.data)
    |> Async.AwaitPromise

let private getApiClient (serverHost) : IOpenXmlApi =
    let typeName = nameof(IOpenXmlApi)
    let getRoute methodName =
        serverHost + Route.builder typeName methodName

    {
        getPackageInfo = fun filePath ->
            let data = [filePath]
            axios.post<Document>(getRoute "getPackageInfo", data)
            |> toAsync

        getPartContent = fun filePath partUri ->
            let data = [filePath; partUri]
            axios.post<string>(getRoute "getPartContent", data)
            |> toAsync
            
        stopApplication = fun () ->
            axios.get(getRoute "stopApplication")
            |> toAsync
    }

open Node
open Node.ChildProcess
open Fable.Core.JsInterop
open Fable.Import

let private toStr = function
  | U2.Case2(x:Buffer.Buffer) ->
    x.toString Buffer.BufferEncoding.Utf8
  | U2.Case1(x:string) -> x

let startServer port extensionPath =
    let cb (e:ExecError option) stdout' stderr' =
      let channel = vscode.window.createOutputChannel "openxml"

      if e.IsSome then
          channel.appendLine($"ExecError: %s{e.Value.ToString()}")
      channel.appendLine($"Err: %s{stderr' |> toStr}")
      channel.appendLine($"Out: %s{stdout' |> toStr}")
      channel.show()

    let host = $"http://0.0.0.0:%d{port}"
    let opts = createEmpty<ExecOptions>
    opts.cwd <- Some (extensionPath + "/bin")
    childProcess.exec ($"dotnet Server.dll %s{host}", opts, cb) |> ignore

    getApiClient host
