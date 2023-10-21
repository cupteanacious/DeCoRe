namespace Benchmark


module TestFileGenerator =
    open DCRGraph.DCRGraphDTO
    open RandomGraphGenerator
    open SingleEventGraphGenerator
    open IncreasingEventGraphGenerator
    open EventWithRelationGraphGenerator
    open Shared.FileUtils
    open DCRGraph.GraphvizUtils
    open System.IO

    let MakeTestFile (filePathWithoutExtension: string) (graph: DCRGraphDTO) =
        writeToFile (sprintf "%s.dot"  filePathWithoutExtension) (graph |> DCRGraphToGraphviz).Codegen
        Shared.JsonUtils.exportAsJson (sprintf "%s.json" filePathWithoutExtension) graph
    
    let MakeTestFileRandomGraph (filePathWithoutExtension: string) (gCtx: GenerationContext) =
        let graph = generateRandomGraph gCtx
        MakeTestFile filePathWithoutExtension graph

    let MakeTestFileSingleEventMultipleRoles (filePathWithoutExtension: string) (gCtx: SingleEventGenerationContext) =
        let graph = generateGraphSingleEventMultipleRoles gCtx
        MakeTestFile filePathWithoutExtension graph

    let MakeTestFileIncreasingEventsAndRoles (filePathWithoutExtension: string) (gCtx: EventsGenerationContext) =
        let graph = generateGraphIncreasingEventsAndRoles gCtx
        MakeTestFile filePathWithoutExtension graph

    let MakeTestFileIncreasingEventsAndRolesWithRelation (filePathWithoutExtension: string) (gCtx: EventsWithRelationsGenerationContext) =
        let graph = generateGraphIncreasingEventsAndRolesWithRelation gCtx
        MakeTestFile filePathWithoutExtension graph