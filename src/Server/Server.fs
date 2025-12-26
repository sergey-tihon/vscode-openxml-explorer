module Server

open Microsoft.AspNetCore.Builder
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
        failwithf $"Please specify server Url as first parameter. args=%A{args}"

    let app = WebApplication.CreateBuilder(args).Build()
    app.Urls.Add(args.[0])
    app.UseRemoting webApp
    app.Run()

    0
