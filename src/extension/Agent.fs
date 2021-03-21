module Agent

open Fable.Import
open Shared
open Model

type Actions =
    | ExploreFile of uri:vscode.Uri
    | ResetView

let createAgent (provider: MyTreeDataProvider) =
    let client = Remoting.client.Value

    MailboxProcessor.Start(fun inbox->
        let rec messageLoop() = async {
            let! cmd = inbox.Receive();
            match cmd with
            | ExploreFile uri -> 
                let! doc = client.getPackageInfo uri.path
                provider.openOpenXml(doc)
                vscode.window.showInformationMessage($"File '%s{doc.FileName}' Opened!", Array.empty<string>) |> ignore
            | ResetView -> 
                provider.clear()
                vscode.window.showInformationMessage("Clear View!", Array.empty<string>) |> ignore
            return! messageLoop()
        }
        messageLoop()
    )
