module OpenXmlExplorer

open Fable.Core
open Fable.Import
open Fable.System.IO
open Shared
open Fable.Axios
open Fable.Axios.Globals

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
    | Activation
    | ExploreFile of uri:vscode.Uri
    | ResetView
    
type MyIp =
    { ip : string}

let createAgent (provider: MyTreeDataProvider) =
    MailboxProcessor.Start(fun inbox->
        let rec messageLoop() = async {
            let! cmd = inbox.Receive();
            match cmd with
            | Activation ->
                printfn "Before call"
                let! resp =
                    axios.get<MyIp> ("https://api.ipify.org?format=json")
                    |> Async.AwaitPromise
                let _ = vscode.window.showInformationMessage($"You IP address is %A{resp.data.ip}", Array.empty<string>)
                printfn $"!!! Never happened before '%A{resp.data.ip}'"

            | ExploreFile uri -> 
                provider.openOpenXml(uri)
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

    agent.Post Activation

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
