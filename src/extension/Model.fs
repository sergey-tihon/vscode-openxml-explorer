module Model

open Fable.Import
open Fable.Core
open Shared

type DataNode =
    | Document of document:Shared.Document 
    | Part of part:Shared.DocumentPart * document:Shared.Document 

    override this.ToString() =
        match this with
        | Document doc -> $"Document: %s{doc.FileName}[%d{doc.MainParts.Length}]"
        | Part (part,_) -> $"Part: %s{part.Uri}[%d{part.ChildParts.Length}]"

let getCollapseStatus (list:'a []) =
    if Array.length list > 0
        then vscode.TreeItemCollapsibleState.Collapsed
        else vscode.TreeItemCollapsibleState.None

type OpenPartCommand(args) =
    interface vscode.Command with
        member val title = "Open OpenXml Resource" with get, set
        member val command = "openxml-explorer.openPart" with get, set
        member val arguments = Some args with get, set

type MyTreeDataProvider() =
    let items = ResizeArray<DataNode>();
    let event = vscode.EventEmitter<DataNode option>()

    member val ApiClint : IOpenXmlApi option = None with get, set

    member this.openOpenXml(document: Document) =
        let node = Document(document)
        items.Add(node)
        event.fire(None)

    member this.clear() =
        items.Clear();
        event.fire(None)

    member this.close(item:DataNode) =
        items.Remove(item) |> ignore
        event.fire(None)

    interface vscode.TreeDataProvider<DataNode> with
        member this.onDidChangeTreeData = event.event
        member this.getTreeItem(node) =
            printfn $"getTreeItem %O{node}"
            match node with
            | Document document -> 
                vscode.TreeItem(document.FileName, getCollapseStatus document.MainParts,
                    tooltip = Some document.Path,
                    resourceUri = Some(vscode.Uri.parse(document.Path)),
                    contextValue = Some "openxml",
                    iconPath = (vscode.ThemeIcon("package") |> U4.Case4 |> Some))
            | Part (part, document) when part.Uri.Contains(".xml") ->
                let command = 
                    let uri = vscode.Uri(scheme="openxml", path=part.Uri, fragment=document.Path)
                    OpenPartCommand([box uri] |> ResizeArray) :> vscode.Command

                vscode.TreeItem(part.Name, getCollapseStatus part.ChildParts,
                    tooltip = Some(part.Uri),
                    command = Some command,
                    contextValue = Some "file",
                    iconPath = (vscode.ThemeIcon("file-code") |> U4.Case4 |> Some)
                )
            | Part (part, _) -> 
                vscode.TreeItem(part.Name, getCollapseStatus part.ChildParts,
                    tooltip = Some(part.Uri),
                    iconPath = (vscode.ThemeIcon("file-binary") |> U4.Case4 |> Some)
                )

        member this.getChildren(node) = 
            match node with
            | None -> items
            | Some(Document document) -> 
                document.MainParts
                |> Array.map(fun x -> Part(x, document))
                |> ResizeArray
            | Some(Part (part, document)) -> 
                part.ChildParts
                |> Array.map(fun x -> Part(x, document))
                |> ResizeArray

        member this.getParent = None

    interface vscode.TextDocumentContentProvider with
        member this.provideTextDocumentContent(url) = 
            match this.ApiClint with
            | Some(client) ->
                client.getPartContent (url.fragment) (url.path)
                |> Async.StartAsPromise
                |> U2.Case2
            | None ->
                U2.Case1 "Extension API client is not available"