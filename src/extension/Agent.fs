module Agent

open Fable.Import
open Shared
open Model

type Actions =
    | ExploreFile of uri:vscode.Uri
    | ResetView

let createAgent (provider: MyTreeDataProvider) =
    let client = Remoting.getClient "http://0.0.0.0:20489"

    MailboxProcessor.Start(fun inbox->
        let rec messageLoop() = async {
            let! cmd = inbox.Receive();
            match cmd with
            | ExploreFile uri -> 
                provider.openOpenXml(uri)
                let! fileName = client.getName uri.path
                vscode.window.showInformationMessage($"File '%s{fileName}' Opened!", Array.empty<string>) |> ignore
            | ResetView -> 
                provider.clear()
                vscode.window.showInformationMessage("Clear View!", Array.empty<string>) |> ignore
            return! messageLoop()
        }
        messageLoop()
    )
