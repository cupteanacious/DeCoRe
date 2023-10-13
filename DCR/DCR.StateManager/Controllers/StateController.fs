namespace StateManager.Controllers

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open StateManager
open DCRGraph.DCRGraph

type LoadRequestModel() =
    member val path: string = "" with get, set
    member val role: string = "" with get, set

type EventRequestModel() =
    member val id: string = "" with get, set
    member val role: string = "" with get, set

type StateRequestModel() =
    member val role: string = "" with get, set

[<ApiController>]
[<Route("[controller]/[action]")>]
type StateController (stateManager: IStateManager) =
    inherit ControllerBase()

    [<HttpGet>]
    member _.Get() =
        "Working!"

    [<HttpGet>]
    member _.GetCurrentState([<FromBody>]stateRequestModel: StateRequestModel) =
        ActionResult<GraphState>(stateManager.getState stateRequestModel.role);

    [<HttpPost>]
    member _.LoadG([<FromBody>]pathModel: LoadRequestModel) =
        //stateManager.useGFromPath pathModel.role pathModel.path
        Console.WriteLine(String.Format("[{0}](LoadG) Data: {1}, {2}", DateTime.Now, pathModel.role, pathModel.path));
        stateManager.initializeFromChoreo pathModel.role pathModel.path
        
    [<HttpPost>]
    member _.LoadChoreo([<FromBody>]pathModel: LoadRequestModel) =
        stateManager.initializeFromChoreo pathModel.role pathModel.path
    
    [<HttpPost>]
    member _.CanExecute([<FromBody>]eventModel: EventRequestModel) =
        Console.WriteLine(String.Format("[{0}](CanExecute) Data: {1}, {2}", DateTime.Now, eventModel.role, eventModel.id));
        ActionResult<bool>(stateManager.canExecute eventModel.role eventModel.id);

    [<HttpPost>]
    member _.Execute([<FromBody>]eventModel: EventRequestModel) =
        Console.WriteLine(String.Format("[{0}](Execute) Data: {1}, {2}", DateTime.Now, eventModel.role, eventModel.id));
        stateManager.execute eventModel.role eventModel.id
