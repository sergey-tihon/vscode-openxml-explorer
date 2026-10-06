module Server.Tests

open System.IO
open System.Text.Encodings.Web
open System.Text.Json
open System.Text.Json.Serialization
open Expecto
open Shouldly

let serializerOptions = JsonFSharpOptions.Default().ToJsonSerializerOptions()
serializerOptions.Encoder <- JavaScriptEncoder.UnsafeRelaxedJsonEscaping
serializerOptions.WriteIndented <- true

ShouldMatchConfiguration.ShouldMatchApprovedDefaults.ConfigureDiffEngine()
|> ignore

let shouldMatchPackageInfo fileName =
    let path = Path.Combine(__SOURCE_DIRECTORY__, "../data", fileName)
    let packageInfo = OpenXmlApi.getPackageInfo path

    let normalizedPath =
        Path.GetRelativePath(__SOURCE_DIRECTORY__, packageInfo.Path).Replace('\\', '/')

    let doc =
        { packageInfo with
            Path = "{ProjectDirectory}/" + normalizedPath
            LastWriteTime =
                packageInfo.LastWriteTime
                |> Option.map(fun time -> time.ToUniversalTime())
        }

    let snapshot = JsonSerializer.Serialize(doc, serializerOptions)

    snapshot.ShouldMatchApproved(fun options -> options.WithDiscriminator(fileName) |> ignore)

[<Tests>]
let tests =
    [
        "word.docx"
        "excel.xlsx"
        "powerpoint.pptx"
        "word-libre6.docx"
        "excel-libre6.xlsx"
        "powerpoint-libre6.pptx"
    ]
    |> List.map(fun fileName -> testCase fileName (fun () -> shouldMatchPackageInfo fileName))
    |> testList "Package info"
