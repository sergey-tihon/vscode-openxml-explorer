module Server.Tests

open System.IO
open System.Xml
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

let private copyToTemp fileName =
    let path =
        Path.Combine(Path.GetTempPath(), $"%s{Path.GetRandomFileName()}-%s{fileName}")

    File.Copy(Path.Combine(__SOURCE_DIRECTORY__, "../data", fileName), path)
    path

[<Tests>]
let setPartContentTests =
    testList
        "setPartContent"
        [
            testCase "persists edited XML"
            <| fun () ->
                let path = copyToTemp "word.docx"

                try
                    let edited =
                        (OpenXmlApi.getPartContent path "/word/document.xml").Replace("This is test file", "Edited by OpenXml Explorer")

                    OpenXmlApi.setPartContent path "/word/document.xml" edited
                    let saved = OpenXmlApi.getPartContent path "/word/document.xml"
                    Expect.stringContains saved "Edited by OpenXml Explorer" "edited text is persisted"
                    Expect.stringStarts saved "<?xml" "XML declaration is written"
                finally
                    File.Delete path

            testCase "preserves whitespace-only nodes"
            <| fun () ->
                let path = copyToTemp "word.docx"

                try
                    let content = "<root>\n  <child />\n</root>"
                    OpenXmlApi.setPartContent path "/word/document.xml" content
                    let saved = OpenXmlApi.getPartContent path "/word/document.xml"
                    Expect.stringContains saved "<root>\n  <child />\n</root>" "whitespace-only text nodes are preserved"
                finally
                    File.Delete path

            testCase "rejects malformed XML"
            <| fun () ->
                let path = copyToTemp "word.docx"

                try
                    let original = OpenXmlApi.getPartContent path "/word/document.xml"
                    Expect.throwsT<XmlException> (fun () -> OpenXmlApi.setPartContent path "/word/document.xml" "<root>") "malformed XML is rejected"
                    Expect.equal (OpenXmlApi.getPartContent path "/word/document.xml") original "package is unchanged"
                finally
                    File.Delete path
        ]
