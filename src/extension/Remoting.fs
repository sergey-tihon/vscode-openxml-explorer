module Remoting

open Fable.Core
open Fable.Axios
open Fable.Axios.Globals
open Shared

let inline private toAsync<'a> (resp: JS.Promise<AxiosXHR<'a>>) =
    resp
    |> Promise.map (fun x -> x.data)
    |> Async.AwaitPromise

let getClient (serverHost) : IOpenXmlApi =
    let typeName = nameof(IOpenXmlApi)
    let getRoute methodName =
        serverHost + Route.builder typeName methodName

    {
        getPackageInfo = fun filePath -> 
            let data = [filePath]
            axios.post<Document>(getRoute "getPackageInfo", data)
            |> toAsync
    }