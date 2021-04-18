module OpenXmlExplorer.Agent

open Fable.Import
open Shared
open Model

type AgentActions =
    | ExplorePackage of uri:vscode.Uri
    | ClosePackage of document:DataNode
    | CloseAllPackages
    | RestartServer
    | StopServer

let createAgent (provider: MyTreeDataProvider) (context : vscode.ExtensionContext) =
    let getNewClient () = async {
        let! port = ServerHost.getFreePort()
        Log.line $"Starting server on port %d{port} ..."
        let client = ServerHost.startServer port context.extensionPath
        let rec waitForBootstrap() = async {
            let! status = client.checkHealth()
            if not status then
                do! Async.Sleep(200)
                do! waitForBootstrap()
        }
        do! waitForBootstrap()
        provider.ApiClint <- Some client
        return client
    }
    MailboxProcessor.Start(fun inbox->
        let rec messageLoop (client':IOpenXmlApi option) = async {
            let! cmd = inbox.Receive();
            match cmd with
            | ExplorePackage uri ->
                let! client =
                    match client' with
                    | Some client -> async { return client }
                    | None -> getNewClient()
                try 
                    let! doc = client.getPackageInfo uri.fsPath
                    provider.openOpenXml(doc)
                with
                | e -> 
                    vscode.window.showErrorMessage($"Package '%s{uri.fsPath}' cannot be opened! Error: '%s{e.Message}'", Array.empty<string>) |> ignore
                return! messageLoop (Some client)
            | ClosePackage document -> 
                provider.close(document)
                return! messageLoop client'
            | CloseAllPackages ->
                provider.clear()
                return! messageLoop client'
            | RestartServer ->
                provider.clear()
                match client' with
                | Some client -> do! client.stopApplication()
                | _ -> ()
                let! newClient = getNewClient()
                return! messageLoop (Some newClient)
            | StopServer -> 
                provider.clear()
                match client' with
                | Some client -> do! client.stopApplication()
                | _ -> ()
                return! messageLoop None
        }
        messageLoop None
    )
