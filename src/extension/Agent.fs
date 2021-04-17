module OpenXmlExplorer.Agent

open Fable.Import
open Fable.Import.PortFinder
open Fable.Core
open Fable.Core.JsInterop
open Shared
open Model

type AgentActions =
    | ExplorePackage of uri:vscode.Uri
    | ClosePackage of document:DataNode
    | CloseAllPackages
    | RestartServer
    | StopServer

let createAgent (provider: MyTreeDataProvider) (context : vscode.ExtensionContext) =
    let getFreePort () = 
        let opts = createEmpty<PortFinderOptions>
        opts.port <- Some (20489)
        Globals.portfinder.getPortPromise(opts)
        |> Async.AwaitPromise
    let getNewClient () = async {
        let! port = getFreePort()
        Log.line $"Starting server on port %d{port} ..."
        let client = Remoting.startServer port context.extensionPath
        provider.ApiClint <- Some client
        return client
    }

    MailboxProcessor.Start(fun inbox->
        let rec messageLoop (client:IOpenXmlApi) = async {
            let! cmd = inbox.Receive();
            match cmd with
            | ExplorePackage uri -> 
                try 
                    let! doc = client.getPackageInfo uri.fsPath
                    provider.openOpenXml(doc)
                with
                | e -> 
                    vscode.window.showErrorMessage($"Package '%s{uri.fsPath}' cannot be opened! Error: '%s{e.Message}'", Array.empty<string>) |> ignore
                return! messageLoop client
            | ClosePackage document -> 
                provider.close(document)
                return! messageLoop client
            | CloseAllPackages ->
                provider.clear()
                return! messageLoop client
            | RestartServer ->
                provider.clear()
                do! client.stopApplication()
                let! newClient = getNewClient()
                return! messageLoop newClient
            | StopServer -> 
                provider.clear()
                do! client.stopApplication()
                return! messageLoop client
        }
        async {
            let! client = getNewClient()
            return! messageLoop client
        }
    )
