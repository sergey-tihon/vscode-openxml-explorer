module Server.Tests

open NUnit.Framework
open System.IO
open VerifyTests
open VerifyNUnit
open Argon

VerifierSettings.AddExtraSettings(fun settings -> settings.DefaultValueHandling <- DefaultValueHandling.Include)

[<TestCase("word.docx")>]
[<TestCase("excel.xlsx")>]
[<TestCase("powerpoint.pptx")>]
[<TestCase("word-libre6.docx")>]
[<TestCase("excel-libre6.xlsx")>]
[<TestCase("powerpoint-libre6.pptx")>]
let verifyPackageInfo fileName =
    let path = Path.Combine(__SOURCE_DIRECTORY__, "../data", fileName)
    let doc = OpenXmlApi.getPackageInfo path

    task {
        let! _ = Verifier.Verify(doc).UseFileName(fileName)
        ()
    }
