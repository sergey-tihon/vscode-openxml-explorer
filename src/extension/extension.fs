module OpenXmlExplorer

open Fable.Core
open Fable.Import
open Fable.System.IO
open Fable.Remoting.Client
open Shared

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

type Actions =
    | ExploreFile of uri:vscode.Uri
    | ResetView 

let createAgent (provider: MyTreeDataProvider) =
    // get a typed-proxy for the service
    let openXmlApi =
        Remoting.createApi()
        |> Remoting.withBaseUrl "http://0.0.0.0:20489"
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.buildProxy<IOpenXmlApi>

    MailboxProcessor.Start(fun inbox->
        let rec messageLoop() = async {
            let! cmd = inbox.Receive();
            match cmd with
            | ExploreFile uri -> 
                provider.openOpenXml(uri)

                async {
                    printfn "Before call"
                    let! name = openXmlApi.getName uri.path
                    printfn "!!! Never happens '%A'" name
                } |> Async.StartImmediate

                let _ = vscode.window.showInformationMessage("File Opened!", Array.empty<string>)
                ()
            | ResetView -> 
                provider.clear()
                let _ = vscode.window.showInformationMessage("Clear View!", Array.empty<string>)
                ()
            return! messageLoop()
        }
        messageLoop()
    )


let activate (context : vscode.ExtensionContext) =
    printfn "[!!!] OpenXml Explorer Extension Activated!"

    let openXmlExplorerProvider = MyTreeDataProvider()
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
