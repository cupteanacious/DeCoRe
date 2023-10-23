namespace DCR2Jolie

open Shared.JsonUtils

module EndpointCodegen =
    open System
    open Shared.FileUtils
    open DCRGraph.DCRGraph
    open DCRGraph.DCRMapping
    open DCRGraph.EndpointProjection
    open Utils
    open PomGeneration
    open JavaConstructs
    open JolieConstructs
    open DcrJolieMapping
    open DcrJavaMapping

    type ServiceInfo =
        {
            Role: Role
            ServicePath: string
            url: string
        }
        member this.JolieServicePath : string = sprintf "%s/jolie/%s_serivce.ol" (this.ServicePath) (this.Role.ToLower())
        member this.JolieFolderPath : string = sprintf "%s/jolie" (this.ServicePath)
        member this.JolieServiceName : string = sprintf "%sService" (this.Role)
        
    type Endpoint =
        {
            Role: Role
            Graph: G
            ShouldOverwrite: bool
        }
        member this.CreateEndpointFiles (ctx: Context) (endpointPorts: Map<Role, int>) (sourcePath: string): string * Set<Role> =
            // Seperate sending and receiving channels from each other.
            let sending: Set<Event> = Set.filter (fun (e: Event) -> e.Initiator = this.Role ) this.Graph.E
            let receiving = Set.filter (fun (e: Event) -> e.Initiator <> this.Role ) this.Graph.E
            let roleLowerCase = this.Role
            let outputDir = System.IO.Path.Join(sourcePath, this.Role)
            let outputDirJolie = System.IO.Path.Join(outputDir, "jolie")
            let outputDirJava = System.IO.Path.Join(outputDir, "java") 

            // Creating role_types.ol file
            let emptyRequestType : string = makeJolieType "Request" |> (fun (t: JolieType) -> t.Codegen ctx)
            let emptyResponseType : string = makeJolieType "Response" |> (fun (t: JolieType) -> t.Codegen ctx)
            let localServiceInterface: string = (makeLocalServiceInterface receiving) |> (fun (t: JolieInterface) -> t.Codegen ctx)
            let jolieServiceInterface: string = (makeServiceInterface this.Role sending receiving) |> (fun (t: JolieInterface) -> t.Codegen ctx)
            let canExecuteRequestType: string = makeCanExecuteRequestType |> (fun (t: JolieType) -> t.Codegen ctx)
            let loadGRequestType: string = makeLoadGRequestType |> (fun (t: JolieType) -> t.Codegen ctx)
            let iStateManagerInterface: string = makeStateManagerInterface |> (fun (t: JolieInterface) -> t.Codegen ctx)
            let typesCode : string = String.concat "\n\n" [emptyRequestType; emptyResponseType; localServiceInterface; jolieServiceInterface; canExecuteRequestType; loadGRequestType; iStateManagerInterface]
            let typesFilePath = System.IO.Path.Join(outputDirJolie, sprintf "%s_types.ol" roleLowerCase)
            writeToFile typesFilePath typesCode

            // Creating role_service.ol file
            let initiatorImportList = [
                {
                    ModuleSpecifier = "console"
                    ImportTarget    = "Console"
                };
                {
                    ModuleSpecifier = "file"
                    ImportTarget    = "File"
                };
                {
                    ModuleSpecifier = sprintf ".%s_types" roleLowerCase
                    ImportTarget    = sprintf "I%s, ILocal, IStateManager" this.Role
                }
            ]

            // from all the sending events gather reciever roles
            let recieverRoles: Set<Role> = 
                sending |> Set.map (fun (e: Event) -> e.Receivers)
                        |> Set.fold (fun (acc: Set<Role>) (r: Set<Role>) -> Set.union acc r) Set.empty
            let roleImports = 
                [
                    for r in recieverRoles -> 
                        {
                            ModuleSpecifier = sprintf ".%s_types" (r.ToLower()); 
                            ImportTarget = sprintf "I%s" r
                    }
                ]
            let importList = initiatorImportList @ roleImports
            let importCode : string = String.concat "\n" [ for i in importList -> i.Codegen ctx ]

            let inputPort: Port = makeInputPort this.Role endpointPorts[this.Role]
            let outputPorts: Port list = 
                [
                    for r in recieverRoles -> 
                        makeOutputPort r endpointPorts[r]
                ]
            let stateManagerPort : Port = 
                {
                    PortType    = "outputPort";
                    PortName = "StateManager"
                    Location    = {Location = sprintf "socket://localhost:5105/State/" };
                    Protocol    = {Protocol = "http { format = \"json\" .statusCode -> statusCode }" }
                    Interfaces  = ["IStateManager"];
                }
            let ports = inputPort :: outputPorts @ [stateManagerPort]

            let localService: string = (makeLocalService ctx this.Role) |> (fun (s: JavaEmbeddedService) -> s.Codegen ctx)
            let jolieService: string = (makeService ctx this.Role sending receiving endpointPorts[this.Role] ports) |> (fun (s: Service) -> s.Codegen ctx)
            let servicesCode: string = String.concat "\n\n" [importCode; localService; jolieService]
            let servicesFilePath = System.IO.Path.Join(outputDirJolie, sprintf "%s_service.ol" roleLowerCase)
            writeToFile servicesFilePath servicesCode


            let javaPackage: string = makeJavaPackage this.Role |> (fun (p: JavaPackage) -> p.Codegen)
            let javaServiceInterface: string = makeJavaInterface this.Role receiving |> (fun (i: JavaInterface) -> i.Codegen ctx)
            let javaInterfaceImports: string = makeJavaServiceInterfaceImports
                                                    |> List.map (fun (i: JavaImport) -> i.Codegen)
                                                    |> String.concat "\n"
            let javaInterfaceFile: string = String.concat "\n\n" [javaPackage; javaInterfaceImports; javaServiceInterface]
            let javaInterfaceFilePath = System.IO.Path.Join(outputDirJava, sprintf "I%sService.java" (this.Role))
            writeToFile javaInterfaceFilePath javaInterfaceFile

            // create gProjection file
            let projectionFilePath = System.IO.Path.Join(outputDirJolie, "projection.json")
            exportAsJson projectionFilePath (this.Graph |> convertGtoDcrGraphDto)
            
            // if the java service file already exists, then we don't want to overwrite it. if it doesn't exist, then we create it.
            // this is because we want to keep the user's code in the java service file.
            // if the shouldOverwrite flag is set to true, then we overwrite the file.

            let javaServiceFilePath = System.IO.Path.Join(outputDirJava, sprintf "%sService.java" (this.Role))
            let javaServiceFileExists = System.IO.File.Exists(javaServiceFilePath)

            if (not javaServiceFileExists) || this.ShouldOverwrite then
                // create java service file
                let javaServiceImports: string = makeJavaServiceImports 
                                                        |> List.map (fun (i: JavaImport) -> i.Codegen) 
                                                        |> String.concat "\n"

                let javaServiceClass: string = makeJavaClass this.Role receiving |> (fun (c: JavaClass) -> c.Codegen ctx)
                let javaServiceFile: string = String.concat "\n\n" [javaPackage; javaServiceImports; javaServiceClass]
                writeToFile javaServiceFilePath javaServiceFile

            let pomFilePath = System.IO.Path.Join(outputDir, "pom.xml")
            let pomFileExists = System.IO.File.Exists(pomFilePath)

            if (not pomFileExists) || this.ShouldOverwrite then
                writePomFileForRole roleLowerCase outputDir
            
            // only run maven if both the pom file and the java service files were newly created
            if this.ShouldOverwrite || (not pomFileExists && not javaServiceFileExists) then
                runMaven outputDir false

            (String.concat "\n\n" [emptyRequestType; emptyResponseType; jolieServiceInterface]), recieverRoles // shareable interface

    type ProgramFile = {
        FileName: string
        Path: string
        Program: Program
    }

    let CreatePortForRole (r: string) (portInfo: Map<string, int>): Port =

        let port = 
            match portInfo.TryFind r with
            | Some(portRes) -> portRes
            | None -> failwith "Role has no port assigned?"

        {
            PortType = "outputPort";
            PortName = r;
            Location = {Location = sprintf "socket://localhost:%d" port}
            Protocol =  {Protocol = "http { format = \"json\" .statusCode -> statusCode }" }
            Interfaces = [sprintf "I%s" r]
        }

    let makeAssignment (varName: string) (value: string) = 
        {
            VariableName    = varName
            Value           = sprintf "\"%s\"" value
        }

    let CreateShareableTypeFiles (initiator: Role) (receivers: Set<Role>) (allShareable: Map<Role, string * Set<Role>>) (sourcePath: string) =
        let outputDir = sourcePath

        // for all receivers, get thier shareable types and write each of them in a separate file in the outputDir for the initiator
        receivers |> Set.iter (fun receiver -> 
            let receiverTypes, _ = allShareable.[receiver]
            let receiverTypesFilePath = System.IO.Path.Join(outputDir, sprintf "%s" initiator, "jolie", sprintf"%s_types.ol"(receiver.ToLower()) )
            writeToFile receiverTypesFilePath receiverTypes
        )

    let GenerateEverything (g: G) (endpointProjections: EndpointProjections) (outputDir: string) (shouldOverwrite: bool) : Map<Role, ServiceInfo> =
        let sourcePath = outputDir
        let targetPath = System.IO.Path.Join(outputDir,"dcr_microservices.zip")
        
        let endpoints: Map<Role, Endpoint> = endpointProjections |> Map.map (fun (r: Role) (g: G) -> { Role = r; Graph = g; ShouldOverwrite = shouldOverwrite})

        let portFolder ((ports, port): Map<Role,int> * int) (r: Role) (g: G) = (ports.Add(r,port), port + 1)
        let (endpointPorts, _) = endpointProjections |> Map.fold portFolder (Map.empty, 9001)

        let generateRoleEndpointFiles (endpoint: Endpoint) : string * Set<Role> =
            try
                endpoint.CreateEndpointFiles {IndentLevel = 0} endpointPorts sourcePath
            with ex -> failwith (sprintf "Codegen failed for role %s, ex: %A" endpoint.Role ex)
        let sharableTypeFiles: Map<Role, string * Set<Role>> = endpointProjections |> Map.map (fun (r: Role) (g: G) -> generateRoleEndpointFiles endpoints[r])
        
        // for each role in sharableTypeFiles, CreateShareableTypeFiles
        sharableTypeFiles |> Map.iter (fun (initiator: Role) (_, receivers) -> CreateShareableTypeFiles initiator receivers sharableTypeFiles sourcePath)
        

        // zip the whole thang
        zipDirectory sourcePath targetPath

        let mapRolePortToServiceInfo (r: Role) (port: int) : ServiceInfo = 
            {
                Role = r
                ServicePath = System.IO.Path.Join(sourcePath, sprintf "%s" (r.ToLower()))
                url = sprintf "http://localhost:%d" port
            }

        //deleteDirectory sourcePath
        endpointPorts |> Map.fold (fun acc role port -> acc.Add (role, mapRolePortToServiceInfo role port)) Map.empty