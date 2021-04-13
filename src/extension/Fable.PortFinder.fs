// ts2fable 0.8.0
namespace Fable.Import.PortFinder
open System
open Fable.Core
open Fable.Core.JS

type Array<'T> = System.Collections.Generic.IList<'T>
type Error = System.Exception

/// portfinder.js typescript definitions.
/// (C) 2011, Charlie Robbins
type [<AllowNullLiteral>] PortfinderCallback =
    /// portfinder.js typescript definitions.
    /// (C) 2011, Charlie Robbins
    [<Emit "$0($1...)">] abstract Invoke: err: Error * port: int -> unit

type [<AllowNullLiteral>] PortFinderOptions =
    /// Host to find available port on.
    abstract host: string option with get, set
    /// search start port (equals to port when not provided)
    /// This exists because getPort and getPortPromise mutates port state in
    /// recursive calls and doesn't have a way to retrieve begininng port while
    /// searching.
    abstract startPort: int option with get, set
    /// <summary>Minimum port (takes precedence over <c>basePort</c>).</summary>
    abstract port: int option with get, set
    /// Maximum port
    abstract stopPort: int option with get, set

/// The lowest port to begin any port search from.
//let [<Import("basePort","module")>] basePort: int = jsNative
/// The highest port to end any port search from.
//let [<Import("highestPort","module")>] highestPort: int = jsNative

type [<AllowNullLiteral>] IExports =
    /// Responds with a unbound port on the current machine.
    abstract getPort: callback: PortfinderCallback -> unit
    abstract getPort: options: PortFinderOptions * callback: PortfinderCallback -> unit
    abstract getPorts: count: int * options: PortFinderOptions * callback: (Error -> Array<int> -> unit) -> unit
    /// Responds a promise of an unbound port on the current machine.
    abstract getPortPromise: ?options: PortFinderOptions -> Promise<int>

module Globals =
    [<Import("*", "portfinder")>]
    let portfinder: IExports = jsNative