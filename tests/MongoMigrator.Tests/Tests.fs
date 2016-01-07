module MongoMigrator.Tests

open Migrator
open NUnit.Framework

[<Test>]
let ``changeset parser parses author name`` () =
  let name = "test.json"
  let lines = ["// changeset phipps:documents2-Core.TenantId_1_DocumentStatus.StatusFlags_1_Core.PublishSetCode_LC_1 runAlways:true"]
  let result = ManifestReader.ReadFromFileAndLines(name, lines) |> Seq.head
  Assert.AreEqual("phipps", result.Author)
