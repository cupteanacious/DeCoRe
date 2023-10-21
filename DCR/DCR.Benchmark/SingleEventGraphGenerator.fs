namespace Benchmark


module SingleEventGraphGenerator =
    open System
    //open System.Collections.Generic
    open DCRGraph.DCRGraphDTO

    let random = Random()


    type SingleEventGenerationContext = 
        { 
            NumRoles: int
        }

    let createEvent (initiator: string) (receivers: string list) : DcrEventDTO = 
        let action = ((Guid.NewGuid()).ToString("N")).Substring(0, 8)

        {
            Id = sprintf "%s%s" initiator action;
            Initiator = initiator;
            Action = action;
            Receivers = receivers;
        }


    let createMarkingsForEvent (event: DcrEventDTO) : DcrMarkingsDTO =
        {
            EventId = event.Id;
            Marking = {
                Executed = false;
                Included = false;
                Pending = false
            }
        }

    let makeNumOfRoleNames (numRoles: int) : string list =
        [for i in 1 .. numRoles do yield sprintf "Role%s" (i.ToString())]

    let generateGraphSingleEventMultipleRoles (gCtx: SingleEventGenerationContext) =
    
        let roles = makeNumOfRoleNames gCtx.NumRoles

        let event = createEvent "Role1" roles


        let markings = createMarkingsForEvent event

        {
            Events = [event];
            Nestings = List.empty;
            Relationships = [];
            Markings = [markings]
        }