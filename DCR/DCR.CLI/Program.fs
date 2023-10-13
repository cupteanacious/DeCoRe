// Copyright (c) Faizan Yousaf, Tobias Bonnesen. All Rights Reserved.

namespace DCR.CLI

module Main =
    open DCRGraph.DCRGraphDTO
    open DCRGraph.GraphvizUtils
    open DCRGraph.GraphvizTypes
    open DCRGraph.DCRMapping
    open DCRGraph.State
    open DCRGraph.EndpointProjection
    open Shared.FileUtils
    open DCR2Jolie.EndpointCodegen
    open Argu
    open System

    [<UniqueAttribute>]
    type CliArguments =
        | [<CliPrefix(CliPrefix.None); Mandatory; MainCommandAttribute; LastAttribute>] DCR_File of DCR_File: string
        | [<AltCommandLine("-f")>] Flatten
        | [<AltCommandLine("-c")>] Codegen
        | [<AltCommandLine("-d")>] Dot
        | Force of bool
        | [<AltCommandLine("-v")>] Verbose
        | [<AltCommandLine("-ow")>] Overwrite
        | [<AltCommandLine("-o")>] Output of path: string

        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Codegen -> "Generate Jolie microservice code for endpoints."
                | Dot -> "Generate DOT code for use with Graphviz."
                | DCR_File _-> "Path to DCR Choreography JSON file."
                | Flatten -> "Flatten the DCR Choreography nestings."
                | Output _ -> "Path of where to output generated code."
                | Force _ -> "Force generation, disregarding deadlocks and non-endpoint projectability."
                | Verbose _ -> "Output information."
                | Overwrite _ -> "Overwrite existing JavaService files inside the output folder."


    [<EntryPoint>]
    let main (args: string array) =
        // Only use Argu to check if arguments are correctly supplied and if they are not, then print usage help.
        let parser: ArgumentParser<CliArguments> =
            ArgumentParser.Create<CliArguments>(programName = "dcr_compiler.exe")

        let usage: string = parser.PrintUsage()
        
        let printUsageError (message: string) =
            sprintf "%s\n\n%s" message usage

        let result: ParseResults<CliArguments> = parser.Parse args
        let inputPath =
            match result.TryGetResult DCR_File with
            | Some path -> path
            | None -> failwith (printUsageError "No input file specified, nothing to do.")

        if ((result.Contains Codegen) && not (result.Contains Output)) then
            failwith (printUsageError "Output path missing.")

        let dcrGraphDTO = 
            let dcrGraphDTO = inputPath |> jsonToDCRGraphDTO
            match result.Contains Flatten with
                | true -> dcrGraphDTO |> flattenDCRGraphDTO
                | false -> dcrGraphDTO

        if result.Contains Dot then
            let graphvizFigure: GraphvizFigure = DCRGraphToGraphviz dcrGraphDTO

            if result.Contains Output then
                let outputPath =
                    match result.TryGetResult Output with 
                    | Some path -> path
                    | None -> failwith (printUsageError "Something went wrong with getting the output path.") // should not happen.

                let filename = sprintf "%s.%s" (System.IO.Path.GetFileNameWithoutExtension(inputPath)) "dot"
                writeToFile (System.IO.Path.Join(outputPath, "graphviz", filename)) graphvizFigure.Codegen
            else
                System.Console.WriteLine(graphvizFigure.Codegen)

        if result.Contains Codegen then
            let outputPath =
                match result.TryGetResult Output with 
                | Some path -> path
                | None -> failwith (printUsageError "Something went wrong with getting the output path.") // should not happen.
            let g = dcrGraphDTO |> convertDCRGraphDTOToG

            let force = result.GetResult (Force, false)
            let mutable errMsgs = Set.empty
            if not (isStronglyDeadlockFree g) && not force then
                errMsgs <- errMsgs.Add "There is a potential strong deadlock in the graph (supply --force flag to ignore)"
            if not (isDeadlockFree g) && not force then
                errMsgs <- errMsgs.Add "There is a potential deadlock in the graph (supply --force flag to ignore)"

            if errMsgs.Count > 0 then
                failwith (sprintf "%s\nAborting.." (String.concat "\n" errMsgs))

            let ePP = endpointProjectGraph g force
            if result.Contains Verbose then
                System.Console.WriteLine("Generating Jolie services for the following endpoints:")
                printEndpointProjections ePP
            
            let path = System.IO.Path.Join(outputPath, "services")

            let overwrite = result.Contains Overwrite

            GenerateEverything g ePP path overwrite |>  ignore

        Console.WriteLine(sprintf "Finished")
        0
