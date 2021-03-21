module Agent

open Fable.Import
open Shared
open Model

type Actions =
    | ExplorePackage of uri:vscode.Uri
    | ClosePackage of document:DataNode
    | CloseAllPackages

let createAgent (provider: MyTreeDataProvider) =
    let client = Remoting.client.Value

    MailboxProcessor.Start(fun inbox->
        let rec messageLoop() = async {
            let! cmd = inbox.Receive();
            match cmd with
            | ExplorePackage uri -> 
                try 
                    let! doc = client.getPackageInfo uri.path
                    provider.openOpenXml(doc)
                    vscode.window.showInformationMessage($"Package '%s{doc.FileName}' opened!", Array.empty<string>) |> ignore
                with
                | e -> 
                    vscode.window.showErrorMessage($"Package '%s{uri.path}' cannot be opened! Error: '%s{e.Message}'", Array.empty<string>) |> ignore
            | ClosePackage document -> 
                provider.close(document)
                vscode.window.showInformationMessage($"Package closed!", Array.empty<string>) |> ignore            
            | CloseAllPackages ->
                provider.clear()
                vscode.window.showInformationMessage("All packages closed!", Array.empty<string>) |> ignore
            return! messageLoop()
        }
        messageLoop()
    )
