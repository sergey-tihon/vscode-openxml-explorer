module OpenXmlApi

open System
open System.IO
open System.IO.Packaging

open Shared

let getPackageInfo (path:string)  : Document = 
    use package = Package.Open(path, FileMode.Open, FileAccess.ReadWrite)
    
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
    }