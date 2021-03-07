module OpenXmlExplorer


open Fable.Import

type MyTreeDataProvider() =
    let event = vscode.EventEmitter<string option>()

    member this.openOpenXml(fileUri: vscode.Uri) =
        event.emit(fileUri.path) |> ignore

    interface vscode.TreeDataProvider<string> with
        member this.onDidChangeTreeData = event.event
        member this.getTreeItem(x) = failwith "not implemented" 
        member this.getChildren(x) = ResizeArray<string>()
        member this.getParent = None

    interface vscode.TextDocumentContentProvider with
        member this.provideTextDocumentContent(url) = ""

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


    let action : obj -> obj = fun _ ->
        vscode.window.showInformationMessage("Hello world from command!", Array.empty<string>) |> box

    vscode.commands.registerCommand("openxml-explorer.sayHello", action)
    |> context.subscriptions.Add

    let exploreFile : obj -> obj = fun param ->
        match param with
        | :? vscode.Uri as uri ->
            // openXmlExplorerProvider.openOpenXml(uri)
            vscode.window.showInformationMessage("File Opened!", Array.empty<string>) |> box
        | _ ->
            vscode.window.showWarningMessage("Unexpected param!", param.ToString()) |> box

    vscode.commands.registerCommand("openxml-explorer.exploreFile", exploreFile)
    |> context.subscriptions.Add

    let clearView : obj -> obj = fun param ->
        vscode.window.showInformationMessage("Clear View!", Array.empty<string>) |> box

    vscode.commands.registerCommand("openxml-explorer.clearView", clearView)
    |> context.subscriptions.Add
