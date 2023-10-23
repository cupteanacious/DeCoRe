namespace DCRGraph

/// This module contains types that reflects the data structure of DCR Graphs, as presented in the research paper:
/// Hildebrandt et al. “Declarative Choreographies and Liveness” 2019
///
/// It's different from DCRCompiler.Frontend.DcrGraphDsl, in that DcrGraphDsl is not about simulating DCR graphs,
/// but serves as an IR between the JSON DSL and this. DcrGraph on the other hand, is about staying as close to
/// the research paper as possible and carrying out DCR graph logic.
/// 
/// In its current state, it does not deal with time or data and is thus not a one-to-one mapping of the paper.
/// However, the goal is to support at least data, and possibly time, at some point.
///
/// All types are named according to the research paper and a more thorough explanation of each type can be found there.
///
module DCRGraph =
    type Role = string
    type Event =
        { Id: string
          Initiator: string
          Action: string
          Receivers: Set<string> }
    type E = Set<Event>
    type Ex = Map<string, bool>
    type Re = Map<string, bool>
    type In = Set<string>
    type M = Ex * Re * In
    type R = Set<Role>

    type RelationSet = Set<Event * Event>

    type GraphState =
        {
            isDeadlocked: bool
            isAccepting: bool
            availableEvents: List<string>
            pendingEvents: List<string>
        }

    type G =
        { E: E
          M: M
          Conditions: RelationSet
          Responses: RelationSet
          Milestones: RelationSet
          Inclusions: RelationSet
          Exclusions: RelationSet
          Cancels: RelationSet 
          R: R}

        member this.allRelations: RelationSet =
            Set.unionMany
                [ this.Conditions
                  this.Responses
                  this.Exclusions
                  this.Inclusions
                  this.Milestones
                  this.Cancels ]

        static member Empty: G =
            { 
                E = Set.empty
                M = (Map.empty, Map.empty, Set.empty)
                Conditions = Set.empty
                Responses = Set.empty
                Milestones = Set.empty
                Inclusions = Set.empty
                Exclusions = Set.empty
                Cancels = Set.empty 
                R = Set.empty
            }
        
        member this.TotalNumberOfRelations: int =
            [this.Milestones; this.Conditions; this.Responses; this.Inclusions; this.Exclusions] |> List.fold (fun (total: int) (lst: RelationSet) -> total + lst.Count ) 0