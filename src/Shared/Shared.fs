namespace Shared

open System

type DocumentPart = {
    Uri: string
    Name: string
    Length: int64
    ContentType: string
    ChildParts: DocumentPart[]
}

type Document = {
    Path: string
    FileName: string
    LastWriteTime: DateTime option
    MainParts: DocumentPart[]
}

module Route =

    let builder typeName methodName =
        $"/api/%s{typeName}/%s{methodName}"

type IOpenXmlApi = {
    getPackageInfo: string -> Async<Document>
    getPartContent: string -> string -> Async<string>

    checkHealth: unit -> Async<bool>
    stopApplication: unit -> Async<unit>
}
