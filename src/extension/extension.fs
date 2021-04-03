module OpenXmlExplorer

open Fable.Import
open Agent

[<AutoOpen>]
module Objectify =
    let inline objfy2 (f: 'a -> 'b): obj -> obj = unbox f
    let inline objfy3 (f: 'a -> 'b -> 'c): obj -> obj -> obj = unbox f

let activate (context : vscode.ExtensionContext) =

    let openXmlExplorerProvider = Model.MyTreeDataProvider()
    let agent = createAgent openXmlExplorerProvider context

    vscode.window.registerTreeDataProvider(
        "openXmlExplorer", openXmlExplorerProvider)
    |> context.subscriptions.Add

    vscode.workspace.registerTextDocumentContentProvider(
        vscode.DocumentSelector.Case1 "openxml", openXmlExplorerProvider)
    |> context.subscriptions.Add

    vscode.commands.registerCommand("openxml-explorer.explorePackage", objfy2 (fun (uri:vscode.Uri) ->
        agent.Post (ExplorePackage uri) 
    )) |> context.subscriptions.Add

    vscode.commands.registerCommand("openxml-explorer.closePackage", objfy2 (fun (node:Model.DataNode) ->
        agent.Post (ClosePackage node)
    ))|> context.subscriptions.Add

    vscode.commands.registerCommand("openxml-explorer.closeAllPackage", objfy2 (fun _ ->
        agent.Post (CloseAllPackages)
    )) |> context.subscriptions.Add

    vscode.commands.registerCommand("openxml-explorer.openPart",  objfy2 (fun (uri:vscode.Uri) ->
        promise {
            let! document = vscode.workspace.openTextDocument(uri)
            let! _ = vscode.window.showTextDocument(document)
            return ()
        }
    )) |> context.subscriptions.Add