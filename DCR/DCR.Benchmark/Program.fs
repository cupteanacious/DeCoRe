// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"

open System
open System.IO

open Benchmark.RandomGraphGenerator
open Benchmark.SingleEventGraphGenerator
open Benchmark.IncreasingEventGraphGenerator
open Benchmark.EventWithRelationGraphGenerator
open Benchmark.TestFileGenerator

open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Running
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Exporters
open BenchmarkDotNet.Exporters.Csv
open BenchmarkDotNet.Columns

open DCRGraph.State
open DCRGraph.DCRMapping
open DCRGraph.DCRGraphDTO
open DCRGraph.EndpointProjection
open DCR2Jolie.EndpointCodegen
open BenchmarkDotNet.Diagnosers
open BenchmarkDotNet.Engines
open DCRGraph.DCRGraph


module CLIMain = DCR.CLI.Main

let SRCDIR = __SOURCE_DIRECTORY__

type RandomGraphBenchmarks() =
    // [<Benchmark>]
    // member this.TestPipeline () =
    //     let dto = jsonToDCRGraphDTO (sprintf "%s.json" this.CurrentTestFilePathWithoutExtension)
    //     let g = dto |> convertDCRGraphDTOToG
    //     if not (isDeadlockFree g) then
    //         failwith "Randomly generated graph was not deadlock free!"
    //     let ePP = endpointProjectGraph g true
    //     GenerateEverything g ePP this.CurrentTestFilesDirPath |>  ignore

    [<Benchmark>]
    member this.TestPipelineThroughCLI () =
        Console.WriteLine(sprintf "[TestPipelineThroughCLI] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        CLIMain.main this.CurrentArgs

    member val ValuesForNumRoles = seq { for i in 2 .. 2 do yield int (2.0**i) } // 4, 8, 16, 32, 64
    member val ValuesForNumEvents = seq { for i in 2 .. 2 do yield int (2.0**i) } // 4, 8, 16, 32, 64
    member val ValuesForNumRelations = seq { for i in 2 .. 2 do yield int (2.0**i) } // 4, 8, 16, 32, 64

    member val CurrentTestFilesDirPath = "" with get, set
    member val CurrentTestFilePathWithoutExtension = "" with get, set
    member val CurrentArgs = [||] with get, set
    
    [<ParamsSource(nameof Unchecked.defaultof<RandomGraphBenchmarks>.ValuesForNumEvents)>]
    member val NumEvents = 0 with get, set
    [<ParamsSource(nameof Unchecked.defaultof<RandomGraphBenchmarks>.ValuesForNumRoles)>]
    member val NumRoles = 0 with get, set
    [<ParamsSource(nameof Unchecked.defaultof<RandomGraphBenchmarks>.ValuesForNumRelations)>]
    member val NumRelations = 0 with get, set

    [<GlobalSetup(Target = nameof Unchecked.defaultof<RandomGraphBenchmarks>.TestPipelineThroughCLI)>]
    member this.GlobalSetup () =
        let gCtx = {
            NumEvents = this.NumEvents;
            NumRoles = this.NumRoles;
            NumRels = this.NumRelations
        }
        let dirFileName = sprintf "e%dro%dre%d_%s" this.NumEvents this.NumRoles this.NumRelations ((Guid.NewGuid()).ToString("N").Substring(0, 8))
        let dirPath = Path.Join(__SOURCE_DIRECTORY__, "benchmarkTestFiles", dirFileName)
        let filePathWithoutExtension = Path.Join(dirPath, dirFileName)
        Console.WriteLine(sprintf "[GlobalSetup] Generating files for %s" filePathWithoutExtension)
        MakeTestFileRandomGraph filePathWithoutExtension gCtx
        this.CurrentTestFilesDirPath <- dirPath
        this.CurrentTestFilePathWithoutExtension <- filePathWithoutExtension

    [<IterationSetup(Target = nameof Unchecked.defaultof<RandomGraphBenchmarks>.TestPipelineThroughCLI)>]
    member this.IterationSetup () = 
        Console.WriteLine("[IterationSetup] Setting current args")
        this.CurrentArgs <- [|   "--flatten"; 
                "--codegen";
                "--force";
                "true"; 
                "--output"; 
                Path.Join(this.CurrentTestFilesDirPath, "gen");
                (sprintf "%s.json" this.CurrentTestFilePathWithoutExtension)|]



type SingleEventWithMultipleReceiversBenchmarks() =
    [<Benchmark>]
    member this.TestDCRGraphDtoToG () =
        Console.WriteLine(sprintf "[DCRGraphTo] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO |> ignore

    [<Benchmark>]
    member this.TestIsDeadlockFree () =
        Console.WriteLine(sprintf "[IsDeadlockFree] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        isDeadlockFree this.g |> ignore

    [<Benchmark>]
    member this.TestIsStronglyDeadlockFree () =
        Console.WriteLine(sprintf "[IsStronglyDeadlockFree] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        isStronglyDeadlockFree this.g |> ignore

    [<Benchmark>]
    member this.TestEndpointProjectGraph () =
        Console.WriteLine(sprintf "[EndpointProjectGraph] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        endpointProjectGraph this.g true |> ignore

    //[<Benchmark>]
    //member this.TestGenerateEverything () =
    //    Console.WriteLine(sprintf "[GenerateEverything] Running with %s!" this.CurrentTestFilePathWithoutExtension)
    //    GenerateEverything this.g this.ePP this.outputPath true |>  ignore

    //[<Benchmark>]
    //member this.TestPipelineThroughCLI () =
    //    Console.WriteLine(sprintf "[TestPipelineThroughCLI] Running with %s!" this.CurrentTestFilePathWithoutExtension)
    //    CLIMain.main this.CurrentArgs


    member val ValuesForNumReceivers = seq { for i in 1 .. 20 do yield int (i * 5) } // 4, 8, 16, 32, 64

    member val CurrentTestFilesDirPath = "" with get, set
    member val CurrentTestFilePathWithoutExtension = "" with get, set
    member val inputPath = "" with get, set
    member val outputPath = "" with get, set
    member val CurrentArgs = [||] with get, set
    
    member val g: G = G.Empty with get, set
    member val ePP: EndpointProjections = Map.empty with get, set
    
    [<ParamsSource(nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.ValuesForNumReceivers)>]
    member val NumReceivers = 1 with get, set

    [<GlobalSetup(Targets = [|
        nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.TestDCRGraphDtoToG;
        nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.TestIsDeadlockFree;
        nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.TestIsStronglyDeadlockFree;
        nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.TestEndpointProjectGraph;
        //nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.TestGenerateEverything;
        //nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.TestPipelineThroughCLI;
        |])>]
    member this.GlobalSetup () =
        let gCtx: SingleEventGenerationContext = {
            NumRoles = this.NumReceivers;
        }
        let dirFileName = sprintf "receivers%d_%s" this.NumReceivers ((Guid.NewGuid()).ToString("N").Substring(0, 8))
        let dirPath = Path.Join(__SOURCE_DIRECTORY__, "SingleEventMultipleReceiversBenchmarkTestFiles", dirFileName)
        let filePathWithoutExtension = Path.Join(dirPath, dirFileName)
        Console.WriteLine(sprintf "[GlobalSetup] Generating files for %s" filePathWithoutExtension)
        MakeTestFileSingleEventMultipleRoles filePathWithoutExtension gCtx
        this.CurrentTestFilesDirPath <- dirPath
        this.CurrentTestFilePathWithoutExtension <- filePathWithoutExtension
        this.inputPath <- sprintf "%s.json" filePathWithoutExtension
        this.outputPath <- Path.Join(dirPath, "gen")

    [<IterationSetup(Target = nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.TestIsDeadlockFree)>]
    member this.DeadLockTestIterationSetup () = 
        let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
        this.g <- dcrGraphDTO |> convertDCRGraphDTOToG

    [<IterationSetup(Target = nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.TestIsStronglyDeadlockFree)>]
    member this.StronglyDeadLockTestIterationSetup () = 
        let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
        this.g <- dcrGraphDTO |> convertDCRGraphDTOToG

    [<IterationSetup(Target = nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.TestEndpointProjectGraph)>]
    member this.EndpointProjectTestIterationSetup () = 
        let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
        this.g <- dcrGraphDTO |> convertDCRGraphDTOToG

    //[<IterationSetup(Target = nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.TestGenerateEverything)>]
    //member this.GenerateEverythingTestIterationSetup () = 
    //    let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
    //    this.g <- dcrGraphDTO |> convertDCRGraphDTOToG
    //    this.ePP <- endpointProjectGraph this.g true

    //[<IterationSetup(Target = nameof Unchecked.defaultof<SingleEventWithMultipleReceiversBenchmarks>.TestPipelineThroughCLI)>]
    //member this.WholePipelineTestIterationSetup () = 
    //    Console.WriteLine("[IterationSetup] Setting current args")
    //    this.CurrentArgs <- [|   "--flatten"; 
    //            "--codegen";
    //            "--force";
    //            "true"; 
    //            "--output";
    //            "--overwrite";
    //            "true";
    //            this.outputPath;
    //            this.inputPath|]




type IncreasingEventsWithMultipleReceiversBenchmarks() =
    [<Benchmark>]
    member this.TestDCRGraphDtoToG () =
        Console.WriteLine(sprintf "[DCRGraphTo] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO |> ignore

    [<Benchmark>]
    member this.TestIsDeadlockFree () =
        Console.WriteLine(sprintf "[IsDeadlockFree] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        isDeadlockFree this.g |> ignore

    [<Benchmark>]
    member this.TestIsStronglyDeadlockFree () =
        Console.WriteLine(sprintf "[IsStronglyDeadlockFree] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        isStronglyDeadlockFree this.g |> ignore

    [<Benchmark>]
    member this.TestEndpointProjectGraph () =
        Console.WriteLine(sprintf "[EndpointProjectGraph] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        endpointProjectGraph this.g true |> ignore

    //[<Benchmark>]
    //member this.TestGenerateEverything () =
    //    Console.WriteLine(sprintf "[GenerateEverything] Running with %s!" this.CurrentTestFilePathWithoutExtension)
    //    GenerateEverything this.g this.ePP this.outputPath true |>  ignore

    //[<Benchmark>]
    //member this.TestPipelineThroughCLI () =
    //    Console.WriteLine(sprintf "[TestPipelineThroughCLI] Running with %s!" this.CurrentTestFilePathWithoutExtension)
    //    CLIMain.main this.CurrentArgs

    member val ValuesForNumReceivers = seq { for i in 1 .. 8 do yield int (i * 2) } // 4, 8, 16, 32, 64

    member val CurrentTestFilesDirPath = "" with get, set
    member val CurrentTestFilePathWithoutExtension = "" with get, set
    member val inputPath = "" with get, set
    member val outputPath = "" with get, set
    member val CurrentArgs = [||] with get, set
    
    member val g: G = G.Empty with get, set
    member val ePP: EndpointProjections = Map.empty with get, set
    
    [<ParamsSource(nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.ValuesForNumReceivers)>]
    member val NumReceivers = 1 with get, set

    [<GlobalSetup(Targets = [|
        nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.TestDCRGraphDtoToG;
        nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.TestIsDeadlockFree;
        nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.TestIsStronglyDeadlockFree;
        nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.TestEndpointProjectGraph;
        //nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.TestGenerateEverything;
        //nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.TestPipelineThroughCLI;
        |])>]
    member this.GlobalSetup () =
        let gCtx: EventsGenerationContext = {
            NumRoles = this.NumReceivers;
        }
        let dirFileName = sprintf "receivers%d_%s" this.NumReceivers ((Guid.NewGuid()).ToString("N").Substring(0, 8))
        let dirPath = Path.Join(__SOURCE_DIRECTORY__, "IncreasingEventsWithMultipleReceiversBenchmarksTestFiles", dirFileName)
        let filePathWithoutExtension = Path.Join(dirPath, dirFileName)
        Console.WriteLine(sprintf "[GlobalSetup] Generating files for %s" filePathWithoutExtension)
        MakeTestFileIncreasingEventsAndRoles filePathWithoutExtension gCtx
        this.CurrentTestFilesDirPath <- dirPath
        this.CurrentTestFilePathWithoutExtension <- filePathWithoutExtension
        this.inputPath <- sprintf "%s.json" filePathWithoutExtension
        this.outputPath <- Path.Join(dirPath, "gen")

    [<IterationSetup(Target = nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.TestIsDeadlockFree)>]
    member this.DeadLockTestIterationSetup () = 
        let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
        this.g <- dcrGraphDTO |> convertDCRGraphDTOToG

    [<IterationSetup(Target = nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.TestIsStronglyDeadlockFree)>]
    member this.StronglyDeadLockTestIterationSetup () = 
        let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
        this.g <- dcrGraphDTO |> convertDCRGraphDTOToG

    [<IterationSetup(Target = nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.TestEndpointProjectGraph)>]
    member this.EndpointProjectTestIterationSetup () = 
        let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
        this.g <- dcrGraphDTO |> convertDCRGraphDTOToG

    //[<IterationSetup(Target = nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.TestGenerateEverything)>]
    //member this.GenerateEverythingTestIterationSetup () = 
    //    let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
    //    this.g <- dcrGraphDTO |> convertDCRGraphDTOToG
    //    this.ePP <- endpointProjectGraph this.g true

    //[<IterationSetup(Target = nameof Unchecked.defaultof<IncreasingEventsWithMultipleReceiversBenchmarks>.TestPipelineThroughCLI)>]
    //member this.WholePipelineTestIterationSetup () = 
    //    Console.WriteLine("[IterationSetup] Setting current args")
    //    this.CurrentArgs <- [|   "--flatten"; 
    //            "--codegen";
    //            "--force";
    //            "true"; 
    //            "--output"; 
    //            this.outputPath;
    //            this.inputPath|]


type EventsWithConditionRelationsBenchmarks() =
    [<Benchmark>]
    member this.TestDCRGraphDtoToG () =
        Console.WriteLine(sprintf "[DCRGraphTo] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO |> ignore

    [<Benchmark>]
    member this.TestIsDeadlockFree () =
        Console.WriteLine(sprintf "[IsDeadlockFree] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        isDeadlockFree this.g |> ignore

    [<Benchmark>]
    member this.TestIsStronglyDeadlockFree () =
        Console.WriteLine(sprintf "[IsStronglyDeadlockFree] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        isStronglyDeadlockFree this.g |> ignore

    [<Benchmark>]
    member this.TestEndpointProjectGraph () =
        Console.WriteLine(sprintf "[EndpointProjectGraph] Running with %s!" this.CurrentTestFilePathWithoutExtension)
        endpointProjectGraph this.g true |> ignore

    //[<Benchmark>]
    //member this.TestGenerateEverything () =
    //    Console.WriteLine(sprintf "[GenerateEverything] Running with %s!" this.CurrentTestFilePathWithoutExtension)
    //    GenerateEverything this.g this.ePP this.outputPath true |>  ignore

    //[<Benchmark>]
    //member this.TestPipelineThroughCLI () =
    //    Console.WriteLine(sprintf "[TestPipelineThroughCLI] Running with %s!" this.CurrentTestFilePathWithoutExtension)
    //    CLIMain.main this.CurrentArgs

    member val ValuesForNumRoles = seq { for i in 1 .. 7 do yield int (i * 2) } // 4, 8, 16, 32, 64

    // Condition, Response, Milestone, Include, Exclude
    member val ValueForRelationTypes = [|
        "Condition";
        "Response";
        "Milestone";
        "Inclusion";
        "Exclusion";
        |]

    member val CurrentTestFilesDirPath = "" with get, set
    member val CurrentTestFilePathWithoutExtension = "" with get, set
    member val inputPath = "" with get, set
    member val outputPath = "" with get, set
    member val CurrentArgs = [||] with get, set
    
    member val g: G = G.Empty with get, set
    member val ePP: EndpointProjections = Map.empty with get, set
    
    [<ParamsSource(nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.ValuesForNumRoles)>]
    member val NumRoles = 1 with get, set

    [<ParamsSource(nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.ValueForRelationTypes)>]
    member val RelationType = "Condition" with get, set

    [<GlobalSetup(Targets = [|
        nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.TestDCRGraphDtoToG;
        nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.TestIsDeadlockFree;
        nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.TestIsStronglyDeadlockFree;
        nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.TestEndpointProjectGraph;
        //nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.TestGenerateEverything;
        //nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.TestPipelineThroughCLI;
        |])>]
    member this.GlobalSetup () =
        let gCtx = {
            NumRoles = this.NumRoles;
            RelationType = this.RelationType
        }
        let dirFileName = sprintf "receivers%d_%s_%s" this.NumRoles this.RelationType ((Guid.NewGuid()).ToString("N").Substring(0, 8))
        let dirPath = Path.Join(__SOURCE_DIRECTORY__, "EventsWithConditionRelationsBenchmarksTestFiles", dirFileName)
        let filePathWithoutExtension = Path.Join(dirPath, dirFileName)
        Console.WriteLine(sprintf "[GlobalSetup] Generating files for %s" filePathWithoutExtension)
        MakeTestFileIncreasingEventsAndRolesWithRelation filePathWithoutExtension gCtx
        this.CurrentTestFilesDirPath <- dirPath
        this.CurrentTestFilePathWithoutExtension <- filePathWithoutExtension
        this.inputPath <- sprintf "%s.json" filePathWithoutExtension
        this.outputPath <- Path.Join(dirPath, "gen")

    [<IterationSetup(Target = nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.TestIsDeadlockFree)>]
    member this.DeadLockTestIterationSetup () = 
        let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
        this.g <- dcrGraphDTO |> convertDCRGraphDTOToG

    [<IterationSetup(Target = nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.TestIsStronglyDeadlockFree)>]
    member this.StronglyDeadLockTestIterationSetup () = 
        let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
        this.g <- dcrGraphDTO |> convertDCRGraphDTOToG

    [<IterationSetup(Target = nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.TestEndpointProjectGraph)>]
    member this.EndpointProjectTestIterationSetup () = 
        let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
        this.g <- dcrGraphDTO |> convertDCRGraphDTOToG

    //[<IterationSetup(Target = nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.TestGenerateEverything)>]
    //member this.GenerateEverythingTestIterationSetup () = 
    //    let dcrGraphDTO = this.inputPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO
    //    this.g <- dcrGraphDTO |> convertDCRGraphDTOToG
    //    this.ePP <- endpointProjectGraph this.g true

    //[<IterationSetup(Target = nameof Unchecked.defaultof<EventsWithConditionRelationsBenchmarks>.TestPipelineThroughCLI)>]
    //member this.WholePipelineTestIterationSetup () = 
    //    Console.WriteLine("[IterationSetup] Setting current args")
    //    this.CurrentArgs <- [|   "--flatten"; 
    //            "--codegen";
    //            "--force";
    //            "true"; 
    //            "--output"; 
    //            this.outputPath;
    //            this.inputPath|]






let config = DefaultConfig.Instance
                .AddJob(Job.Default
                    .WithWarmupCount(0)
                    .WithIterationCount(3)
                    .WithInvocationCount(1)
                    .WithLaunchCount(1)
                    .WithUnrollFactor(1))
                .AddExporter(BenchmarkReportExporter.Default)
                .AddExporter(CsvExporter.Default)
                .AddExporter(CsvMeasurementsExporter.Default)
                .AddColumn(StatisticColumn.AllStatistics)

//BenchmarkRunner.Run<RandomGraphBenchmarks>(config) |> ignore
//BenchmarkRunner.Run<SingleEventWithMultipleReceiversBenchmarks>(config) |> ignore
//BenchmarkRunner.Run<IncreasingEventsWithMultipleReceiversBenchmarks>(config) |> ignore
BenchmarkRunner.Run<EventsWithConditionRelationsBenchmarks>(config) |> ignore
