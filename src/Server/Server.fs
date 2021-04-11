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
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .Configure(fun app -> app.UseRemoting webApp)
        .UseUrls(Route.host)
        .Build()
        .Run()
    0
