module OpenXmlExplorer.Model

open System.Collections.Generic
open Fable.Import.VSCode
open Fable.Core
open Fable.Core.JsInterop

type DataNode =
    | Document of document: Shared.Document
    | Part of part: Shared.DocumentPart * document: Shared.Document

    override this.ToString() =
        match this with
        | Document doc -> $"Document: %s{doc.FileName}[%d{doc.MainParts.Length}]"
        | Part(part, _) -> $"Part: %s{part.Uri}[%d{part.ChildParts.Length}]"

let getCollapseStatus(list: 'a[]) =
    if Array.length list > 0 then
        Vscode.TreeItemCollapsibleState.Collapsed
    else
        Vscode.TreeItemCollapsibleState.None

type OpenPartCommand(path: string, fragment: string) =
    let uri =
        vscode.Uri.from(
            { new Vscode.UriStaticFromComponents with
                member _.scheme = "openxml"
                member _.path = path |> Some
                member _.fragment = fragment |> Some
                member _.authority = None
                member _.query = None
            }
        )

    let args = [ box uri |> Some ] |> ResizeArray

    interface Vscode.Command with
        member val title = "Open OpenXml Resource" with get, set
        member val command = "openxml-explorer.openPart" with get, set
        member val arguments = Some args with get, set
        member val tooltip = None with get, set

type MyTreeDataProvider() =
    let items = ResizeArray<DataNode>()

    let onDidChangeFileEmitter =
        vscode.EventEmitter.Create<ResizeArray<Vscode.FileChangeEvent>>()

    let modifiedTimes = Dictionary<string, float>()

    let readOnlyError(uri: Vscode.Uri) =
        unbox<exn>(vscode.FileSystemError.NoPermissions(U2.Case2 uri))

    let unavailableError(uri: Vscode.Uri) =
        unbox<exn>(vscode.FileSystemError.Unavailable(U2.Case2 uri))

    let onDidChangeTreeDataEmitter =
        vscode.EventEmitter.Create<U3<DataNode, ResizeArray<DataNode>, unit> option>()

    member val ApiClint: Shared.IOpenXmlApi option = None with get, set

    member _.openOpenXml(document: Shared.Document) =
        let node = Document(document)
        items.Add(node)
        onDidChangeTreeDataEmitter.fire(None)

    member _.clear() =
        items.Clear()
        onDidChangeTreeDataEmitter.fire(None)

    member _.close(item: DataNode) =
        items.Remove(item) |> ignore
        onDidChangeTreeDataEmitter.fire(None)

    interface Vscode.TreeDataProvider<DataNode> with
        member val onDidChangeTreeData = onDidChangeTreeDataEmitter.event |> Some with get, set

        member _.getTreeItem(node) =
            match node with
            | Document document ->
                let item =
                    vscode.TreeItem.Create(U2.Case1 document.FileName, getCollapseStatus document.MainParts)

                item.tooltip <- document.Path |> U2.Case1 |> Some
                item.resourceUri <- Some(vscode.Uri.parse(document.Path))
                item.contextValue <- Some "openxml"
                item.iconPath <- vscode.ThemeIcon.Create("package") |> U4.Case4 |> Some
                U2.Case1 item
            | Part(part, document) when part.Uri.Contains(".xml") ->
                let item =
                    vscode.TreeItem.Create(U2.Case1 part.Name, getCollapseStatus part.ChildParts)

                item.tooltip <- part.GetTooltip() |> U2.Case1 |> Some
                item.command <- Some(OpenPartCommand(part.Uri, document.Path) :> Vscode.Command)
                item.contextValue <- Some "file"
                item.iconPath <- vscode.ThemeIcon.Create("file-code") |> U4.Case4 |> Some
                U2.Case1 item
            | Part(part, _) ->
                let item =
                    vscode.TreeItem.Create(U2.Case1 part.Name, getCollapseStatus part.ChildParts)

                item.tooltip <- part.GetTooltip() |> U2.Case1 |> Some
                item.iconPath <- vscode.ThemeIcon.Create("file-binary") |> U4.Case4 |> Some
                U2.Case1 item

        member _.resolveTreeItem(item, element, _) =
            item |> U2.Case1 |> Some

        member _.getChildren(node) =
            match node with
            | None -> items
            | Some(Document document) ->
                document.MainParts
                |> Array.map(fun x -> Part(x, document))
                |> ResizeArray
            | Some(Part(part, document)) ->
                part.ChildParts
                |> Array.map(fun x -> Part(x, document))
                |> ResizeArray
            |> U2.Case1
            |> Some

        member _.getParent _ = None

    interface Vscode.FileSystemProvider with
        member _.onDidChangeFile = onDidChangeFileEmitter.event

        member _.watch(_, _) =
            vscode.Disposable.Create(fun () -> None)

        member _.stat(uri) =
            let key = uri.toString()

            if not(modifiedTimes.ContainsKey key) then
                modifiedTimes[key] <- JS.Constructors.Date.now()

            let stat = createEmpty<Vscode.FileStat>
            stat.``type`` <- Vscode.FileType.File
            stat.ctime <- 0.
            stat.mtime <- modifiedTimes[key]
            stat.size <- 0.
            U2.Case1 stat

        member _.readDirectory _ =
            U2.Case1(ResizeArray<string * Vscode.FileType>())

        member _.createDirectory uri =
            raise(readOnlyError uri)

        member this.readFile(uri) =
            match this.ApiClint with
            | Some client ->
                async {
                    let! content = client.getPartContent uri.fragment uri.path
                    return Utf8.encode content
                }
                |> Async.StartAsPromise
                |> Promise.toThenable
                |> U2.Case2
            | None -> raise(unavailableError uri)

        member this.writeFile(uri, content, _) =
            match this.ApiClint with
            | Some client ->
                async {
                    match! client.setPartContent uri.fragment uri.path (Utf8.decode content) with
                    | Some error -> raise(unbox<exn>(vscode.FileSystemError.Create(U2.Case1 $"Cannot save part '%s{uri.path}': %s{error}")))
                    | None -> modifiedTimes[uri.toString()] <- JS.Constructors.Date.now()
                }
                |> Async.StartAsPromise
                |> Promise.toThenable
                |> U2.Case2
            | None -> raise(unavailableError uri)

        member _.delete(uri, _) =
            raise(readOnlyError uri)

        member _.rename(oldUri, _, _) =
            raise(readOnlyError oldUri)

        member _.copy(source, _, _) =
            raise(readOnlyError source)
