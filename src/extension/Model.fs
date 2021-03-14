module Model

open Fable.Import
open Fable.System.IO

type MyTreeDataProvider() =
    let items = ResizeArray<string>();
    let event = vscode.EventEmitter<string option>()

    member this.openOpenXml(fileUri: vscode.Uri) =
        items.Add(fileUri.path)
        event.fire(None)

    member this.clear() =
        items.Clear();
        event.fire(None)

    interface vscode.TreeDataProvider<string> with
        member this.onDidChangeTreeData = event.event
        member this.getTreeItem(x) = 
            let fileName = Path.GetFileName(x)
            vscode.TreeItem(fileName, vscode.TreeItemCollapsibleState.None)
        member this.getChildren(x) = 
            if System.String.IsNullOrEmpty(x) then items else ResizeArray<_>()
        member this.getParent = None

    interface vscode.TextDocumentContentProvider with
        member this.provideTextDocumentContent(url) = url.path
