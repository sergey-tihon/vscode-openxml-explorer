module OpenXmlApi

open System
open System.IO
open System.IO.Packaging

open Shared
open System.Xml.Linq

let getPackageInfo (path:string)  : Document = 
    use package = Package.Open(path, FileMode.Open, FileAccess.Read)
    
    let rec parsePart (parent:string)  (part:PackagePart) =
        let uri = part.Uri.OriginalString
        {
            Uri = uri
            Title = Path.GetFileName(uri)
            ContentType = part.ContentType
            ChildParts = part.GetRelationships() |> parseRelationships parent uri
        }
    and parseRelationships (parentUri:string) (thisUri:string) (relationship:PackageRelationshipCollection) =
        relationship
        |> Seq.choose (fun relationship ->
            let uri = PackUriHelper.ResolvePartUri(relationship.SourceUri, relationship.TargetUri)
            if uri.OriginalString = parentUri then None
            else relationship.Package.GetPart uri
                 |> parsePart thisUri |> Some)
        |> Seq.toArray
        
    {
        Path = path
        FileName = Path.GetFileName(path)
        LastWriteTime = package.PackageProperties.Modified |> Option.ofNullable
        MainParts = package.GetRelationships() |> parseRelationships "" ""
    }

let getPartContent  (path:string) (partUri:string) formatXml : string = 
    use package = Package.Open(path, FileMode.Open, FileAccess.Read)
    let part = package.GetPart(Uri(partUri, UriKind.Relative))
    use stream = part.GetStream()
    use sr = new StreamReader(stream)
    let xml = sr.ReadToEnd()
    if formatXml then
        let xDoc = XDocument.Parse(xml)
        xDoc.ToString()
    else xml

let openXmlApi : IOpenXmlApi =
    { 
        getPackageInfo = 
            fun filePath -> async {
                try 
                    return getPackageInfo filePath
                with
                | ex ->
                    printfn $"%A{ex}"
                    return getPackageInfo filePath
            }
        getPartContent =
            fun filePath partUri -> async {
                try
                    return getPartContent filePath partUri true
                with
                | ex ->
                    printfn $"%A{ex}"
                    return $"%A{ex}"
            }
    }