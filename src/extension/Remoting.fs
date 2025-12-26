module OpenXmlExplorer.Remoting

open Fable.Core
open Fable.Import.Axios
open Fable.Import.Axios.Globals
open Shared

let inline private toAsync<'a>(resp: JS.Promise<AxiosXHR<'a>>) =
    resp |> Promise.map _.data |> Async.AwaitPromise

let getApiClient(serverHost) : IOpenXmlApi =
    let typeName = nameof IOpenXmlApi

    let getRoute methodName =
        serverHost + Route.builder typeName methodName

    { getPackageInfo =
        fun filePath ->
            let data = [ filePath ]
            axios.post<Document>(getRoute "getPackageInfo", data) |> toAsync

      getPartContent =
        fun filePath partUri ->
            let data = [ filePath; partUri ]
            axios.post<string>(getRoute "getPartContent", data) |> toAsync

      setPartContent =
        fun filePath partUri content ->
            let data = [ filePath; partUri; content ]
            axios.post<bool>(getRoute "setPartContent", data) |> toAsync

      checkHealth =
        fun () ->
            async {
                try
                    return! axios.post<bool>(getRoute "checkHealth", []) |> toAsync
                with _ ->
                    return false
            }

      stopApplication =
        fun () ->
            async {
                Log.line "Stopping API Server ..."
                do! axios.post<unit>(getRoute "stopApplication", []) |> toAsync
            } }
