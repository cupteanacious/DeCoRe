namespace DCRGraph

module Utils =
    open DCRGraph
    open DCRGraphDTO
    open DCRGraph.DCRMapping

    type GLoader = 
        {
            basePath: string
        }
        member this.Load(filename: string) =
            jsonToDCRGraphDTO (System.IO.Path.Join(this.basePath, filename))
            |> flattenDCRGraphDTO
            |> convertDCRGraphDTOToG