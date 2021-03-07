module VSCodeFable

open Fable.Import

let activate (context : vscode.ExtensionContext) =
    printfn "Hello world from extension activate"

    let action : obj -> obj = fun _ ->
        vscode.window.showInformationMessage("Hello world from command!", Array.empty<string>) |> box

    vscode.commands.registerCommand("openxml-explorer.sayHello", action)
    |> context.subscriptions.Add
