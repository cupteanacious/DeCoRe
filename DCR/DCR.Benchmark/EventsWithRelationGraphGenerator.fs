namespace Benchmark


module EventWithRelationGraphGenerator =
    open System
    //open System.Collections.Generic
    open DCRGraph.DCRGraphDTO

    let random = Random()


    type EventsWithRelationsGenerationContext = 
        { 
            NumRoles: int;
            RelationType: string
        }

    let createEvent (initiator: string) (receivers: string list) : DcrEventDTO = 
        let action = ((Guid.NewGuid()).ToString("N")).Substring(0, 8)

        {
            Id = sprintf "%s%s" initiator action;
            Initiator = initiator;
            Action = action;
            Receivers = receivers;
        }

    let createEvents (initiator: string) (receivers: string list) : DcrEventDTO list = 
        let initiatorForAll = initiator

        [for i in 2 .. receivers.Length do
            let receiversForEvent = receivers |> List.take i
            let lastTwoReceivers = receiversForEvent |> List.rev |> List.take 2
            yield createEvent initiatorForAll lastTwoReceivers]


    let createMarkingsForEvent (event: DcrEventDTO) : DcrMarkingsDTO =
        {
            EventId = event.Id;
            Marking = {
                Executed = false;
                Included = true;
                Pending = false
            }
        }

    let creaetRelations (events: DcrEventDTO list) (relationType: string): DcrRelationshipDTO list =
        [for i in 0 .. events.Length - 2 do
            let source = events.[i]
            let target = events.[i + 1]
            {
                From = source.Id;
                To = target.Id;
                Relations = [
                    {
                        RelationType = relationType;
                    }
                ]
            }]

    let makeNumOfRoleNames (numRoles: int) : string list =
        [for i in 1 .. numRoles do yield sprintf "Role%s" (i.ToString())]

    let generateGraphIncreasingEventsAndRolesWithRelation (gCtx: EventsWithRelationsGenerationContext) =

        let roles = makeNumOfRoleNames gCtx.NumRoles

        let events = createEvents "Role1" roles

        let relationships = creaetRelations events gCtx.RelationType

        let markings = events |> List.map createMarkingsForEvent

        {
            Events = events;
            Nestings = List.empty;
            Relationships = relationships;
            Markings = markings
        }