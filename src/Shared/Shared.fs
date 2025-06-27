namespace Shared

open System

type DocumentPart =
    { Uri: string
      Name: string
      RelationshipId: string
      Length: int64
      ContentType: string
      ChildParts: DocumentPart[] }

    member this.GetTooltip() =
        $"%s{this.Uri} (%s{this.RelationshipId})"

type Document =
    { Path: string
      FileName: string
      LastWriteTime: DateTime option
      MainParts: DocumentPart[] }

module Route =
    let builder typeName methodName =
        $"/api/%s{typeName}/%s{methodName}"

type IOpenXmlApi =
    { getPackageInfo: string -> Async<Document>
      getPartContent: string -> string -> Async<string>
      setPartContent: string -> string -> string -> Async<bool>

      checkHealth: unit -> Async<bool>
      stopApplication: unit -> Async<unit> }
