namespace DCRGraph

module DCRGraphDTO =
    open Shared
    open System.IO
    open Microsoft.FSharp.Collections

    let schemaPath = Path.Join(__SOURCE_DIRECTORY__, "dcr-schema.json")

    // Define F# types to represent the JSON Schema.
    type DcrEventDTO =
        { Id: string
          Initiator: string
          Action: string
          Receivers: string list }

    type DcrNestingDTO =
        { Id: string
          Events: string list
          ChildNestings: DcrNestingDTO list }

    type DcrRelationDTO = { RelationType: string }

    type DcrRelationshipDTO =
        { From: string
          To: string
          Relations: DcrRelationDTO list }

    type DcrMarkingDTO =
        { Executed: bool
          Included: bool
          Pending: bool }

    type DcrMarkingsDTO =
        { EventId: string; Marking: DcrMarkingDTO }

    type DCRGraphDTO =
        { Events: DcrEventDTO list
          Nestings: DcrNestingDTO list
          Relationships: DcrRelationshipDTO list
          Markings: DcrMarkingsDTO list }
    
    let totalNumberOfRelations (dcrGraphDTO: DCRGraphDTO) =
            dcrGraphDTO.Relationships
            |> List.map (fun r -> r.Relations.Length)
            |> List.sum

    let mapUnion map1 map2 =
        Map.fold (fun acc k v -> acc |> Map.add k v) map1 map2

    type DcrNamed =
        | DcrEventDTO of DcrEventDTO
        | DcrNestingDTO of DcrNestingDTO

    let rec getNesting (nesting: DcrNestingDTO) : Map<string, DcrNamed> =
        //System.Console.WriteLine(sprintf "nesting: %A" nesting.Id)
        let mutable symbolTable: Map<string, DcrNamed> =
            Map.ofList [ (nesting.Id, DcrNestingDTO(nesting)) ]

        symbolTable <-
            nesting.ChildNestings
            |> List.fold (fun (acc: Map<string, DcrNamed>) (n: DcrNestingDTO) -> mapUnion acc (getNesting n)) symbolTable

        symbolTable

    let getSymbolTable (graph: DCRGraphDTO) : Map<string, DcrNamed> =
        let mutable symbolTable: Map<string, DcrNamed> = Map.empty

        symbolTable <-
            graph.Nestings
            |> List.fold (fun (acc: Map<string, DcrNamed>) (n: DcrNestingDTO) -> mapUnion acc (getNesting n)) symbolTable

        symbolTable <-
            graph.Events
            |> List.fold (fun (acc: Map<string, DcrNamed>) (e: DcrEventDTO) -> acc.Add(e.Id, DcrEventDTO(e))) symbolTable

        symbolTable

    let jsonToDCRGraphDTO (filePath: string) : DCRGraphDTO =
        let schema: Newtonsoft.Json.Schema.JSchema = JsonUtils.parseJsonSchema schemaPath
        let jsonObject: Newtonsoft.Json.Linq.JObject = JsonUtils.parseJson filePath

        match JsonUtils.checkJsonAdheresToSchema schema jsonObject with
        | Ok(jObject: Newtonsoft.Json.Linq.JObject) -> jObject.ToObject<DCRGraphDTO>()
        | Error(message: string) -> failwith message

    // Flattens list of nestings and returns a list that include the nestings and all of their child nestings
    let rec flattenNestings (nestings: DcrNestingDTO list) : DcrNestingDTO list =
        let flattenNesting (nesting: DcrNestingDTO) : DcrNestingDTO list =
            let childNestings = flattenNestings nesting.ChildNestings
            nesting :: childNestings

        List.concat (List.map flattenNesting nestings)

    // Flattens a single nesting and returns all of its events
    let rec flattenNestingEvents (nesting: DcrNestingDTO) : string list =
        let eventsFromChildNestings =
            List.collect flattenNestingEvents nesting.ChildNestings

        List.concat [ nesting.Events; eventsFromChildNestings ]

    // Flattens all the relationships in the graph
    let flattenRelationships (graph: DCRGraphDTO) : DcrRelationshipDTO list =
        let flattenedNestings = flattenNestings graph.Nestings

        let applyRelationshipToNesting (relationship: DcrRelationshipDTO) =
            let fromNesting = List.tryFind (fun x -> x.Id = relationship.From) flattenedNestings
            let toNesting = List.tryFind (fun x -> x.Id = relationship.To) flattenedNestings

            match (fromNesting, toNesting) with
            | (Some fn, Some tn) ->
                List.collect
                    (fun fromId ->
                        List.map
                            (fun toId ->
                                { From = fromId
                                  To = toId
                                  Relations = relationship.Relations })
                            (flattenNestingEvents tn))
                    (flattenNestingEvents fn)
            | (Some fn, None) ->
                List.map
                    (fun fromId ->
                        { From = fromId
                          To = relationship.To
                          Relations = relationship.Relations })
                    (flattenNestingEvents fn)
            | (None, Some tn) ->
                List.map
                    (fun toId ->
                        { From = relationship.From
                          To = toId
                          Relations = relationship.Relations })
                    (flattenNestingEvents tn)
            | (None, None) -> [ relationship ]

        List.collect applyRelationshipToNesting graph.Relationships

    // Generate a new graph without nestings
    let flattenDCRGraphDTO (graph: DCRGraphDTO) : DCRGraphDTO =
        let flattenedRelationships = flattenRelationships graph

        { Events = graph.Events
          Nestings = []
          Relationships = flattenedRelationships
          Markings = graph.Markings }
