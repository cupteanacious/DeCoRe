// Copyright (c) Faizan Yousaf, Tobias Bonnesen. All Rights Reserved.

namespace Shared

module FileUtils =
    open System
    open System.IO

    let writeToFile (path: string) (text: string) =
        Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
        let fileStream = new FileStream(path, FileMode.Create)
        let streamWriter = new StreamWriter(fileStream)
        streamWriter.Write(text)
        streamWriter.Close()
        fileStream.Close()

    let zipDirectory (sourceDir: string) (targetFile: string) =
        if File.Exists(targetFile) then
            printfn "Overwriting old zip file: %s" targetFile
            File.Delete(targetFile)
        //System.IO.Compression.ZipFile.CreateFromDirectory(sourceDir, targetFile)

    let deleteDirectory (dirPath: string) =
        try
            if Directory.Exists(dirPath) then
                Directory.Delete(dirPath, true)
        with ex ->
            // write to console about the error
            Console.WriteLine(sprintf "While deleting the directory: %s" dirPath)
            Console.WriteLine(ex.Message)
