module OpenXmlExplorer.Model

open Fable.Import.VSCode
open Fable.Core

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
                member _.query = None }
        )

    let args = [ box uri |> Some ] |> ResizeArray

    interface Vscode.Command with
        member val title = "Open OpenXml Resource" with get, set
        member val command = "openxml-explorer.openPart" with get, set
        member val arguments = Some args with get, set
        member val tooltip = None with get, set

type MyTreeDataProvider() =
    let items = ResizeArray<DataNode>()

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

                item.tooltip <- part.Uri |> U2.Case1 |> Some
                item.command <- Some(OpenPartCommand(part.Uri, document.Path) :> Vscode.Command)
                item.contextValue <- Some "file"
                item.iconPath <- vscode.ThemeIcon.Create("file-code") |> U4.Case4 |> Some
                U2.Case1 item
            | Part(part, _) ->
                let item =
                    vscode.TreeItem.Create(U2.Case1 part.Name, getCollapseStatus part.ChildParts)

                item.tooltip <- part.Uri |> U2.Case1 |> Some
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

    interface Vscode.TextDocumentContentProvider with
        member val onDidChange = None with get, set

        member this.provideTextDocumentContent(url, _) =
            match this.ApiClint with
            | Some(client) ->
                async {
                    let! content = client.getPartContent (url.fragment) (url.path)
                    return Some content
                }
                |> Async.StartAsPromise
                |> Promise.toThenable
                |> U2.Case2
            | None -> U2.Case1 "Extension API client is not available"
            |> Some
