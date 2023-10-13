// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"

open System
open System.IO

open Benchmark.RandomGraphGenerator
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


module CLIMain = DCR.CLI.Main

let SRCDIR = __SOURCE_DIRECTORY__

type Benchmarks() =
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

    member val ValuesForNumRoles = seq { for i in 2 .. 6 do yield int (2.0**i) } // 4, 8, 16, 32, 64
    member val ValuesForNumEvents = seq { for i in 2 .. 6 do yield int (2.0**i) } // 4, 8, 16, 32, 64
    member val ValuesForNumRelations = seq { for i in 2 .. 6 do yield int (2.0**i) } // 4, 8, 16, 32, 64

    member val CurrentTestFilesDirPath = "" with get, set
    member val CurrentTestFilePathWithoutExtension = "" with get, set
    member val CurrentArgs = [||] with get, set
    
    [<ParamsSource(nameof Unchecked.defaultof<Benchmarks>.ValuesForNumEvents)>]
    member val NumEvents = 0 with get, set
    [<ParamsSource(nameof Unchecked.defaultof<Benchmarks>.ValuesForNumRoles)>]
    member val NumRoles = 0 with get, set
    [<ParamsSource(nameof Unchecked.defaultof<Benchmarks>.ValuesForNumRelations)>]
    member val NumRelations = 0 with get, set

    [<GlobalSetup(Target = nameof Unchecked.defaultof<Benchmarks>.TestPipelineThroughCLI)>]
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
        MakeTestFile filePathWithoutExtension gCtx
        this.CurrentTestFilesDirPath <- dirPath
        this.CurrentTestFilePathWithoutExtension <- filePathWithoutExtension

    [<IterationSetup(Target = nameof Unchecked.defaultof<Benchmarks>.TestPipelineThroughCLI)>]
    member this.IterationSetup () = 
        Console.WriteLine("[IterationSetup] Setting current args")
        this.CurrentArgs <- [|   "--flatten"; 
                "--codegen";
                "--force";
                "true"; 
                "--output"; 
                Path.Join(this.CurrentTestFilesDirPath, "gen");
                (sprintf "%s.json" this.CurrentTestFilePathWithoutExtension)|]

let config = DefaultConfig.Instance
                .AddJob(Job.Default
                    .WithWarmupCount(0)
                    .WithIterationCount(3)
                    .WithInvocationCount(1)
                    .WithLaunchCount(1)
                    .WithUnrollFactor(1))
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddExporter(RPlotExporter.Default)
                .AddExporter(HtmlExporter.Default)
                .AddExporter(MarkdownExporter.GitHub)
                .AddExporter(MarkdownExporter.StackOverflow)
                .AddExporter(BenchmarkReportExporter.Default)
                .AddExporter(CsvExporter.Default)
                .AddExporter(CsvMeasurementsExporter.Default)
                .AddColumn(StatisticColumn.AllStatistics)
                .AddDiagnoser(EventPipeProfiler.Default)

BenchmarkRunner.Run<Benchmarks>(config) |> ignore