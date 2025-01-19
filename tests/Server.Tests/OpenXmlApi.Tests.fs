module Server.Tests

open NUnit.Framework
open System.IO
open VerifyTests
open VerifyNUnit
open Argon

VerifierSettings.AddExtraSettings(fun settings -> settings.DefaultValueHandling <- DefaultValueHandling.Include)

[<TestCase("../data/word.docx")>]
[<TestCase("../data/excel.xlsx")>]
[<TestCase("../data/powerpoint.pptx")>]
[<TestCase("../data/word-libre6.docx")>]
[<TestCase("../data/excel-libre6.xlsx")>]
[<TestCase("../data/powerpoint-libre6.pptx")>]
let verifyPackageInfo path =
    let path = Path.Combine(__SOURCE_DIRECTORY__, path)
    let doc = OpenXmlApi.getPackageInfo path

    task {
        let fileName = Path.GetFileName(path)
        let! _ = Verifier.Verify(doc).UseFileName(fileName)
        ()
    }
