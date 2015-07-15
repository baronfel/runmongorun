namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("MongoMigrator")>]
[<assembly: AssemblyProductAttribute("MongoMigrator")>]
[<assembly: AssemblyDescriptionAttribute("A repeatable, deterministic mongo script runner.")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
