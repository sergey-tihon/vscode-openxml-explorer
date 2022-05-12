namespace OpenXmlExplorer

module Log =
    open Fable.Import.VSCode

    let private channel = lazy(Vscode.window.createOutputChannel "OpenXml Package Explorer")

    let line str = 
        printfn $"%s{str}"
        channel.Value.appendLine(str)
    let show() = channel.Value.show(true)

module Promise =
    open Fable.Core
    open Fable.Import.VSCode

    let ofThenable (t: Thenable<'t>) : JS.Promise<'t> = unbox (box t)
    let toThenable (p: JS.Promise<'t>) : Thenable<'t> = unbox (box p)

[<AutoOpen>]
module ContextExtensions =
    open Fable.Import.VSCode.Vscode
    
    type ExtensionContext with
        member inline x.Subscribe< ^t when ^t: (member dispose: unit -> obj option)>(item: ^t) =
            x.subscriptions.Add(unbox (box item))


[<AutoOpen>]
module Objectify =
    let inline objfy2 (f: 'a -> 'b) : ResizeArray<obj option> -> obj option = unbox f
    let inline objfy3 (f: 'a -> 'b -> 'c) : ResizeArray<obj option> -> obj option = unbox f
