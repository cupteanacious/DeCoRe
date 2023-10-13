// Copyright (c) Faizan Yousaf, Tobias Bonnesen.  All Rights Reserved.

namespace DCR2Jolie

open System
open System.IO
open System.IO.Compression
open System.Diagnostics

module Utils =
    let indent (str: string) (level: int) : string =
        let indentation = String.replicate (level * 4) " "
        let output = indentation + str.Replace("\n", "\n" + indentation)
        output
    
    let adjustBlockForNewLine (block: string) : string =
        match block = "" with
        | false -> block + "\n\n"
        | true -> block
    
    type Context = 
        {
            IndentLevel: int
        }
        member this.IncrementIndentLevel =
            { this with IndentLevel = this.IndentLevel + 1; }
        member this.Indent (input: string) = 
            indent input this.IndentLevel

    let inline GenerateCodegenList (lst: ^T list) (ctx: Context) (sep: string) : string =
        match lst.IsEmpty with
        | false -> lst
                |> List.map (fun elm -> (^T : (member Codegen : Context -> string) (elm, ctx))) // SRTP
                |> String.concat sep
        | true -> ""

    let runMaven (directory: string) (verbose: bool) =
        use _process = new Process()

        if verbose = false then
            _process.StartInfo.RedirectStandardOutput <- true

        match Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            Runtime.InteropServices.OSPlatform.Windows) with
        | true ->
            _process.StartInfo.FileName <- "cmd.exe"
            _process.StartInfo.Arguments <- "/c mvn package"
        | false ->
            _process.StartInfo.FileName <- "/bin/bash" 
            _process.StartInfo.Arguments <- "-c \"mvn package\""

        _process.StartInfo.WorkingDirectory <- directory
        
        _process.Start() |> ignore
        _process.WaitForExit()

    type CodeBlock =
        {
            String: string
        }
        member this.Codegen (ctx: Context) =
            ctx.Indent (sprintf "%s" this.String)
