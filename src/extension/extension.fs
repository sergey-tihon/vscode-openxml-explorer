module OpenXmlExplorer.Extension

open Fable.Import.VSCode
open Agent

let mutable agentOption: MailboxProcessor<AgentActions> option = None

type DefaultViewOpions() =
    interface Vscode.TextDocumentShowOptions with
        member val viewColumn = None with get, set
        member val preserveFocus = None with get, set
        member val preview = None with get, set
        member val selection = None with get, set

let activate(context: Vscode.ExtensionContext) =

    let openXmlExplorerProvider = Model.MyTreeDataProvider()
    let agent = createAgent openXmlExplorerProvider context
    agentOption <- Some agent

    vscode.Disposable.Create(fun _ ->
        Log.line "Stopping server from Disposable"
        agent.Post StopServer
        None)
    |> context.Subscribe

    Vscode.window.registerTreeDataProvider("openXmlExplorer", openXmlExplorerProvider)
    |> context.Subscribe

    Vscode.workspace.registerTextDocumentContentProvider("openxml", openXmlExplorerProvider)
    |> context.Subscribe

    Vscode.commands.registerCommand("openxml-explorer.explorePackage", objfy2(fun (uri: Vscode.Uri) -> agent.Post(ExplorePackage uri)))
    |> context.Subscribe

    Vscode.commands.registerCommand("openxml-explorer.closePackage", objfy2(fun (node: Model.DataNode) -> agent.Post(ClosePackage node)))
    |> context.Subscribe

    Vscode.commands.registerCommand("openxml-explorer.closeAllPackage", objfy2(fun _ -> agent.Post(CloseAllPackages)))
    |> context.Subscribe

    Vscode.commands.registerCommand(
        "openxml-explorer.openPart",
        objfy2(fun (uri: Vscode.Uri) ->
            promise {
                let! document = Vscode.workspace.openTextDocument(uri) |> Promise.ofThenable

                let! _ =
                    Vscode.window.showTextDocument(document, DefaultViewOpions())
                    |> Promise.ofThenable

                return ()
            })
    )
    |> context.Subscribe

    Vscode.commands.registerCommand("openxml-explorer.restartServer", objfy2(fun () -> agent.Post RestartServer))
    |> context.Subscribe

// Signature is important https://github.com/microsoft/vscode/issues/567
let deactivate() =
    Log.line "Extension deactivation"

    match agentOption with
    | Some(agent) ->
        Log.line "Stopping server from extension deactivation"
        agent.Post StopServer
    | _ -> ()
