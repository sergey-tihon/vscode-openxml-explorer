module Server.Tests

open NUnit.Framework
open FsUnit
open System.IO

let parseFile path expected =
    let path = Path.Combine(__SOURCE_DIRECTORY__, path)
    let doc = OpenXmlApi.getPackageInfo path
    doc.MainParts
    |> Seq.sumBy (fun x-> x.ChildParts.Length)
    |> should be (greaterThanOrEqualTo expected)

let [<Test>] ``Parse word.docx``() =
    parseFile "../data/word.docx" 5

let [<Test>] ``Parse excel.xlsx``() =
    parseFile "../data/excel.xlsx" 3

let [<Test>] ``Parse powerpoint.pptx``() =
    parseFile "../data/powerpoint.pptx" 6

let [<Test>] ``Parse word-libre6.docx``() =
    parseFile "../data/word-libre6.docx" 3

let [<Test>] ``Parse excel-libre6.xlsx``() =
    parseFile "../data/excel-libre6.xlsx" 2

let [<Test>] ``Parse powerpoint-libre6.pptx``() =
    parseFile "../data/powerpoint-libre6.pptx" 5
