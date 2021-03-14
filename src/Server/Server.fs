module Server

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn

open Shared

let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue OpenXmlApi.openXmlApi
    |> Remoting.buildHttpHandler

let app =
    application {
        url "http://0.0.0.0:20489"
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
    }

run app
