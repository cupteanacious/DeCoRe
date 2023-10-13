// Copyright (c) Faizan Yousaf, Tobias Bonnesen. All Rights Reserved.

namespace DCRGraph

open DCRGraph

module State =
    open System
    
    type Answer<'V, 'M> = { value: 'V; msg: 'M }

    // Definition 4 (Event and time step enabling), modified.
    // Hildebrandt et al. 2019: Declarative Choreographies and Liveness, new version
    let isEnabled (e: string) (g: G) : Answer<bool, bool * bool * bool> =
        if not (Set.exists (fun (ev: Event) -> ev.Id = e) g.E) then
            failwith "Called isEnabled on event that does not exist in supplied graph."
        
        // e is enabled if all of the following are true.
        let exx, ree, inn = g.M

        // 1. e must be included.
        let one = inn.Contains e

        // 2. Events that are conditions for e, must have been executed
        let two =
            g.Conditions
            |> Set.filter (fun (_, sink) -> sink.Id = e) // find all events that have condition relation to e
            |> Set.forall (fun elm -> Option.defaultValue false (Map.tryFind ((fst elm).Id) exx))

        // 3. Events that are milestones for e, must not be pending.
        let three =
            g.Milestones
            |> Set.filter (fun (_, sink) -> sink.Id = e)
            |> Set.forall (fun (source, _) -> not (Option.defaultValue false (Map.tryFind source.Id ree)))

        // 4. e must not be enabled, if it has an inclusion relation to an event e' whose deadline has passed.
        // Not implemented yet. Requires Re to map to time.
        // 5. Not implemented yet.

        { value = (one && two && three)
          msg = (one, two, three) }

    let getEnabledEvents (g: G) : Set<Event> =
        g.E |> Set.filter (fun e -> (isEnabled e.Id g).value)

    // Definition 7.
    // Hildebrandt et al. 2019: Declarative Choreographies and Liveness, new version
    let isDirectDependency (e': string) (e: string) (g: G) : Answer<bool, bool * bool * bool * bool> =
        // There is a direct dependency between e' and e, if any of the following are true:
        // 1. If the events are the same
        let one = e' = e
        // 2. If there is any relation between them
        let two = g.allRelations |> Set.exists (fun (e1, e2) -> e1.Id = e' && e2.Id = e)
        // 3. If e' includes or excludes an event e'', which is a condition or milestone for e
        let three =
            Set.exists
                (fun (source, sink) ->
                    source.Id = e'
                    && (Set.union g.Conditions g.Milestones) |> Set.exists (fun (so, si) -> so.Id = sink.Id && si.Id = e))
                (Set.union g.Inclusions g.Exclusions)
        // If e' has a response relation to e'', and e'' is a milestone for e
        let four =
            g.Responses 
            |> Set.exists
                (fun (source, sink) -> source.Id = e' && g.Milestones |> Set.exists (fun (so, si) -> so.Id = sink.Id && si.Id = e))
    

        { value = (one || two || three || four)
          msg = (one, two, three, four) }

    // Proposition 8.
    // Hildebrandt et al. 2019: Declarative Choreographies and Liveness, new version
    let isDirectDependencyGivenMarkings (e: string) (g: G) (gP: G) : bool =
        let exx, ree, inn = g.M
        let exP, reP, inP = gP.M

        // There is a direct dependency between e' and e, if any of the following are true:
        // 1. The enabledness of e was changed between states.
        ((isEnabled e g).value && not (isEnabled e gP).value)
        <> (not ((isEnabled e g).value && (isEnabled e gP).value))
        // 2. The execution of e was changed between states.
        || ((Option.defaultValue false (exx.TryFind e))
            <> (Option.defaultValue false (exP.TryFind e)))
        // 3. The pending status of e was changed between states.
        || ((Option.defaultValue false (ree.TryFind e))
            <> (Option.defaultValue false (reP.TryFind e)))
        // 4. The inclusion of e was changed between states.
        || ((Set.exists (fun elm -> elm = e) inn) <> (Set.exists (fun elm -> elm = e) inP))

    // Definition 8 (Execution).
    // Hildebrandt et al. 2019: Declarative Choreographies and Liveness, older version.
    let execute (e: string) (g: G) : G =
        match Set.exists (fun (event: Event) -> event.Id = e) g.E with
        | false -> failwith (sprintf "Trying to execute an event \"%s\" which does not exist." e)
        | true ->
            match (isEnabled e g).value with
            | false ->
                Console.WriteLine(sprintf "(%s) is not enabled, not executing." e)
                g
            | true ->
                let exx, ree, inn = g.M

                // Update e to be executed.
                let newEx = exx.Add(e, true)

                // If e has a response relation to another event, make that event pending.
                // Also, e should not be pending anymore.
                let newRe =
                    g.Responses
                    |> Set.filter (fun (so, _) -> so.Id = e)
                    |> Set.fold (fun (acc: Re) (so, si) -> acc.Add(si.Id, true)) ree
                    |> fun (map: Re) -> map.Add(e, false)

                // If e has an exclusion/inclusion relation to some other event,
                // remove/add that event from/to the included events
                let newIn =
                    g.Exclusions
                    |> Set.filter (fun (so, _) -> so.Id = e)
                    |> Set.fold (fun (acc: In) (so, si) -> acc.Remove si.Id) inn
                    |> Set.union (
                        g.Inclusions
                        |> Set.filter (fun (so, _) -> so.Id = e)
                        |> Set.fold (fun (acc: In) (so, si) -> acc.Add si.Id) Set.empty
                    )

                { g with M = (newEx, newRe, newIn) }

    let isExecuted (e: string) (g: G) : bool =
        if not (Set.exists (fun (ev: Event) -> ev.Id = e) g.E) then
            failwith "Called isExecuted on event that does not exist in supplied graph."
        
        let exx, _, _ = g.M
        Option.defaultValue false (exx.TryFind e)

    let canExecuteEvent (id: string) (g: G): bool =
        (isEnabled id g).value

    let isIncluded (e: string) (g: G) : bool =
        if not (Set.exists (fun (ev: Event) -> ev.Id = e) g.E) then
            failwith "Called isIncluded on event that does not exist in supplied graph."
        
        let _, _, inn = g.M
        inn.Contains e

    // isExculded checks if an event is marked as excluded in the marking of a graph.
    let isExcluded (e: string) (g: G) : bool = not (isIncluded e g)

    let isPending (e: string) (g: G) : bool =
        let _, ree, _ = g.M
        Option.defaultValue false (ree.TryFind e)

    let getAllPendingEvents (g: G) : Set<string> =
        let _, ree, _ = g.M
        ree |> Map.filter (fun _ value -> value) |> Map.keys |> Set.ofSeq

    let getCurrentState (g: G): GraphState =
          let (exx, _, inn) = g.M
            
          let ree = getAllPendingEvents g

          let availableEvents = 
              g.E 
              |> Set.filter (fun (e: Event) -> canExecuteEvent e.Id g) 
              |> Set.map (fun (e: Event) -> e.Id) 
              |> Set.toList

          let isAccepting = ree.IsEmpty
          let isDeadlocked = availableEvents.IsEmpty && not isAccepting

          {
              isDeadlocked = isDeadlocked
              isAccepting = ree.IsEmpty
              availableEvents = availableEvents
              pendingEvents = ree |> Set.toList
          }


    let allPossibleNextStates (g: G) : Set<G> =
        g.E
        |> Set.filter (fun e -> (canExecuteEvent e.Id g))
        |> Set.map (fun e -> execute e.Id g)
        |> Set.filter (fun gP -> gP <> g) // disregard loops

    let rec dfsGatherStateSpace ((visitedStates, leafStates): Set<G> * Set<G>) (currentState: G) : Set<G> * Set<G> =
        if Set.contains currentState visitedStates then
            (visitedStates, leafStates)
        else
            // get all possible next states, not including loops.
            let nextStates = allPossibleNextStates currentState
            
            let newVisitedStates = visitedStates.Add(currentState)

            if nextStates.IsEmpty then
                // Nothing more to do, we are in a leaf state.
                (newVisitedStates, leafStates.Add(currentState))
            else
                // recursively perform DFS for all next states
                nextStates
                |> Set.fold (fun acc nextState -> dfsGatherStateSpace acc nextState) (newVisitedStates, leafStates)
        

    // Check if a state is in deadlock
    let isDeadlocked (g: G) : bool =
        // T. T. Hildebrandt, R. R. Mukkamala, T. Slaats, and F. Zanitti, �Contracts for cross-organizational workflows
        // as timed dynamic condition response graphs, Journal of Logic and Algebraic Programming, vol. 82, no. 5-7,
        // pp. 164-185, 2013.
        //
        // Definition 4.2

        let (_, _, inn) = g.M
        let ree = getAllPendingEvents g
        let eventsBothPendingAndIncluded = Set.intersect inn ree
        Set.forall (fun e -> not (isEnabled e.Id g).value) g.E && 
        not (Set.isEmpty eventsBothPendingAndIncluded)

    // Check if a state is in strong deadlock
    let isStronglyDeadlocked (g: G) : bool =
        // T. T. Hildebrandt, R. R. Mukkamala, T. Slaats, and F. Zanitti, �Contracts for cross-organizational workflows
        // as timed dynamic condition response graphs, Journal of Logic and Algebraic Programming, vol. 82, no. 5-7,
        // pp. 164-185, 2013.
        //
        // Definition 4.2

        let (_, _, inn) = g.M
        let ree = getAllPendingEvents g
        let eventsBothPendingAndIncluded = Set.intersect inn ree
        not (Set.exists (fun e -> (isEnabled e g).value) ree) &&
        // what if I can execute an event that excludes a pending event? Am I strongly deadlocked then?
        not (Set.isEmpty eventsBothPendingAndIncluded)
    
    let isDeadlockFree (g: G): bool =
        // T. T. Hildebrandt, R. R. Mukkamala, T. Slaats, and F. Zanitti, �Contracts for cross-organizational workflows
        // as timed dynamic condition response graphs, Journal of Logic and Algebraic Programming, vol. 82, no. 5-7,
        // pp. 164-185, 2013.
        //
        // Definition 4.2

        // Start a depth-first search from the initial state
        let _, leafs = dfsGatherStateSpace (Set.empty, Set.empty) g
        // Check if all reachable states are not deadlocked
        Set.forall (fun g -> not (isDeadlocked g)) leafs


    let isStronglyDeadlockFree (g: G): bool =
        // T. T. Hildebrandt, R. R. Mukkamala, T. Slaats, and F. Zanitti, Contracts for cross-organizational workflows
        // as timed dynamic condition response graphs, Journal of Logic and Algebraic Programming, vol. 82, no. 5-7,
        // pp. 164-185, 2013.
        //
        // Definition 4.2

        // Start a depth-first search from the initial state
        let _, leafs = dfsGatherStateSpace (Set.empty, Set.empty) g
        // Check if all reachable states are not deadlocked
        Set.forall (fun g -> not (isStronglyDeadlocked g)) leafs

