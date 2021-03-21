module OpenXmlExplorer

open Fable.Import
open Fable.Core.JS
open Agent

let activate (context : vscode.ExtensionContext) =
    
    let openXmlExplorerProvider = Model.MyTreeDataProvider()
    let agent = createAgent openXmlExplorerProvider

    vscode.window.registerTreeDataProvider(
        "openXmlExplorer", openXmlExplorerProvider)
    |> context.subscriptions.Add

    vscode.workspace.registerTextDocumentContentProvider(
        vscode.DocumentSelector.Case1 "openxml", openXmlExplorerProvider)
    |> context.subscriptions.Add

    let explorePackage : obj -> obj = fun param ->
        match param with
        | :? vscode.Uri as uri ->
            agent.Post (ExplorePackage uri) |> box
        | _ ->
            vscode.window.showWarningMessage("Unexpected param!", param.ToString()) |> box

    vscode.commands.registerCommand("openxml-explorer.explorePackage", explorePackage)
    |> context.subscriptions.Add

    let closePackage : obj -> obj = fun param ->
        match param with
        | :? Model.DataNode as node ->
            agent.Post (ClosePackage node) |> box
        | _ ->
            vscode.window.showWarningMessage("Unexpected param!", param.ToString()) |> box

    vscode.commands.registerCommand("openxml-explorer.closePackage", closePackage)
    |> context.subscriptions.Add

    let closeAllPackage : obj -> obj = fun param ->
        agent.Post (CloseAllPackages) |> box

    vscode.commands.registerCommand("openxml-explorer.closeAllPackage", closeAllPackage)
    |> context.subscriptions.Add

    let openOpenXmlResource : obj -> obj = fun param ->
        match param with
        | :? vscode.Uri as uri ->
            promise {
                let! document = vscode.workspace.openTextDocument(uri)
                let! editor = vscode.window.showTextDocument(document)
                return ()
            } |> box
        | _ ->
            vscode.window.showWarningMessage("Unexpected param!", param.ToString()) |> box

    vscode.commands.registerCommand("openxml-explorer.openPart", openOpenXmlResource)
    |> context.subscriptions.Add