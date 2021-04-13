module OpenXmlApi

open System
open System.IO
open System.IO.Packaging
open System.Xml.Linq
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http

open Shared

let getPackageInfo (path:string)  : Document = 
    use package = Package.Open(path, FileMode.Open, FileAccess.Read)
    
    let rec parsePart (parent:string)  (part:PackagePart) =
        let uri = part.Uri.OriginalString
        use stream = part.GetStream()
        {
            Uri = uri
            Name = Path.GetFileName(uri)
            Length = stream.Length
            ContentType = part.ContentType
            ChildParts = part.GetRelationships() |> parseRelationships parent uri
        }
    and parseRelationships (parentUri:string) (thisUri:string) (relationship:PackageRelationshipCollection) =
        relationship
        |> Seq.filter (fun r -> r.TargetMode = TargetMode.Internal)
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

let getPartContent  (path:string) (partUri:string) : string = 
    use package = Package.Open(path, FileMode.Open, FileAccess.Read)
    let part = package.GetPart(Uri(partUri, UriKind.Relative))
    use stream = part.GetStream()
    use sr = new StreamReader(stream)
    sr.ReadToEnd()

let createOpenXmlApiFromContext (httpContext: HttpContext) : IOpenXmlApi =
    let lifetime = httpContext.GetService<IHostApplicationLifetime>()
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
                    let content = getPartContent filePath partUri
                    if partUri.Contains(".xml") then
                        let xDoc = XDocument.Parse(content)
                        return xDoc.ToString()
                    else 
                        return content
                with
                | ex ->
                    printfn $"%A{ex}"
                    return $"%A{ex}"
            }
        stopApplication = fun () -> async {
            lifetime.StopApplication()
            return ()
        }
    }
