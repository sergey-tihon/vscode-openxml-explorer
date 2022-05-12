module OpenXmlExplorer.Remoting

open Fable.Core
open Fable.Import.Axios
open Fable.Import.Axios.Globals
open Shared

let inline private toAsync<'a>(resp: JS.Promise<AxiosXHR<'a>>) =
    resp |> Promise.map(fun x -> x.data) |> Async.AwaitPromise

let getApiClient(serverHost) : IOpenXmlApi =
    let typeName = nameof(IOpenXmlApi)

    let getRoute methodName =
        serverHost + Route.builder typeName methodName

    {
        getPackageInfo =
            fun filePath ->
                let data = [ filePath ]
                axios.post<Document>(getRoute "getPackageInfo", data) |> toAsync

        getPartContent =
            fun filePath partUri ->
                let data = [ filePath; partUri ]
                axios.post<string>(getRoute "getPartContent", data) |> toAsync

        checkHealth =
            fun () -> async {
                try
                    return! axios.get(getRoute "checkHealth") |> toAsync
                with
                | e -> return false
            }
        stopApplication =
            fun () ->
                Log.line $"Stopping API Server ..."
                axios.get(getRoute "stopApplication") |> toAsync
    }
