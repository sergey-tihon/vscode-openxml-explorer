module Server

open Microsoft.AspNetCore.Hosting
open Fable.Remoting.Server
open Fable.Remoting.AspNetCore

open Shared

let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromContext OpenXmlApi.createOpenXmlApiFromContext

[<EntryPoint>]
let main args =
    if args.Length = 0 then
        failwithf "Please specify server Url as first parameter. args=%A" args
    WebHostBuilder()
        .UseKestrel()
        .Configure(fun app -> app.UseRemoting webApp)
        .UseUrls(args.[0])
        .Build()
        .Run()
    0
