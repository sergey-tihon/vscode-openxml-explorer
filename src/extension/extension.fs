module OpenXmlExplorer

open Fable.Import
open Fable.System.IO

type MyTreeDataProvider() =
    let items = ResizeArray<string>();
    let event = vscode.EventEmitter<string option>()

    member this.openOpenXml(fileUri: vscode.Uri) =
        printfn "Open %A" fileUri
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

let activate (context : vscode.ExtensionContext) =
    printfn "Hello world from extension activate"

    let openXmlExplorerProvider = MyTreeDataProvider()

    vscode.window.registerTreeDataProvider(
        "openXmlExplorer", openXmlExplorerProvider)
    |> context.subscriptions.Add

    vscode.workspace.registerTextDocumentContentProvider(
        vscode.DocumentSelector.Case1 "pptx", openXmlExplorerProvider)
    |> context.subscriptions.Add
    vscode.workspace.registerTextDocumentContentProvider(
        vscode.DocumentSelector.Case1 "docx", openXmlExplorerProvider)
    |> context.subscriptions.Add
    vscode.workspace.registerTextDocumentContentProvider(
        vscode.DocumentSelector.Case1 "xlsx", openXmlExplorerProvider)
    |> context.subscriptions.Add

    let exploreFile : obj -> obj = fun param ->
        match param with
        | :? vscode.Uri as uri ->
            openXmlExplorerProvider.openOpenXml(uri)
            vscode.window.showInformationMessage("File Opened!", Array.empty<string>) |> box
        | _ ->
            vscode.window.showWarningMessage("Unexpected param!", param.ToString()) |> box

    vscode.commands.registerCommand("openxml-explorer.exploreFile", exploreFile)
    |> context.subscriptions.Add

    let clearView : obj -> obj = fun param ->
        openXmlExplorerProvider.clear()
        vscode.window.showInformationMessage("Clear View!", Array.empty<string>) |> box

    vscode.commands.registerCommand("openxml-explorer.clearView", clearView)
    |> context.subscriptions.Add
