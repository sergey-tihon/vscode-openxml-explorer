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
        member val command = "openOpenXmlResource" with get, set
        member val arguments = Some args with get, set

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
                    resourceUri = Some(vscode.Uri.parse(document.Path)),
                    contextValue = Some "openxml",
                    iconPath = (vscode.ThemeIcon("package") |> U4.Case4 |> Some))
            | Part (part, document) -> 
                let command = 
                    if part.Uri.Contains(".xml") then 
                        let uri = vscode.Uri(scheme="openxml", path=part.Uri, fragment=document.Path)
                        let cmd = OpenPartCommand([box uri] |> ResizeArray) :> vscode.Command
                        Some cmd
                    else None

                vscode.TreeItem(part.Title, getCollapseStatus part.ChildParts,
                    tooltip = Some part.Uri,
                    command = command,
                    contextValue = (command |> Option.map(fun _ -> "file")),
                    iconPath =
                        (if command.IsSome then "file-code" else "file-binary"
                         |> vscode.ThemeIcon |> U4.Case4 |> Some)
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
            Remoting.client.Value.getPartContent (url.fragment) (url.path)
            |> Async.StartAsPromise
            |> U2.Case2
