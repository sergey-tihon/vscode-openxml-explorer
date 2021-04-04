namespace Fable.Import.Axios

open System
open Fable.Core

/// Fable bindings for Axios
///
/// For usage instructions refer to the Axios docs at https://github.com/axios/axios
//module Axios =
type AxiosHttpBasicAuth =
    abstract username : string with get, set
    abstract password : string with get, set

and AxiosXHRConfigBase<'T> =
    abstract baseURL : string option with get, set
    abstract headers : obj option with get, set
    abstract ``params`` : obj option with get, set
    abstract paramsSerializer : Func<obj, string> option with get, set
    abstract timeout : float option with get, set
    abstract withCredentials : bool option with get, set
    abstract auth : AxiosHttpBasicAuth option with get, set
    abstract responseType : string option with get, set
    abstract xsrfCookieName : string option with get, set
    abstract xsrfHeaderName : string option with get, set
    abstract transformRequest : U2<Func<'T, 'U>, Func<'T, 'U>> option with get, set
    abstract transformResponse : Func<'T, 'U> option with get, set

and AxiosXHRConfig<'T> =
    inherit AxiosXHRConfigBase<'T>
    abstract url : string with get, set
    abstract method : string option with get, set
    abstract data : 'T option with get, set

/// This untyped version of the AxiosXHR interface is required to implement Axios.all
and AxiosXHR = interface end

and AxiosXHR<'T> =
    inherit AxiosXHR
    abstract data : 'T with get, set
    abstract status : int with get, set
    abstract statusText : string with get, set
    abstract headers : obj with get, set
    abstract config : AxiosXHRConfig<'T> with get, set

and Interceptor =
    abstract request : RequestInterceptor with get, set
    abstract response : ResponseInterceptor with get, set

and InterceptorId =
    float

and RequestInterceptor =
    abstract ``use`` : fulfilledFn:Func<AxiosXHRConfig<'U>, AxiosXHRConfig<'U>> -> InterceptorId
    abstract ``use`` : fulfilledFn:Func<AxiosXHRConfig<'U>, AxiosXHRConfig<'U>> * rejectedFn:Func<obj, obj> -> InterceptorId
    abstract eject : interceptorId:InterceptorId -> unit

and ResponseInterceptor =
    abstract ``use`` : fulfilledFn:Func<AxiosXHR<'T>, AxiosXHR<'T>> -> InterceptorId
    abstract ``use`` : fulfilledFn:Func<AxiosXHR<'T>, AxiosXHR<'T>> * rejectedFn:Func<obj, obj> -> InterceptorId
    abstract eject : interceptorId:InterceptorId -> unit

and AxiosInstance =
    abstract interceptors : Interceptor with get, set
    [<Emit("$0($1...)")>] abstract Invoke : config:AxiosXHRConfig<'T> -> JS.Promise<AxiosXHR<'T>>
    [<Emit("new $0($1...)")>] abstract Create : config:AxiosXHRConfig<'T> -> JS.Promise<AxiosXHR<'T>>
    abstract request : config:AxiosXHRConfig<'T> -> JS.Promise<AxiosXHR<'T>>
    /// Don't use this directly, use the strongly-typed Axios.all variant instead
    abstract all : xhrPromises: JS.Promise<AxiosXHR> seq -> JS.Promise<AxiosXHR[]>
    abstract spread : fn:Func<'T1, 'T2, 'U> -> Func<'T1 * 'T2, 'U>
    abstract get : url:string * ?config:AxiosXHRConfigBase<'T> -> JS.Promise<AxiosXHR<'T>>
    abstract delete : url:string * ?config:AxiosXHRConfigBase<'T> -> JS.Promise<AxiosXHR<'T>>
    abstract head : url:string * ?config:AxiosXHRConfigBase<'T> -> JS.Promise<AxiosXHR<'T>>
    abstract post : url:string * ?data:obj * ?config:AxiosXHRConfigBase<'T> -> JS.Promise<AxiosXHR<'T>>
    abstract put : url:string * ?data:obj * ?config:AxiosXHRConfigBase<'T> -> JS.Promise<AxiosXHR<'T>>
    abstract patch : url:string * ?data:obj * ?config:AxiosXHRConfigBase<'T> -> JS.Promise<AxiosXHR<'T>>

and AxiosStatic =
    inherit AxiosInstance
    abstract create : config:AxiosXHRConfigBase<'T> -> AxiosInstance

//
// Error handling
//

/// The error object returned by axios to the catch of a failed request promise.
/// See https://github.com/axios/axios/blob/master/lib/core/enhanceError.js
type private AxiosErrorJS<'T, 'E> =
    /// Exception name
    abstract member name : string
    /// Exception message
    abstract member message : string
    /// The config for the axios request that caused the error
    abstract member config : AxiosXHRConfigBase<'T> option
    /// An instance of XMLHttpRequest, populated if the request was made but no response was received
    abstract member request : obj option
    /// Error response returned by the server
    abstract member response : AxiosXHR<'E> option
module private AxiosErrorJS =
    /// Downcast the specified javascript error 'obj' to a strongly-typed AxiosErrorJs
    let fromJsNativeObj<'T, 'E> (jsNative : obj) : AxiosErrorJS<'T, 'E> =
         downcast jsNative

/// The request was sent and the server responded with a status code outside of the 2xx range.
type AxiosErrorResponse<'T, 'E> =
    { name : string
      message : string
      config : AxiosXHRConfigBase<'T>
      request : obj
      response : AxiosXHR<'E> }

/// The request was made but no response was received.
type AxiosNoResponse<'T> =
    { name : string
      message : string
      config : AxiosXHRConfigBase<'T>
      request : obj }

/// An error occurred while setting up the request
type AxiosRequestFailed<'T> =
    { name : string
      message : string
      config : AxiosXHRConfigBase<'T> }

/// <summary>
/// The three types of errors that can occur when making an axios request.
/// See: https://github.com/axios/axios#handling-errors
/// </summary>
///
/// <typeparam name="T">Content data type for a successful response.</typeparam>
/// <typeparam name="E">Content data type for an error response.</typeparam>
type AxiosError<'T, 'E> =
    | ErrorResponse of AxiosErrorResponse<'T, 'E>
    | NoResponse of AxiosNoResponse<'T>
    | RequestFailed of AxiosRequestFailed<'T>
    // An error thrown in the promise chain, but not thrown by Axios (e.g. exceptions thrown in user code)
    | UnknownError of System.Exception
module AxiosError =
    /// Get the message from the specified error
    let getMessage (error : AxiosError<_,_>) =
        match error with
        | ErrorResponse e ->
            e.message
        | NoResponse e ->
            e.message
        | RequestFailed e ->
            e.message
        | UnknownError e ->
            e.Message

    /// Convert a jsNative error 'obj' returned by axios to a strontly-typed AxiosError
    let fromNativeJsObj (jsNative : System.Exception) : AxiosError<'T, 'E> =
        let error = AxiosErrorJS.fromJsNativeObj jsNative
        match error.config, error.response, error.request with
        | Some config, Some response, Some request ->
            ErrorResponse
                { name = error.name
                  message = error.message
                  config = config
                  request = request
                  response = response }
        | Some config, _ , Some request ->
            NoResponse
                { name = error.name
                  message = error.message
                  config = config
                  request = request }
        | Some config, _, _ ->
            RequestFailed
                { name = error.name
                  message = error.message
                  config = config }
        | e ->
            UnknownError jsNative


/// Shared global instance of axios
module Globals =
    /// import axios from 'axios'
    [<Import("default", from="axios")>]
    let axios : AxiosStatic = jsNative


/// Axios-specific extensions to Promise.
module Promise =
    let private handleAxiosError (fail : AxiosError<'T, 'E> -> 'R) (jsError : System.Exception) : 'R =
        fail <| AxiosError.fromNativeJsObj jsError

    /// <summary>
    /// JS.Promise catch function, allowing the user to provide an error handling function
    /// that accepts a strongly typed <c>AxiosError</c>.
    /// </summary>
    ///
    /// <param name="fail">Error handling function.</param>
    /// <param name="">The promise for which to apply the error handling function.</param>
    let catchAxios (fail : AxiosError<'T, 'E> -> 'R) (promise : JS.Promise<'R>) : JS.Promise<'R> =
        promise
        |> Promise.catch (handleAxiosError fail)

    /// <summary>
    /// JS.Promise catch function, allowing the user to provide an error handling function
    /// that accepts a strongly typed <c>AxiosError</c>.
    /// </summary>
    ///
    /// <param name="fail">Error handling function.</param>
    /// <param name="">The promise for which to apply the error handling function.</param>
    let catchBindAxios (fail : AxiosError<'T, 'E> -> JS.Promise<'R>) (promise : JS.Promise<'R>) : JS.Promise<'R> =
        promise
        |> Promise.catchBind (handleAxiosError fail)

/// Axios functions needed to be wrapped in additional F# logic
module AxiosHelpers =
    let private upcastAxiosXhr (xhr : AxiosXHR<'t>) : AxiosXHR =
        xhr :> AxiosXHR

    let all2
        (xhr1 : JS.Promise<AxiosXHR<'T1>>)
        (xhr2 : JS.Promise<AxiosXHR<'T2>>)
        : JS.Promise<AxiosXHR<'T1> * AxiosXHR<'T2>> =

        // Box upcast all response types (Promise.all needs all promises to have the same type)
        let xhrObjSeq : JS.Promise<AxiosXHR> list = [
            xhr1 |> Promise.map upcastAxiosXhr
            xhr2 |> Promise.map upcastAxiosXhr ]

        Globals.axios.all xhrObjSeq
        |> Promise.map (fun results ->
            if results.Length <> xhrObjSeq.Length then
                failwith "Incorrect response array length returned by axios.all() JS implementation"

            let xhr1Result : AxiosXHR<'T1> = downcast results.[0]
            let xhr2Result : AxiosXHR<'T2> = downcast results.[1]
            (xhr1Result, xhr2Result))

    let all3
        (xhr1 : JS.Promise<AxiosXHR<'T1>>)
        (xhr2 : JS.Promise<AxiosXHR<'T2>>)
        (xhr3 : JS.Promise<AxiosXHR<'T3>>)
        : JS.Promise<AxiosXHR<'T1> * AxiosXHR<'T2> * AxiosXHR<'T3>> =

        // Box upcast all response types (Promise.all needs all promises to have the same type)
        let xhrObjSeq : JS.Promise<AxiosXHR> list = [
            xhr1 |> Promise.map upcastAxiosXhr
            xhr2 |> Promise.map upcastAxiosXhr
            xhr3 |> Promise.map upcastAxiosXhr ]

        Globals.axios.all xhrObjSeq
        |> Promise.map (fun results ->
            if results.Length <> xhrObjSeq.Length then
                failwith "Incorrect response array length returned by axios.all() JS implementation"

            let xhr1Result : AxiosXHR<'T1> = downcast results.[0]
            let xhr2Result : AxiosXHR<'T2> = downcast results.[1]
            let xhr3Result : AxiosXHR<'T3> = downcast results.[2]
            (xhr1Result, xhr2Result, xhr3Result))

    let all4
        (xhr1 : JS.Promise<AxiosXHR<'T1>>)
        (xhr2 : JS.Promise<AxiosXHR<'T2>>)
        (xhr3 : JS.Promise<AxiosXHR<'T3>>)
        (xhr4 : JS.Promise<AxiosXHR<'T4>>)
        : JS.Promise<AxiosXHR<'T1> * AxiosXHR<'T2> * AxiosXHR<'T3> * AxiosXHR<'T4>> =

        // Box upcast all response types (Promise.all needs all promises to have the same type)
        let xhrObjSeq : JS.Promise<AxiosXHR> list = [
            xhr1 |> Promise.map upcastAxiosXhr
            xhr2 |> Promise.map upcastAxiosXhr
            xhr3 |> Promise.map upcastAxiosXhr
            xhr4 |> Promise.map upcastAxiosXhr ]

        Globals.axios.all xhrObjSeq
        |> Promise.map (fun results ->
            if results.Length <> xhrObjSeq.Length then
                failwith "Incorrect response array length returned by axios.all() JS implementation"

            let xhr1Result : AxiosXHR<'T1> = downcast results.[0]
            let xhr2Result : AxiosXHR<'T2> = downcast results.[1]
            let xhr3Result : AxiosXHR<'T3> = downcast results.[2]
            let xhr4Result : AxiosXHR<'T4> = downcast results.[3]
            (xhr1Result, xhr2Result, xhr3Result, xhr4Result))

    let all5
        (xhr1 : JS.Promise<AxiosXHR<'T1>>)
        (xhr2 : JS.Promise<AxiosXHR<'T2>>)
        (xhr3 : JS.Promise<AxiosXHR<'T3>>)
        (xhr4 : JS.Promise<AxiosXHR<'T4>>)
        (xhr5 : JS.Promise<AxiosXHR<'T5>>)
        : JS.Promise<AxiosXHR<'T1> * AxiosXHR<'T2> * AxiosXHR<'T3> * AxiosXHR<'T4> * AxiosXHR<'T5>> =

        // Box upcast all response types (Promise.all needs all promises to have the same type)
        let xhrObjSeq : JS.Promise<AxiosXHR> list = [
            xhr1 |> Promise.map upcastAxiosXhr
            xhr2 |> Promise.map upcastAxiosXhr
            xhr3 |> Promise.map upcastAxiosXhr
            xhr4 |> Promise.map upcastAxiosXhr
            xhr5 |> Promise.map upcastAxiosXhr ]

        Globals.axios.all xhrObjSeq
        |> Promise.map (fun results ->
            if results.Length <> xhrObjSeq.Length then
                failwith "Incorrect response array length returned by axios.all() JS implementation"

            let xhr1Result : AxiosXHR<'T1> = downcast results.[0]
            let xhr2Result : AxiosXHR<'T2> = downcast results.[1]
            let xhr3Result : AxiosXHR<'T3> = downcast results.[2]
            let xhr4Result : AxiosXHR<'T4> = downcast results.[3]
            let xhr5Result : AxiosXHR<'T5> = downcast results.[4]
            (xhr1Result, xhr2Result, xhr3Result, xhr4Result, xhr5Result))

    let all6
        (xhr1 : JS.Promise<AxiosXHR<'T1>>)
        (xhr2 : JS.Promise<AxiosXHR<'T2>>)
        (xhr3 : JS.Promise<AxiosXHR<'T3>>)
        (xhr4 : JS.Promise<AxiosXHR<'T4>>)
        (xhr5 : JS.Promise<AxiosXHR<'T5>>)
        (xhr6 : JS.Promise<AxiosXHR<'T6>>)
        : JS.Promise<AxiosXHR<'T1> * AxiosXHR<'T2> * AxiosXHR<'T3> * AxiosXHR<'T4> * AxiosXHR<'T5> * AxiosXHR<'T6>> =

        // Box upcast all response types (Promise.all needs all promises to have the same type)
        let xhrObjSeq : JS.Promise<AxiosXHR> list = [
            xhr1 |> Promise.map upcastAxiosXhr
            xhr2 |> Promise.map upcastAxiosXhr
            xhr3 |> Promise.map upcastAxiosXhr
            xhr4 |> Promise.map upcastAxiosXhr
            xhr5 |> Promise.map upcastAxiosXhr
            xhr6 |> Promise.map upcastAxiosXhr ]

        Globals.axios.all xhrObjSeq
        |> Promise.map (fun results ->
            if results.Length <> xhrObjSeq.Length then
                failwith "Incorrect response array length returned by axios.all() JS implementation"

            let xhr1Result : AxiosXHR<'T1> = downcast results.[0]
            let xhr2Result : AxiosXHR<'T2> = downcast results.[1]
            let xhr3Result : AxiosXHR<'T3> = downcast results.[2]
            let xhr4Result : AxiosXHR<'T4> = downcast results.[3]
            let xhr5Result : AxiosXHR<'T5> = downcast results.[4]
            let xhr6Result : AxiosXHR<'T6> = downcast results.[5]
            (xhr1Result, xhr2Result, xhr3Result, xhr4Result, xhr5Result, xhr6Result))