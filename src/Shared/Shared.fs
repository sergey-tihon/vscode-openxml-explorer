namespace Shared

open System

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

type IOpenXmlApi =
    { 
        test : unit -> Async<string>
        getName : string -> Async<string> 
    }