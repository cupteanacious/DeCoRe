namespace DCR2Jolie

module JolieConstructs =
    open Utils

    type Field =
        {
            Name: string
            FieldType: string
        }
        member this.Codegen (ctx:Context) = 
            ctx.Indent (sprintf "%s: %s" this.Name this.FieldType)

    type JolieType =
        {
            Name: string
            Fields: List<Field>
        }
        member this.Codegen (ctx:Context) = 
            let fieldsCode = GenerateCodegenList this.Fields ctx.IncrementIndentLevel "\n"
            sprintf "type %s {\n%s\n}" this.Name fieldsCode

    type JolieTypeList =
        {
            Types: List<JolieType>
        }
        member this.Codegen (ctx:Context) =
            GenerateCodegenList this.Types ctx "\n\n"

    type OneWayOperation =
        {
            Name: string
            RequestType: string
        }
        member this.Codegen (ctx:Context) = 
            ctx.Indent (sprintf "%s( %s )" this.Name this.RequestType)

    type RequestResponseOperation =
        {
            Name: string
            RequestType: string
            ResponseType: string
        }
        member this.Codegen (ctx:Context) = 
            ctx.Indent (sprintf "%s( %s )( %s )" this.Name this.RequestType this.ResponseType)

    type JolieInterface =
        {
            Name: string
            OneWayOperations: List<OneWayOperation>
            RequestResponseOperations: List<RequestResponseOperation>
        }
        member this.Codegen (ctx:Context) = 
            let oneWayOperationsCode = match this.OneWayOperations.IsEmpty with
                                        | true -> ""
                                        | false -> this.OneWayOperations
                                                        |> List.map (fun op -> op.Codegen ctx.IncrementIndentLevel)
                                                        |> String.concat ",\n"
                                                        |> fun ops -> ctx.IncrementIndentLevel.Indent (sprintf "OneWay:\n%s" ops)

            let requestResponseOperationsCode = match this.RequestResponseOperations.IsEmpty with
                                                | true -> ""
                                                | false -> this.RequestResponseOperations
                                                                |> List.map (fun op -> op.Codegen ctx.IncrementIndentLevel)
                                                                |> String.concat ",\n"
                                                                |> fun ops -> ctx.IncrementIndentLevel.Indent (sprintf "RequestResponse:\n%s" ops)
            let oneWayCodeAdjusted = match this.OneWayOperations.IsEmpty, this.RequestResponseOperations.IsEmpty with
                                        | true, _ -> ""
                                        | false, true -> oneWayOperationsCode
                                        | false, false -> oneWayOperationsCode |> adjustBlockForNewLine
            sprintf "interface %s {\n%s%s\n}" this.Name oneWayCodeAdjusted requestResponseOperationsCode

    type JolieInterfaceList =
        {
            Interfaces: List<JolieInterface>
        }
        member this.Codegen (ctx:Context) =
            this.Interfaces
                |> List.map (fun i -> i.Codegen ctx)
                |> String.concat "\n\n"

    type PortLocation = 
        {
            Location: string
        } 
        member this.Codegen (ctx:Context) = ctx.Indent (sprintf "location: \"%s\"" this.Location)
    
    type PortProtocol = 
        {
            Protocol: string
        }
        member this.Codegen (ctx:Context) = ctx.Indent (sprintf "protocol: %s" this.Protocol)

    type Port = 
        {
            PortType: string
            PortName: string
            Location: PortLocation
            Protocol: PortProtocol
            Interfaces: List<string>
        }
        member this.Codegen (ctx:Context) =
            let locationCode = this.Location.Codegen ctx
            let protocolCode = this.Protocol.Codegen ctx
            let interfaceCode = ctx.Indent (sprintf "interfaces: %s" (String.concat ", " this.Interfaces))
            
            let singleTab = ctx.IncrementIndentLevel

            let body = singleTab.Indent (String.concat "\n" [locationCode; protocolCode; interfaceCode])

            ctx.Indent (sprintf "%s %s {\n%s\n}" this.PortType this.PortName body)

    type OperationType = 
        | OneWayOperation of OneWayOperation
        | RequestResponseOperation of RequestResponseOperation
        member this.Codegen (ctx: Context) =
            match this with
                | OneWayOperation owo -> owo.Codegen ctx
                | RequestResponseOperation rro -> rro.Codegen ctx

    type OperationExecutionRR =
        {
            ServiceName: string
            OperationName: string
            Request: string
            Response: string
        }
        member this.Codegen (ctx: Context) =
            ctx.Indent (sprintf "%s@%s( %s )( %s )" this.OperationName this.ServiceName this.Request this.Response)
            
    type OperationExecutionOW =
        {
            ServiceName: string
            OperationName: string
            Request: string
        }
        member this.Codegen (ctx: Context) =
            ctx.Indent (sprintf "%s@%s( %s )" this.OperationName this.ServiceName this.Request)

    type Assignment = 
        {
            VariableName: string;
            Value: string;
        }
        member this.Codegen (ctx: Context) =
            ctx.Indent (sprintf "%s = %s" this.VariableName this.Value)

    type Stmt =
        | OperationExecutionRR of OperationExecutionRR
        | OperationExecutionOW of OperationExecutionOW
        | IfStatment of IfStatment
        | Assignment of Assignment
        | CodeBlock of CodeBlock
        | Scope of Scope
        | Install of Install
        | Parallel of Parallel
        member this.Codegen (ctx: Context) =
            match this with
                | OperationExecutionRR owo -> owo.Codegen ctx
                | OperationExecutionOW rro -> rro.Codegen ctx
                | IfStatment ifstmt -> ifstmt.Codegen ctx
                | Assignment a -> a.Codegen ctx
                | CodeBlock cb -> cb.Codegen ctx
                | Scope s -> s.Codegen ctx
                | Install i -> i.Codegen ctx
                | Parallel p -> p.Codegen ctx
    and IfStatment =
        {
            Condition: string
            TrueBody: List<Stmt>
            FalseBody: List<Stmt>
        }
        member this.Codegen (ctx: Context) =
            let singleTab = ctx.IncrementIndentLevel
            
            let trueBody = GenerateCodegenList this.TrueBody singleTab "\n"
            let falseBody = GenerateCodegenList this.FalseBody singleTab "\n"

            ctx.Indent (sprintf "if (%s) {\n%s\n}\nelse {\n%s\n}" this.Condition trueBody falseBody)
    and Scope = 
        {
            Name: string
            Body: List<Stmt>
        }
        member this.Codegen (ctx: Context) = 
            let body = GenerateCodegenList this.Body ctx.IncrementIndentLevel "\n"

            ctx.Indent (sprintf "scope( %s ) {\n%s\n}" this.Name body)

    and Install = 
        {
            Handlers: List<Handler>
        }
        member this.Codegen (ctx: Context) =
            let indent = ctx.IncrementIndentLevel
            let handlers = GenerateCodegenList this.Handlers indent "\n"
            ctx.Indent (sprintf "install(\n%s\n);" handlers)

    and Handler = 
        {
            Exception: string
            Body:   List<Stmt>
        }
        member this.Codegen (ctx: Context) =
            let indent = ctx.IncrementIndentLevel
            ctx.Indent (sprintf "%s =>\n%s" this.Exception (GenerateCodegenList this.Body indent "\n"))
    and Parallel =
        {
            Body: List<Stmt>
        }
        member this.Codegen (ctx: Context) =
            let indent = ctx.IncrementIndentLevel
            ctx.Indent (sprintf "{\n%s\n}" (GenerateCodegenList this.Body indent "\n|\n"))

    type OperationDefinition = 
        {
            OperationType: OperationType
            Body: List<Stmt>
        }
        member this.Codegen (ctx: Context) =
            sprintf "[%s {\n%s\n}]" (this.OperationType.Codegen ctx) ((ctx.IncrementIndentLevel).Indent (GenerateCodegenList this.Body ctx "\n"))

    type MainMethod =
        {
            Body: List<OperationDefinition>
        }
        member this.Codegen (ctx:Context) =
            let body = 
                match this.Body.IsEmpty with
                | false -> GenerateCodegenList this.Body ctx "\n\n"
                | true -> ctx.Indent (sprintf "nullProcess")

            ctx.Indent (sprintf "main {\n%s\n}" (ctx.IncrementIndentLevel.Indent body))

    type Embed = 
        {
            ServiceName: string
            Alias: string
        }
        member this.Codegen (ctx:Context) = 
            ctx.Indent (sprintf "embed %s as %s" this.ServiceName this.Alias)

    type InitMethod = 
        {
            Body: List<Stmt>
        }
        member this.Codegen (ctx:Context) = 
            let body = GenerateCodegenList this.Body ctx "\n"
            ctx.Indent (sprintf "init {\n%s\n}" (ctx.IncrementIndentLevel.Indent body))

    type Service = 
        {
            ServiceName: string
            InitMethod: InitMethod
            MainMethod: MainMethod
            PortList: List<Port>
            Embeds: List<Embed>
            Execution: string
        }
        member this.Codegen (ctx:Context) : string = 
            let embedCode = GenerateCodegenList this.Embeds ctx "\n"

            let executionCode = (sprintf "execution: %s" this.Execution)

            let portCode = GenerateCodegenList this.PortList ctx "\n\n"

            let initCode = this.InitMethod.Codegen ctx

            let mainMethodCode = this.MainMethod.Codegen ctx

            let body = String.concat "\n\n" [embedCode; executionCode; portCode; initCode; mainMethodCode]

            sprintf "service %s {\n%s\n}" this.ServiceName (ctx.IncrementIndentLevel.Indent body)

    type JavaEmbeddedService = 
        {
            ServiceName: string
            InterfaceName: string
            JavaClassName: string
        }
        member this.Codegen (ctx:Context) = 
            let singleTab = ctx.IncrementIndentLevel
            
            let inputPortCode = 
                singleTab.Indent (sprintf "inputPort Input {\n%s\n%s\n}   foreign java {\n%s\n}" 
                    (singleTab.Indent (sprintf "location: \"local\""))
                    (singleTab.Indent (sprintf "interfaces: %s" this.InterfaceName))
                    (singleTab.Indent (sprintf "class: \"%s\"" this.JavaClassName)))
            

            ctx.Indent (sprintf "service %s {\n%s\n}" this.ServiceName inputPortCode)

    type ServiceType = 
        | Service of Service
        | JavaEmbeddedService of JavaEmbeddedService
        member this.Codegen (ctx: Context) =
            match this with 
            | Service s -> s.Codegen ctx
            | JavaEmbeddedService jes -> jes.Codegen ctx

    type NullProcess = string

    type Import = 
        {
            ModuleSpecifier: string
            ImportTarget: string
        }
        member this.Codegen (ctx: Context) =
            let mutable importStr = sprintf "import %s" this.ImportTarget
            if this.ModuleSpecifier.Length > 0 then
                importStr <- (sprintf "from %s " this.ModuleSpecifier) + importStr
            ctx.Indent (importStr)

    type Program = 
        {
            Imports: List<Import>
            Services: List<ServiceType>
        }
        member this.Codegen (ctx:Context) = 
            let importCode = GenerateCodegenList this.Imports ctx "\n"
            let serviceCode = GenerateCodegenList this.Services ctx "\n\n"
            sprintf "%s\n\n%s\n" importCode serviceCode
            