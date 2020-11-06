#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "Facades/netstandard"
#r "netstandard"
#endif
open System
open Fake.Core
open Fake.DotNet
open Fake.Tools
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

type DeployEnvironment =
    | Test
    | Prod
let sln = "ActiveGameNight.sln"
let rootPath = __SOURCE_DIRECTORY__
let src = rootPath @@ "src"
let srcGlob = src @@ "**/*.??proj"
let deployGlob = rootPath  @@ "deploy/**/*.??proj"
let backendPath = src @@ "Backend"
let backendProj = backendPath @@ "Backend.fsproj"
let farmerDeployPath = rootPath @@ "deploy" @@ "farmer"
let outputPath = "./output"

//-----------------------------------------------------------------------------
// Helpers
//-----------------------------------------------------------------------------
let invokeAsync f = async { f () }

let isRelease (targets : Target list) =
    targets
    |> Seq.map(fun t -> t.Name)
    |> Seq.exists ((=)"CreateRelease")

let configuration (targets : Target list) =
    let defaultVal = if isRelease targets then "Release" else "Debug"
    match Environment.environVarOrDefault "CONFIGURATION" defaultVal with
    | "Debug" -> DotNet.BuildConfiguration.Debug
    | "Release" -> DotNet.BuildConfiguration.Release
    | config -> DotNet.BuildConfiguration.Custom config

let dotNetWatch watchCmd workingDir =
    DotNet.exec
        (fun p -> { p with WorkingDirectory = workingDir })
        (sprintf "watch %s" watchCmd)
        ""
    |> ignore

let semVersion = 
    let changelog = Changelog.load "CHANGELOG.md"
    changelog.LatestEntry.SemVer

    
//-----------------------------------------------------------------------------
// Build Target Implementations
//-----------------------------------------------------------------------------

let clean _ =
    [ "bin"; "temp"; outputPath ]
    |> Shell.cleanDirs

    !! srcGlob
    ++ deployGlob
    |> Seq.collect(fun p ->
        ["bin";"obj"]
        |> Seq.map(fun sp ->
            IO.Path.GetDirectoryName p @@ sp)
        )
    |> Shell.cleanDirs

    [ "paket-files/paket.restore.cached" ]
    |> Seq.iter Shell.rm

let dotnetPublishBackend ctx =
    Shell.copyDir (outputPath @@ "public") (backendPath @@ "public") (fun _ -> true)

    let args =
        [
            sprintf "/p:PackageVersion=%s" (semVersion).AsString
        ]
    DotNet.publish(fun c ->
        { c with
            Configuration = configuration (ctx.Context.AllExecutingTargets)
            NoBuild = true
            NoRestore = true
            SelfContained = Some false
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
            OutputPath = Some (outputPath)
        }) backendProj

let dotnetRestore _ = DotNet.restore id sln

let writeVersionToFile _ =
    let sb = System.Text.StringBuilder("module Backend.Version\n\n")
    Printf.bprintf sb "    let version = \"%s\"\n" (semVersion.AsString)

    File.writeString false (backendPath @@ "Version.fs") (sb.ToString())
    
let dotnetBuild ctx =
    let args =
        [
            sprintf "/p:PackageVersion=%s" semVersion.AsString
        ]
    DotNet.build(fun c ->
        { c with
            Configuration = configuration (ctx.Context.AllExecutingTargets)
            NoRestore = true
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
        }) sln

let watchApp _ =

    let server() = dotNetWatch "run" backendPath

    [ server ]
    |> Seq.iter (invokeAsync >> Async.Catch >> Async.Ignore >> Async.Start)
    printfn "Press Ctrl+C (or Ctrl+Break) to stop..."
    let cancelEvent = Console.CancelKeyPress |> Async.AwaitEvent |> Async.RunSynchronously
    cancelEvent.Cancel <- true

let runFarmerDeploy env _ =
    match env with
    | Test -> 
        DotNet.exec (DotNet.Options.withWorkingDirectory farmerDeployPath) "run" "-- deployEnvironment=test"
    | Prod -> 
        DotNet.exec (DotNet.Options.withWorkingDirectory farmerDeployPath) "run" "-- deployEnvironment=prod"
    |> ignore

let gitCheckUniqueTag _ =
    let allTags = 
        match Git.CommandHelper.runGitCommand "" "tag" with
        | false, _,_ -> 
            failwith "git error"
        | true, tags, _ -> 
            tags 
    if allTags |> List.contains (semVersion.AsString) then failwithf "git tag already exists for %s, make sure ChangeLog is updated with a new version for this release" semVersion.AsString

let gitTagDeployment _ =
    Git.Branches.tag "" semVersion.AsString


let waitForDeployment env _ =
    let waitForResponse timeout url =
        let sw = System.Diagnostics.Stopwatch.StartNew()
        let mutable lastExceptionMsg = ""
        let rec waitForResponse'() =
            if sw.Elapsed < timeout then
                try 
                    Fake.Net.Http.get "" "" url |> ignore
                    Trace.tracefn "Site %s responded after %f s" url sw.Elapsed.TotalSeconds
                with exn ->
                    lastExceptionMsg <- exn.ToString()
                    Trace.tracefn "Site %s not responding after %f s, waiting..." url sw.Elapsed.TotalSeconds
                    System.Threading.Thread.Sleep 2000
                    waitForResponse'()
            else 
                Trace.traceErrorfn "%O" lastExceptionMsg 
                failwithf "Site %s is not running after %f s" url timeout.TotalSeconds
        waitForResponse'()


    Trace.tracefn "Waiting 5 seconds before warmup tests..."
    System.Threading.Thread.Sleep 5000
    match env with
    | Test ->
        waitForResponse (TimeSpan.FromSeconds(30.)) "https://active-game-night-test.azurewebsites.net/"
    | Prod ->
        waitForResponse (TimeSpan.FromSeconds(30.)) "https://active-game-night.azurewebsites.net/"

//-----------------------------------------------------------------------------
// Build Target Declaration
//-----------------------------------------------------------------------------

Target.create "Clean" clean
Target.create "DotnetRestore" dotnetRestore
Target.create "WriteVersionToFile" writeVersionToFile
Target.create "DotnetBuild" dotnetBuild
Target.create "WatchApp" watchApp
Target.create "Build" ignore
Target.create "DotnetPublishBackend" dotnetPublishBackend
Target.create "Package" ignore
Target.create "DeployToTest" (runFarmerDeploy Test)
Target.create "DeployToProd" (runFarmerDeploy Prod)
Target.create "GitCheckVersionTag" gitCheckUniqueTag
Target.create "GitTagProdDeployment" gitTagDeployment
Target.create "WaitForTestDeployment" (waitForDeployment Test)
Target.create "WaitForProdDeployment" (waitForDeployment Prod)
Target.create "CreateTestRelease" ignore
Target.create "CreateProdRelease" ignore


//-----------------------------------------------------------------------------
// Build Target Dependencies
//-----------------------------------------------------------------------------

// Only call Clean if 'Package' was in the call chain
// Ensure Clean is called before 'DotnetRestore' and 'InstallClient'
"Clean" ?=> "DotnetRestore"
"Clean" ==> "Package"

"WriteVersionToFile"
    ?=> "WatchApp"

"DotnetRestore"
    ==> "WriteVersionToFile"
    ==> "DotnetBuild"
    ==> "Build"
    ==> "DotnetPublishBackend"
    ==> "Package"

"DeployToTest"
    ==> "WaitForTestDeployment"
    ==> "CreateTestRelease"

"GitCheckVersionTag"
    ==> "DeployToProd"
    ==> "GitTagProdDeployment"
    ==> "WaitForProdDeployment"
    ==> "CreateProdRelease"

//-----------------------------------------------------------------------------
// Start
//-----------------------------------------------------------------------------

Target.runOrDefaultWithArguments "Build"