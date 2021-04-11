module Agent

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
    let client = Remoting.startServer context.extensionPath
    provider.ApiClint <- Some client

    MailboxProcessor.Start(fun inbox->
        let rec messageLoop() = async {
            let! cmd = inbox.Receive();
            match cmd with
            | ExplorePackage uri -> 
                try 
                    let! doc = client.getPackageInfo uri.fsPath
                    provider.openOpenXml(doc)
                with
                | e -> 
                    vscode.window.showErrorMessage($"Package '%s{uri.fsPath}' cannot be opened! Error: '%s{e.Message}'", Array.empty<string>) |> ignore
            | ClosePackage document -> 
                provider.close(document)
            | CloseAllPackages ->
                provider.clear()
            | RestartServer ->
                do! client.stopApplication()
                Remoting.startServer context.extensionPath |> ignore
            | StopServer -> 
                do! client.stopApplication()
            return! messageLoop()
        }
        messageLoop()
    )
