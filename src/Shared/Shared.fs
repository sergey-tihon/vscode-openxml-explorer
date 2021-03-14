namespace Shared

open System

type DocumentPart =
    {
        Uri : string
        Title : string
        ContentType : string
        ChildParts : DocumentPart []
    }
    
type Document =
    {
        Path : string
        FileName : string
        LastWriteTime : DateTime option
        MainParts: DocumentPart []
    }

module Route =
    let builder typeName methodName =
        $"/api/%s{typeName}/%s{methodName}"

type IOpenXmlApi =
    { 
        getPackageInfo : string -> Async<Document> 
    }