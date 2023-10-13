namespace DCRGraph

module EndpointProjection =
    open System
    open State
    open DCRGraph

    type EndpointProjections = Map<Role, G>

    exception NotEndpointProjectable of string

    let isRParticipantOfE (eP: Event) (r: Role) : bool =
            eP.Initiator = r || Set.contains r eP.Receivers

    let getEventsWhereInitiator (r: string) ( E: E ) : E =
        E |> Set.filter (fun (e: Event) -> e.Initiator = r)

    let getEventsWhereReceiver (r: string) ( E: E ) : E =
        E |> Set.filter (fun (e: Event) -> e.Receivers.Contains r)

    let getEventsWhereRParticipant (r: string) (E: E) : E =
        E |> Set.filter (fun (e: Event) -> isRParticipantOfE e r)

    let isEndpointProjectable (r: string) (g: G) : Result<bool, string> =
            let eventsWithDirectDependencyToE (e: Event) =
                g.E
                |> Set.filter (fun (eP: Event) -> (isDirectDependency eP.Id e.Id g).value)

            let eventsRInitiate = getEventsWhereInitiator r g.E
            
            let mutable errors = ""
            let mutable result = true

            eventsRInitiate
            |> Set.iter (fun (e: Event) -> 
                eventsWithDirectDependencyToE e
                |> Set.iter (fun (eP: Event) -> (
                    if not (isRParticipantOfE eP r) then
                        errors <- errors + (sprintf "\n%s has a direct dependency to %s, but %s is NOT a participant in it!" eP.Id e.Id r)
                        result <- false
                )))
            
            match result with
            | true -> Ok true
            | false -> Error errors

    let isGraphEndpointProjectable (g: G) : Result<bool, string> =
        let roles: Set<Role> = g.R

        let mutable result = true
        let mutable errors = ""

        roles |> Set.iter (fun (r: Role) -> 
            try
                isEndpointProjectable r g |> ignore
            with (NotEndpointProjectable(innerErrors)) -> 
                errors <- errors + innerErrors
                result <- false
            )

        match result with
        | true -> Ok true
        | false -> Error errors

    let getExDelta (exx: Ex) (eDelta: E) : Ex =
        eDelta |> Set.fold (fun (acc: Map<string, bool>) (e: Event) -> acc.Add(e.Id, Option.defaultValue false (Map.tryFind e.Id exx)) ) Map.empty<string, bool>

    let getReDelta (ree: Re) (eDelta: E) : Re =
        eDelta |> Set.fold (fun (acc: Map<string, bool>) (e: Event) -> acc.Add(e.Id, Option.defaultValue false (Map.tryFind e.Id ree)) ) Map.empty<string, bool>
    
    let getInDelta (inn: In) (delta: E) (eDelta: E) (conditionDelta: RelationSet) (milestoneDelta: RelationSet) : In =
        // condition events for delta events
        let conditionEventsDelta = 
            conditionDelta |> Set.map (fun (source, _) -> source.Id)

        // milestone events for delta events
        let milestoneEventsDelta = 
            milestoneDelta |> Set.map (fun (source, _) -> source.Id)

        // delta events
        let deltaAsStrings = 
            delta |> Set.map (fun (e: Event) -> e.Id)

        let eDeltaAsStrings = 
            eDelta |> Set.map (fun (e: Event) -> e.Id)

        let eventSets = seq {
            yield conditionEventsDelta
            yield milestoneEventsDelta
            yield deltaAsStrings
        }

        let unionOfConditionsMilestonesDeltas = Set.unionMany eventSets

        let LHS = Set.intersect inn unionOfConditionsMilestonesDeltas

        let RHS = Set.difference eDeltaAsStrings unionOfConditionsMilestonesDeltas

        Set.union LHS RHS

    let getMDelta (g: G) (delta: E) (eDelta: E) (conditionDelta: RelationSet) (milestoneDelta: RelationSet) : M =
        let (exx, ree, inn) = g.M
        ((getExDelta exx eDelta), (getReDelta ree eDelta), (getInDelta inn delta eDelta conditionDelta milestoneDelta))     

    let getInnMPrime (g: G) (ePrime: E) =
        let (exx, ree, inn) = g.M
        let innAsE = Set.filter (fun (e: Event) -> Set.contains e.Id inn) g.E
        let InnMPrime: In = Set.map (fun (e: Event) -> e.Id) (Set.difference ePrime (Set.difference g.E innAsE))
        InnMPrime

    let getConditionsDelta (g: G) (eDelta: E) : RelationSet =
        g.Conditions 
            |> Set.filter (fun (_, sink) -> Set.exists (fun (e: Event) -> e.Id = sink.Id) eDelta)

    let getMilestonesDelta (g: G) (eDelta: E) : RelationSet =
        g.Milestones
            |> Set.filter (fun (_, sink) -> Set.exists (fun (e: Event) -> e.Id = sink.Id) eDelta)
    
    let getResponseDelta (g: G) (eDelta: E) (milestonesDelta: RelationSet) : RelationSet =
        let responseMilestonesDelta = 
            g.Responses |> Set.filter (fun (_, sink) -> Set.exists (fun (source, _) -> source = sink) milestonesDelta )
        
        let response = 
            g.Responses |> Set.filter (fun (_, sink) -> Set.exists (fun (e: Event) -> e.Id = sink.Id) eDelta)

        Set.union responseMilestonesDelta response

    let getInclusionDelta (g: G) (eDelta: E) (milestoneDelta: RelationSet) (conditionDelta: RelationSet): RelationSet =
        let inclusionConditionDelta = 
            g.Inclusions |> Set.filter (fun (_, sink) -> Set.exists (fun (source, _) -> source = sink) conditionDelta)

        let inclusionMilestoneDelta =
            g.Inclusions |> Set.filter (fun (_, sink) -> Set.exists (fun (source, _) -> source = sink) milestoneDelta)

        let inclusionDelta =
            g.Inclusions |> Set.filter (fun (_, sink) -> Set.exists (fun (e: Event) -> e.Id = sink.Id) eDelta)
        
        let sets = seq { yield inclusionConditionDelta; yield inclusionMilestoneDelta; yield inclusionDelta }
        Set.unionMany sets
    
    let getExclusionDelta (g: G) (eDelta: E) (milestoneDelta: RelationSet) (conditionDelta: RelationSet): RelationSet =
        let exclusionConditionDelta = 
            g.Exclusions |> Set.filter (fun (_, sink) -> Set.exists (fun (source, _) -> source = sink) conditionDelta)

        let exclusionMilestoneDelta =
            g.Exclusions |> Set.filter (fun (_, sink) -> Set.exists (fun (source, _) -> source = sink) milestoneDelta)

        let exclusionDelta =
            g.Exclusions |> Set.filter (fun (_, sink) -> Set.exists (fun (e: Event) -> e.Id = sink.Id) eDelta)
        
        let sets = seq { yield exclusionConditionDelta; yield exclusionMilestoneDelta; yield exclusionDelta }
        Set.unionMany sets

    let getEventsWithDirectDepedencyTo (e: Event) (delta: Set<Event>) (g: G) : Set<Event> =
        match Set.exists (fun (deltaEvent: Event) -> (isDirectDependency e.Id deltaEvent.Id g).value) delta with
        | true -> Set.empty.Add e
        | false -> Set.empty

    let getAllEventsWithDirectDependencyToEventSet (E: Set<Event>) (delta: Set<Event>) (g: G) : Set<Event> =
        Set.fold 
            (fun (eventSet: Set<Event>) (event: Event) -> Set.union eventSet (getEventsWithDirectDepedencyTo event delta g)) 
            Set.empty 
            E
    
    let (|Default|) defaultValue input =
        defaultArg input defaultValue

    let endpointProjectR (g: G) (r: Role) (force: bool): G =
        match isEndpointProjectable r g with
        | Ok b -> () |> ignore
        | Error e -> 
            match force with
            | false -> raise (NotEndpointProjectable(e))
            | true -> Console.WriteLine(sprintf "Warning: Graph not endpoint projectable, but force is true. Errors: %s" e)
        
        let delta = getEventsWhereInitiator r g.E
        
        let EDelta = getAllEventsWithDirectDependencyToEventSet g.E delta g

        let EPrime = getEventsWhereReceiver r g.E

        let conditionsDelta = getConditionsDelta g delta

        let milestonesDelta = getMilestonesDelta g delta

        let responseDelta = getResponseDelta g delta milestonesDelta
        
        let inclusionDelta = getInclusionDelta g delta milestonesDelta conditionsDelta
    
        let exclusionDelta = getExclusionDelta g delta milestonesDelta conditionsDelta

        let MDelta = getMDelta g delta EDelta conditionsDelta milestonesDelta

        let InnMPrime = getInnMPrime g EDelta
        
        let (exxMDelta, reeMDelta, innMDelta) = MDelta
        {
            E = Set.union EDelta EPrime
            M = (exxMDelta, reeMDelta, Set.union InnMPrime innMDelta)
            Conditions = conditionsDelta
            Milestones = milestonesDelta
            Responses = responseDelta
            Inclusions = inclusionDelta
            Exclusions = exclusionDelta
            Cancels = Set.empty
            R = Set.empty.Add(r)
        }

    let endpointProjectGraph (g: G) (force: bool) : EndpointProjections =
        let roles: Set<Role> = g.R

        let mutable errors = ""
        let mutable ePP = Map.empty

        roles 
        |> Set.iter (fun (r: Role) -> 
            try
                ePP <- ePP.Add(r, (endpointProjectR g r force))
            with (NotEndpointProjectable innerErrors) -> 
                errors <- errors + innerErrors
        )

        match errors.Length = 0 with
        | true -> ePP
        | false -> raise (NotEndpointProjectable(errors))
        
    let printEndpointProjections (ePP: EndpointProjections) =
        for (r, g) in Map.toList ePP do
            Console.WriteLine("=================================")
            Console.WriteLine(sprintf "Endpoint projections for %s" r)
            Console.WriteLine("=================================")
            
            let sending = Set.filter (fun (e: Event) -> e.Initiator = r ) g.E
            let receiving = Set.filter (fun (e: Event) -> e.Initiator <> r ) g.E

            let updateLabel (e: Event) (mode: string) = 
                let receivers = String.concat ", " e.Receivers
                { e with Id = sprintf "%s(%s, %s->(%s))" mode e.Action e.Initiator receivers }

            let sendingLabelsUpdated = Set.map (fun e -> updateLabel e "!") sending
            let receivingLabelsUpdated = Set.map (fun e -> updateLabel e "?") receiving

            for e in sendingLabelsUpdated do
                Console.WriteLine(e.Id)

            for e in receivingLabelsUpdated do
                Console.WriteLine(e.Id)
        
        Console.WriteLine("\n")