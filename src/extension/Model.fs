module Model

open Fable.Import
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

type MyTreeDataProvider() =
    let items = ResizeArray<DataNode>();
    let event = vscode.EventEmitter<DataNode option>()

    member this.openOpenXml(document: Document) =
        let node = Document(document)
        items.Add(node)
        event.fire(None)

    member this.clear() =
        items.Clear();
        event.fire(None)

    interface vscode.TreeDataProvider<DataNode> with
        member this.onDidChangeTreeData = event.event
        member this.getTreeItem(node) =
            printfn $"getTreeItem %O{node}"
            match node with
            | Document document -> 
                vscode.TreeItem(document.FileName, getCollapseStatus document.MainParts,
                    tooltip = Some document.Path,
                    resourceUri = Some(vscode.Uri.parse(document.Path)))
            | Part (part, document) -> 
                vscode.TreeItem(part.Title, getCollapseStatus part.ChildParts,
                    tooltip = Some part.Uri,
                    id = Some $"%s{document.Path};%s{part.Uri}")

        member this.getChildren(node) = 
            printfn $"getChildren %O{node}"
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
        member this.provideTextDocumentContent(url) = url.path
