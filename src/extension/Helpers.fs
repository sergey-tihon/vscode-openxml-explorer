namespace OpenXmlExplorer

module Log =
    open Fable.Import

    let private channel = lazy(vscode.window.createOutputChannel "OpenXml Package Explorer")

    let line str = 
        printfn $"%s{str}"
        channel.Value.appendLine(str)
    let show() = channel.Value.show()

[<AutoOpen>]
module Objectify =
    let inline objfy2 (f: 'a -> 'b): obj -> obj = unbox f
    let inline objfy3 (f: 'a -> 'b -> 'c): obj -> obj -> obj = unbox f
    