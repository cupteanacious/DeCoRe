namespace StateManager

open DCRGraph.DCRGraphDTO
open DCRGraph.DCRMapping
open DCRGraph.DCRGraph
open DCRGraph.State
open Shared
open System

type IStateManager = 
    abstract member useGFromPath: string -> string -> unit
    abstract member initializeFromChoreo: string -> string -> unit
    abstract member canExecute: string -> string -> bool
    abstract member execute: string -> string -> unit
    abstract member getState: string -> GraphState

type StateManager() =
    // create a map of roles to graphs
    member val roleGraphMap: Map<string, G> = Map.empty with get, set

    interface IStateManager with
        member this.useGFromPath (role: string) (path: string) =
            let schemaPath: string = "./GSchema.json"

            let jsonToGGraph (filePath: string) : G =
                let schema: Newtonsoft.Json.Schema.JSchema = JsonUtils.parseJsonSchema schemaPath
                let jsonObject: Newtonsoft.Json.Linq.JObject = JsonUtils.parseJson filePath
            
                match JsonUtils.checkJsonAdheresToSchema schema jsonObject with
                | Ok(jObject: Newtonsoft.Json.Linq.JObject) -> jObject.ToObject<G>()
                | Error(message: string) -> failwith message

            let graph: G = jsonToGGraph path
            this.roleGraphMap <- Map.add role graph this.roleGraphMap

        member this.initializeFromChoreo (role: string) (path: string) =
            let graph: G = path |> jsonToDCRGraphDTO |> flattenDCRGraphDTO |> convertDCRGraphDTOToG

            this.roleGraphMap <- Map.add role graph this.roleGraphMap

        member this.canExecute (role: string) (id: string): bool =
            match this.roleGraphMap.TryFind(role) with
                | Some(graph: G) -> canExecuteEvent id graph
                | None -> failwith (sprintf "Role %s not found" role)

        member this.execute (role: string) (id: string) =
            match this.roleGraphMap.TryFind(role) with
                | Some(graph: G) -> this.roleGraphMap <- Map.add role (execute id graph) this.roleGraphMap
                | None -> failwith (sprintf "Role %s not found" role)

        member this.getState (role: string) : GraphState =
            match this.roleGraphMap.TryFind(role) with
                | Some(graph: G) -> getCurrentState graph
                | None -> failwith "Role not found"