﻿module PackageTasks

open ProjectInfo

open MessagePrompts
open BasicTasks
open TestTasks

open BlackFox.Fake
open Fake.Core
open Fake.IO.Globbing.Operators

open System.Text.RegularExpressions

/// https://github.com/Freymaurer/Fake.Extensions.Release#release-notes-in-nuget
let private replaceCommitLink input = 
    let commitLinkPattern = @"\[\[#[a-z0-9]*\]\(.*\)\] "
    Regex.Replace(input,commitLinkPattern,"")

let pack = BuildTask.create "Pack" [clean; build; runTests] {
    if promptYesNo (sprintf "creating stable package with version %s OK?" stableVersionTag ) 
        then
            !! "src/**/*.*proj"
            -- "src/bin/*"
            |> Seq.iter (Fake.DotNet.DotNet.pack (fun p ->
                let msBuildParams =
                    {p.MSBuildParams with 
                        Properties = ([
                            "Version",stableVersionTag
                            "PackageReleaseNotes",  (release.Notes |> List.map replaceCommitLink |> String.concat "\r\n" )
                        ] @ p.MSBuildParams.Properties)
                    }
                {
                    p with 
                        MSBuildParams = msBuildParams
                        OutputPath = Some pkgDir
                }
            ))
            // This is used to create ISADotNet.Fable with the Fable subfolder as explained here:
            // https://fable.io/docs/your-fable-project/author-a-fable-library.html
            "src/ArcGraphModel/ArcGraphModel.fsproj"
            |> Fake.DotNet.DotNet.pack (fun p ->
                let msBuildParams =
                    {p.MSBuildParams with 
                        Properties = ([
                            "PackageId", "ArcGraphModel.Fable"
                            "Version",stableVersionTag
                            "PackageReleaseNotes",  (release.Notes |> List.map replaceCommitLink |> String.concat "\r\n")
                        ] @ p.MSBuildParams.Properties)
                    }
                let test = p
                {
                    p with 
                        MSBuildParams = msBuildParams
                        OutputPath = Some pkgDir
                }
            )
    else failwith "aborted"
}

let packPrerelease = BuildTask.create "PackPrerelease" [setPrereleaseTag; clean; build; runTests] {
    if promptYesNo (sprintf "package tag will be %s OK?" prereleaseTag )
        then 
            !! "src/**/*.*proj"
            -- "src/bin/*"
            |> Seq.iter (Fake.DotNet.DotNet.pack (fun p ->
                        let msBuildParams =
                            {p.MSBuildParams with 
                                Properties = ([
                                    "Version", prereleaseTag
                                    "PackageReleaseNotes",  (release.Notes |> List.map replaceCommitLink  |> String.toLines )
                                ] @ p.MSBuildParams.Properties)
                            }
                        {
                            p with 
                                VersionSuffix = Some prereleaseSuffix
                                OutputPath = Some pkgDir
                                MSBuildParams = msBuildParams
                        }
            ))
            // This is used to create ISADotNet.Fable with the Fable subfolder as explained here:
            // https://fable.io/docs/your-fable-project/author-a-fable-library.html
            "src/ArcGraphModel/ArcGraphModel.fsproj"
            |> Fake.DotNet.DotNet.pack (fun p ->
                let msBuildParams =
                    {p.MSBuildParams with 
                        Properties = ([
                            "PackageId", "ArcGraphModel.Fable"
                            "Version", prereleaseTag
                            "PackageReleaseNotes",  (release.Notes |> List.map replaceCommitLink |> String.toLines)
                        ] @ p.MSBuildParams.Properties)
                    }
                {
                    p with 
                        VersionSuffix = Some prereleaseSuffix
                        OutputPath = Some pkgDir
                        MSBuildParams = msBuildParams
                }
            )
    else
        failwith "aborted"
}