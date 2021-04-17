module OpenXmlExplorer.Extension

open Fable.Import
open Agent

let mutable agentOption : MailboxProcessor<AgentActions> option = None

let activate (context : vscode.ExtensionContext) =

    let openXmlExplorerProvider = Model.MyTreeDataProvider()
    let agent = createAgent openXmlExplorerProvider context
    agentOption <- Some agent

    vscode.Disposable(fun _ -> 
        Log.line "Stopping server from Disposable"
        agent.Post StopServer)
    |> context.subscriptions.Add

    vscode.window.registerTreeDataProvider("openXmlExplorer", openXmlExplorerProvider)
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

    vscode.commands.registerCommand("openxml-explorer.restartServer", objfy2 (fun _ ->
        agent.Post RestartServer
    )) |> context.subscriptions.Add
    
// Signature is important https://github.com/microsoft/vscode/issues/567
let deactivate() =
    Log.line "Extension deactivation"
    match agentOption with
    | Some(agent) -> 
        Log.line "Stopping server from extension deactivation"
        agent.Post StopServer
    | _ -> ()