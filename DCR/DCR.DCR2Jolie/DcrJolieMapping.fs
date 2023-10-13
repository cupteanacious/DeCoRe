namespace DCR2Jolie

module DcrJolieMapping =
    open System
    open Shared.FileUtils
    open DCRGraph.DCRGraph
    open DCRGraph.EndpointProjection
    open Utils
    open PomGeneration
    open JavaConstructs
    open JolieConstructs

    let makeInterfaceOperationRR (e: Event): RequestResponseOperation =
        {
            Name            = e.Id
            RequestType     = "Request"
            ResponseType    = "Response"
        }

    let makeOperationExecution (operationName: string) (receiver: string) =
        {
            ServiceName     = receiver
            OperationName   = operationName
            Request         = "request"
            Response        = sprintf "%sResponse" (receiver.ToLower())
        }

    let makeStateMangerExecuteOperation: OperationExecutionRR =
        {
                ServiceName     = "StateManager"
                OperationName   = "Execute"
                Request         = "canExecuteRequest"
                Response        = "canExecute"
        }

    let makeServiceInterface (role: string) (sending: Set<Event>) (receiving: Set<Event>): JolieInterface =
        let sendingOperationList = [ for e in sending -> makeInterfaceOperationRR e ]
        let receivingOperationList = [ for e in receiving -> makeInterfaceOperationRR e ]

        {
            Name = sprintf "I%s" role
            OneWayOperations = []
            RequestResponseOperations = sendingOperationList @ receivingOperationList
        }

    let makeLocalServiceInterface (receiving: Set<Event>): JolieInterface =
        let receivingOperationList = [ for e in receiving -> makeInterfaceOperationRR e ]
        let operationList = List.append receivingOperationList []

        {
            Name = sprintf "ILocal"
            OneWayOperations = []
            RequestResponseOperations = operationList
        }

    let makeLocalService (ctx: Context) (role: string): JavaEmbeddedService =
        {
            ServiceName = "Local"
            InterfaceName = sprintf "ILocal"
            JavaClassName = sprintf "%s.%sService" (role.ToLower()) role
        }

    let makeSendingOperationsOrchestratorPattern (role: string) (e: Event): OperationDefinition =
        let op : OperationExecutionRR = {
            ServiceName     = "Orchestrator"
            OperationName   = e.Id // Do we wanna use Action or Id?
            Request         = "request"
            Response        = "orchestratorResponse"
        }

        let ot : RequestResponseOperation = {
            Name        = e.Id // Do we wanna use Action or Id?
            RequestType = "request"
            ResponseType = sprintf "%sResponse" (e.Initiator.ToLower())
        }

        let statusCodeVarAssign : Assignment = {
            VariableName = "statusCode"
            Value = "503"
        }

        let handler : Handler = {
            Exception = "IOException"
            Body = [Assignment statusCodeVarAssign]
        }

        let install : Install = {
            Handlers = [handler]
        }

        let scope : Scope = {
            Name = e.Id
            Body = [Install install; OperationExecutionRR op]
        }

        let wasHit : CodeBlock = {
            String = sprintf "println@console( \"endpoint %s on %s was hit\" )( )" e.Id role
        }

        {
            OperationType   = RequestResponseOperation ot
            Body            = [CodeBlock wasHit; Scope scope]
        }

    let makeStateManagerInterface : JolieInterface =
        {
            Name = "IStateManager"
            RequestResponseOperations = [
                {
                    Name = "LoadG"
                    RequestType = "LoadGRequest"
                    ResponseType = "void"
                };
                {
                    Name = "CanExecute"
                    RequestType = "CanExecuteRequest"
                    ResponseType = "bool"
                };
                {
                    Name = "Execute"
                    RequestType = "CanExecuteRequest"
                    ResponseType = "void"
                }
            ]
            OneWayOperations = []
        }

    let makeCanExecuteRequestType : JolieType =
       {
            Name = "CanExecuteRequest"
            Fields = [
                {
                    Name = "id"
                    FieldType = "string"
                };
                {
                    Name = "role"
                    FieldType = "string"
                }
            ]
        }

    let makeLoadGRequestType : JolieType =
       {
            Name = "LoadGRequest"
            Fields = [
                {
                    Name = "path"
                    FieldType = "string"
                };
                {
                    Name = "role"
                    FieldType = "string"
                }
            ]
        }

    let makePrintStatement (text: string) : OperationExecutionRR =
        {
            ServiceName     = "console"
            OperationName   = "println"
            Request         = sprintf "\"%s\"" text
            Response        = ""
        }

    let makeSendingOperations (role: string) (e: Event): OperationDefinition =
        let assignmentEventID : Assignment = {
            VariableName    = "canExecuteRequest.id"
            Value           = sprintf "\"%s\"" e.Id
        }
        let assignmentEventRole : Assignment = {
            VariableName    = "canExecuteRequest.role"
            Value           = sprintf "\"%s\"" role
        }

        let canExecuteOpex : OperationExecutionRR = {
            ServiceName     = "StateManager"
            OperationName   = "CanExecute"
            Request         = "canExecuteRequest"
            Response        = "canExecute"
        }

        let callingReceivers : List<Stmt> = [for r in e.Receivers -> OperationExecutionRR (makeOperationExecution e.Id r)]

        let callingReceiversParallel : Parallel = {
            Body = callingReceivers
        }

        let ot : RequestResponseOperation = {
            Name        = e.Id
            RequestType = "request"
            ResponseType = sprintf"%sResponse" (role.ToLower())
        }

        let statusCodeVarAssign : Assignment = {
            VariableName = "statusCode"
            Value = "503"
        }

        let ifStmt : IfStatment = {
            Condition   = "canExecute == true";
            TrueBody    = [
                            Parallel callingReceiversParallel;
                            OperationExecutionRR makeStateMangerExecuteOperation;
                            OperationExecutionRR (makePrintStatement (sprintf "[%s] Executed %s" role e.Id))
                          ];
            FalseBody   = [
                OperationExecutionRR (makePrintStatement (sprintf "[%s] Tried to execute %s, but not allowed" role e.Id));
                Assignment statusCodeVarAssign
            ];
        }

        let somethingWentWrong : OperationExecutionRR = makePrintStatement (sprintf "[%s] Something went wrong while executing %s" role e.Id)

        let handler : Handler = {
            Exception = "IOException"
            Body = [OperationExecutionRR somethingWentWrong; Assignment statusCodeVarAssign]
        }

        let install : Install = {
            Handlers = [handler]
        }

        let scope : Scope = {
            Name = e.Id
            Body = [Install install; Assignment assignmentEventID; Assignment assignmentEventRole; OperationExecutionRR canExecuteOpex; IfStatment ifStmt]
        }

        let wasHit : CodeBlock = {
            String = sprintf "println@console( \"[%s] Sending endpoint %s was hit\" )( )" role e.Id
        }

        {
            OperationType   = RequestResponseOperation ot
            Body            = [CodeBlock wasHit; Scope scope]
        }

    let makeReceivingOperations (role: string) (e: Event): OperationDefinition =
            let assignmentEventID : Assignment = {
                VariableName    = "canExecuteRequest.id"
                Value           = sprintf "\"%s\"" e.Id
            }

            let assignmentRole : Assignment = {
                VariableName    = "canExecuteRequest.role"
                Value           = sprintf "\"%s\"" role
            }

            let canExecuteOpex : OperationExecutionRR = {
                ServiceName     = "StateManager"
                OperationName   = "CanExecute"
                Request         = "canExecuteRequest"
                Response        = "canExecute"
            }

            let localServiceOE : OperationExecutionRR = {
                ServiceName     = "local"
                OperationName   = e.Id
                Request         = "request"
                Response        = sprintf "localResponse"
            }

            let operationType : RequestResponseOperation = {
                Name        = e.Id
                RequestType = "request"
                ResponseType = sprintf "%sResponse" role
            }
            
            let trueBodyOEs : List<Stmt> = [OperationExecutionRR localServiceOE; OperationExecutionRR makeStateMangerExecuteOperation; OperationExecutionRR (makePrintStatement (sprintf "[%s] Executed %s" role e.Id))]
            
            let ifStmt : IfStatment = {
                Condition   = "canExecute == true";
                TrueBody    = trueBodyOEs;
                FalseBody   = [OperationExecutionRR (makePrintStatement (sprintf "[%s] Tried to execute %s, but not allowed" role e.Id))];
            }
            let somethingWentWrong : OperationExecutionRR = makePrintStatement (sprintf "[%s] Something went wrong while executing %s" role e.Id)

            let statusCodeVarAssign : Assignment = {
                VariableName = "statusCode"
                Value = "503"
            }

            let handler : Handler = {
                Exception = "IOException"
                Body = [OperationExecutionRR somethingWentWrong; Assignment statusCodeVarAssign]
            }

            let install : Install = {
                Handlers = [handler]
            }

            let scope : Scope = {
                Name = e.Id
                Body = [Install install; Assignment assignmentEventID; Assignment assignmentRole; OperationExecutionRR canExecuteOpex; IfStatment ifStmt]
            }

            let wasHit : CodeBlock = {
                String = sprintf "println@console( \"[%s] Receiving endpoint %s was hit\" )( )" role e.Id
            }

            {
                OperationType   = RequestResponseOperation operationType
                Body            = [CodeBlock wasHit; Scope scope;]
            }

    let makeReceivingOperationsOrchestratorPattern (role: string) (e: Event): OperationDefinition =
            let op : OperationExecutionRR = {
                ServiceName     = "local"
                OperationName   = e.Id // Do we wanna use Action or Id?
                Request         = "request"
                Response        = sprintf "localResponse"
            }

            let ot : RequestResponseOperation = {
                Name        = e.Id // Do we wanna use Action or Id?
                RequestType = "request"
                ResponseType = sprintf "%sResponse" role
            }

            let statusCodeVarAssign : Assignment = {
                VariableName = "statusCode"
                Value = "503"
            }

            let handler : Handler = {
                Exception = "IOException"
                Body = [Assignment statusCodeVarAssign]
            }

            let install : Install = {
                Handlers = [handler]
            }

            let scope : Scope = {
                Name = e.Id
                Body = [Install install; OperationExecutionRR op]
            }

            let wasHit : CodeBlock = {
                String = sprintf "println@console( \"endpoint %s on %s was hit\" )( )" e.Id role
            }

            {
                OperationType   = RequestResponseOperation ot
                Body            = [CodeBlock wasHit; Scope scope;]
            }

    let makeInputPort (role: string) (portNumber: int): Port =
        {
            PortType    = "inputPort";
            PortName = sprintf "%s" role;
            Location    = {Location = sprintf "socket://localhost:%d" portNumber };
            Protocol    = {Protocol = "http { format = \"json\" .statusCode -> statusCode }" }
            Interfaces  = [sprintf "I%s" role];
        }

    let makeOutputPort (role: string) (portNumber: int): Port =
        {
            PortType    = "outputPort";
            PortName = sprintf "%s" role;
            Location    = {Location = sprintf "socket://localhost:%d" portNumber };
            Protocol    = {Protocol = "http { format = \"json\" .statusCode -> statusCode }" }
            Interfaces  = [sprintf "I%s" role];
        }

    let makeService (ctx: Context) (role: string) (sending: Set<Event>) (receiving: Set<Event>) (port: int) (ports: Port list): Service =
        let serviceName = sprintf "%sService" role
        let consoleEmbed = {
            ServiceName = "Console"
            Alias = "console"
        }

        let localEmbed = {
            ServiceName = "Local"
            Alias = "local"
        }

        let fileEmbed = {
            ServiceName = "File"
            Alias = "file"
        }

        let embedList = [consoleEmbed; localEmbed; fileEmbed]

        let getServiceDirectory : OperationExecutionRR = {
            ServiceName     = "file"
            OperationName   = "getServiceDirectory"
            Request         = ""
            Response        = "directory"
        }

        let loadGRequestPathAssignment : Assignment = {
            VariableName    = "loadGRequest.path"
            Value           = "directory + \"/projection.json\"" 
        }

        let loadGRequestRoleAssignment : Assignment = {
            VariableName    = "loadGRequest.role"
            Value           = sprintf "\"%s\"" role 
        }

        let showLoadingGErrorMessage : OperationExecutionRR = {
            ServiceName     = "console"
            OperationName   = "println"
            Request         = "\"Failed to load projection json with StateManager service. Reason: \" + LoadingProjection.IOException"
            Response        = ""
        }

        let handleIOException : Handler = {
            Exception = "IOException"
            Body = [OperationExecutionRR showLoadingGErrorMessage]
        }

        let installIOException : Install = {
            Handlers = [handleIOException]
        }

        let loadGStateManager : OperationExecutionRR = {
            ServiceName     = "StateManager"
            OperationName   = "LoadG"
            Request         = "loadGRequest"
            Response        = "loadGResponse"
        }

        let scope : Scope = {
            Name = "LoadingProjection"
            Body = [
                Install installIOException;
                OperationExecutionRR loadGStateManager;
            ]
        }

        let printServiceStarted : OperationExecutionRR = makePrintStatement (sprintf "[%s] %s started on port %d" role serviceName port)

        let initMethod = {
            Body = [
                OperationExecutionRR printServiceStarted;
                OperationExecutionRR getServiceDirectory;
                Assignment loadGRequestPathAssignment;
                Assignment loadGRequestRoleAssignment;
                Scope scope    
            ]
        }

        let sendingOperationList = [ for e in sending -> makeSendingOperations role e ]
        let receivingOperationList = [ for e in receiving -> makeReceivingOperations role e ]
        let operationList = List.append sendingOperationList receivingOperationList

        let mainMethod : MainMethod = {
            Body = operationList
        }

        {
            ServiceName = serviceName
            Embeds      = embedList
            PortList    = ports
            InitMethod  = initMethod
            MainMethod  = mainMethod
            Execution   = "sequential"
        }

    let makeInterfaceOperationOW (e: Event): OneWayOperation =
        {
            Name        = e.Id // Do we wanna use Action or Id?
            RequestType = "Request"
        }

    let makeJolieType (name: string) : JolieType =
        {
            Name = name
            Fields = []
        }