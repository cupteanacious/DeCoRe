namespace Benchmark

module TestFileGenerator =
    open RandomGraphGenerator
    open Shared.FileUtils
    open DCRGraph.GraphvizUtils
    open System.IO
    
    let MakeTestFile (filePathWithoutExtension: string) (gCtx: GenerationContext) =
        let graph = generateRandomGraph gCtx
        writeToFile (sprintf "%s.dot"  filePathWithoutExtension) (graph |> DCRGraphToGraphviz).Codegen
        Shared.JsonUtils.exportAsJson (sprintf "%s.json" filePathWithoutExtension) graph