module OpenXmlExplorer

open Fable.Import
open Agent

let activate (context : vscode.ExtensionContext) =
    printfn "[!!!] OpenXml Explorer Extension Activated!"

    let openXmlExplorerProvider = Model.MyTreeDataProvider()
    let agent = createAgent openXmlExplorerProvider

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
            agent.Post (ExploreFile uri) |> box
        | _ ->
            vscode.window.showWarningMessage("Unexpected param!", param.ToString()) |> box

    vscode.commands.registerCommand("openxml-explorer.exploreFile", exploreFile)
    |> context.subscriptions.Add

    let clearView : obj -> obj = fun param ->
        agent.Post ResetView |> box

    vscode.commands.registerCommand("openxml-explorer.clearView", clearView)
    |> context.subscriptions.Add
