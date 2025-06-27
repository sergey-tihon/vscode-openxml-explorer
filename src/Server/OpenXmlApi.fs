module OpenXmlApi

open System
open System.IO
open System.IO.Packaging
open System.Xml.Linq
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http

open Shared

let getPackageInfo(path: string) : Document =
    use package = Package.Open(path, FileMode.Open, FileAccess.Read)

    let rec parsePart (parent: string) (relId: string) (part: PackagePart) =
        let uri = part.Uri.OriginalString
        use stream = part.GetStream()

        { Uri = uri
          Name = Path.GetFileName uri
          RelationshipId = relId
          Length = stream.Length
          ContentType = part.ContentType
          ChildParts = part.GetRelationships() |> parseRelationships parent uri }

    and parseRelationships (parentUri: string) (thisUri: string) (relationship: PackageRelationshipCollection) =
        relationship
        |> Seq.filter(fun r -> r.TargetMode = TargetMode.Internal)
        |> Seq.choose(fun relationship ->
            let uri =
                PackUriHelper.ResolvePartUri(relationship.SourceUri, relationship.TargetUri)

            if uri.OriginalString = parentUri then
                None
            else
                relationship.Package.GetPart uri
                |> parsePart thisUri relationship.Id
                |> Some)
        |> Seq.sortBy _.Name
        |> Seq.toArray

    { Path = path
      FileName = Path.GetFileName path
      LastWriteTime = package.PackageProperties.Modified |> Option.ofNullable
      MainParts = package.GetRelationships() |> parseRelationships "" "" }

let getPartContent (path: string) (partUri: string) : string =
    use package = Package.Open(path, FileMode.Open, FileAccess.Read)
    let part = package.GetPart(Uri(partUri, UriKind.Relative))
    use stream = part.GetStream()
    use sr = new StreamReader(stream)
    sr.ReadToEnd()

let setPartContent (path: string) (partUri: string) (content: string) : bool =
    try
        use package = Package.Open(path, FileMode.Open, FileAccess.ReadWrite)
        let part = package.GetPart(Uri(partUri, UriKind.Relative))
        use stream = part.GetStream(FileMode.Create, FileAccess.Write)
        use writer = new StreamWriter(stream)
        writer.Write(content)
        writer.Flush()
        package.Flush()
        true
    with ex ->
        printfn $"Error saving part content: %A{ex}"
        false

let createOpenXmlApiFromContext(httpContext: HttpContext) : IOpenXmlApi =
    let lifetime = httpContext.GetService<IHostApplicationLifetime>()

    { getPackageInfo =
        fun filePath ->
            async {
                try
                    return getPackageInfo filePath
                with ex ->
                    printfn $"%A{ex}"
                    return getPackageInfo filePath
            }
      getPartContent =
        fun filePath partUri ->
            async {
                try
                    let content = getPartContent filePath partUri

                    if partUri.Contains ".xml" then
                        let xDoc = XDocument.Parse content
                        return xDoc.ToString()
                    else
                        return content
                with ex ->
                    printfn $"%A{ex}"
                    return $"%A{ex}"
            }
      setPartContent =
        fun filePath partUri content ->
            async {
                try
                    return setPartContent filePath partUri content
                with ex ->
                    printfn $"Error in setPartContent API: %A{ex}"
                    return false
            }
      checkHealth = fun () -> async { return true }
      stopApplication =
        fun () ->
            async {
                lifetime.StopApplication()
                return ()
            } }
