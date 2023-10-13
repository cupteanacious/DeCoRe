// Copyright (c) Faizan Yousaf, Tobias Bonnesen. All Rights Reserved.

namespace DCRGraph

/// This module contains functions to convert from the DCRGraphDTO type of DCRCompiler.Frontend.DcrGraphDsl
/// to the DcrGraph type of DCRGraph.DcrGraphs. 
/// 
/// The difference between the two, is that DcrGraphDsl exists as an F# representation of the JSON DSL we have 
/// implemented, in which DCR Choreographies can be specified as input for our program. From DcrGraphDsl, Graphviz
/// output can be generated for use with the DOT engine, and other things.
/// 
/// DcrGraphs on the other hand, serves as an F# implementation of DCR graph as they appear in the research paper
/// Hildebrandt et al. “Declarative Choreographies and Liveness” 2019. Thus, it's purpose is to stay as close to 
/// those definitions as possible, and to actually simulate DCR Graphs. 
/// 
module DCRMapping =
    open DCRGraph
    open DCRGraphDTO

    type RelationAccumulator = Map<string, Set<Event * Event>>

    let emptyRelationAccumulator: RelationAccumulator =
        Map.empty<string, Set<Event * Event>>

    // add a marking to set Map<string, bool>
    let addExecutedMarkingToMap (acc: Map<string, bool>) (marking: DcrMarkingsDTO) : Map<string, bool> =
        acc.Add(marking.EventId, marking.Marking.Executed)

    let addResponseMarkingToMap (acc: Map<string, bool>) (marking: DcrMarkingsDTO) : Map<string, bool> =
        acc.Add(marking.EventId, marking.Marking.Pending)

    let addIncludedMarkingToMap (acc: Set<string>) (marking: DcrMarkingsDTO) : Set<string> =
        match marking.Marking.Included with
        | true -> acc.Add(marking.EventId)
        | false -> acc

    let convertDCRGraphDTOToG (dcrGraph: DCRGraphDTO) : G =
        match dcrGraph.Nestings.Length with
        | n when n > 0 ->
            failwith
                "This DCR Graph contains nestings, which do not exist in DCR Graphs. Use DCRCompiler.Frontend.DcrGraphs.flattenDCRGraphDTO to replace them."
        | _ -> true |> ignore

        let frontendEventToBackendEvent (event : DcrEventDTO) : Event =
            {Id = event.Id; Initiator = event.Initiator; Action = event.Action; Receivers = (Set.ofList event.Receivers) }

        let events: Set<Event> =
            dcrGraph.Events
            |> List.fold (fun (acc: Set<Event>) (e: DcrEventDTO) -> acc.Add (frontendEventToBackendEvent e)) Set.empty

        let rec getEventObjFromIdString (idString: string) (eventList: List<Event>) (originalEventList: List<Event>)  : Event =
            match eventList with
            | h :: t when h.Id = idString -> h
            | _ :: t -> getEventObjFromIdString idString t originalEventList
            | [] -> failwith (sprintf "Event Object does not exist in list of events with string ID %s. Most likely the JSON file references an event in the relationships section, which is not listed in the events section. Events found in events section (after conversion to G): %A" idString (List.map (fun (e: Event) -> e.Id) originalEventList))
            
        let foldFF (rels: DcrRelationshipDTO) =
            fun (acc: RelationAccumulator) (rel: DcrRelationDTO) ->
                let eventList = Set.toList events
                let event1 = getEventObjFromIdString rels.From eventList eventList
                let event2 = getEventObjFromIdString rels.To eventList eventList

                // If the RelationAccumulator has previously seen rel.RelationType,
                // it will already contain a set with string * string in it. 
                // Then we need to update the existing set in RelationAccumulator. 
                let relSet = Option.defaultValue Set.empty (acc.TryFind rel.RelationType)
                acc.Add(rel.RelationType, (relSet.Add((event1, event2))))

        let foldF (acc: RelationAccumulator) (rels: DcrRelationshipDTO) : RelationAccumulator =
            // { From: string; To: string; Relations: DcrRelation list }
            rels.Relations |> List.fold (foldFF (rels)) acc

        let relations: RelationAccumulator =
            dcrGraph.Relationships |> List.fold (foldF) emptyRelationAccumulator

        let exx : Map<string, bool> =
            dcrGraph.Markings |> List.fold (addExecutedMarkingToMap) Map.empty<string, bool>

        let ree : Map<string, bool> =
            dcrGraph.Markings |> List.fold (addResponseMarkingToMap) Map.empty<string, bool>

        let inn : Set<string> = dcrGraph.Markings |> List.fold (addIncludedMarkingToMap) Set.empty<string>

        let getRoleSetFromEvent (s: R) (e: Event) : Set<string> =
            let s = s.Add(e.Initiator)
            e.Receivers |> Set.fold (fun (acc: Set<string>) (r: string) -> acc.Add(r)) s

        let roleSet: R = events |> Set.fold getRoleSetFromEvent Set.empty

        { E = events
          M = (exx, ree, inn)
          Conditions = Option.defaultValue Set.empty (relations.TryFind "Condition")
          Responses = Option.defaultValue Set.empty (relations.TryFind "Response")
          Milestones = Option.defaultValue Set.empty (relations.TryFind "Milestone")
          Inclusions = Option.defaultValue Set.empty (relations.TryFind "Inclusion")
          Exclusions = Option.defaultValue Set.empty (relations.TryFind "Exclusion")
          Cancels = Set.empty 
          R = roleSet}

    let convertGtoDcrGraphDto (g: G) : DCRGraphDTO =
        let backendEventToFrontendEvent (event : Event) : DcrEventDTO =
            {Id = event.Id; Initiator = event.Initiator; Action = event.Action; Receivers = (Set.toList event.Receivers) }

        let events: List<DcrEventDTO> =
            g.E
            |> Set.toList
            |> List.map (backendEventToFrontendEvent)

        let convertGRelToDcrRel (relSet: RelationSet) (relType: string): DcrRelationshipDTO list =
            relSet
            |> Set.toList
            |> List.map (fun (e1, e2) -> { 
                From = e1.Id;
                To = e2.Id;
                Relations = [{ RelationType = relType }] 
                })

        let conditions: DcrRelationshipDTO list = convertGRelToDcrRel g.Conditions "Condition"
        let responses: DcrRelationshipDTO list = convertGRelToDcrRel g.Responses "Response"
        let milestones: DcrRelationshipDTO list = convertGRelToDcrRel g.Milestones "Milestone"
        let inclusions: DcrRelationshipDTO list = convertGRelToDcrRel g.Inclusions "Inclusion"
        let exclusions: DcrRelationshipDTO list = convertGRelToDcrRel g.Exclusions "Exclusion"
        let relationships: DcrRelationshipDTO list = conditions @ responses @ milestones @ inclusions @ exclusions

        let getMarkingForAnEvent (e: DcrEventDTO) (gM: M) : DcrMarkingsDTO =
            let (exx, ree, inn) = gM

            let isExecuted = Option.defaultValue false (exx.TryFind e.Id)
            let isPending = Option.defaultValue false (ree.TryFind e.Id)
            let isIncluded = inn.Contains e.Id

            { EventId = e.Id; Marking = { Executed = isExecuted; Pending = isPending; Included = isIncluded; }}

        let eventMarkings: DcrMarkingsDTO list = events |> List.map (fun (e: DcrEventDTO) -> getMarkingForAnEvent e g.M)

        { 
            Events = events
            Relationships = relationships
            Markings = eventMarkings
            Nestings = [] 
        }