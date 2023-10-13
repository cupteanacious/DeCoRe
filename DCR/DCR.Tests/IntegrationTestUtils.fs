namespace Tests


module IntegrationTestUtils =
    open System
    open System.IO
    open System.Diagnostics
    open System.Text
    open System.Threading.Tasks
    open NUnit.Framework
    open System.Net
    open System.Net.Http
    open FSharp.Data
    open System.Threading
    open DCR2Jolie.EndpointCodegen
    open DCRGraph.DCRGraph
    open DCRGraph.DCRMapping
    open DCRGraph.DCRGraphDTO
    open DCRGraph.EndpointProjection

    let TESTS_PROJECT_DIR = __SOURCE_DIRECTORY__

    type ProcessResult = { 
        Process : Process;
        StdOut : StringBuilder; 
        StdErr : StringBuilder;
    }

    type ExecutionMode =
        | Sync
        | Async

    let FailIfNonZeroExitCode (processResult: ProcessResult) (sender: obj) (e: System.EventArgs) =
        let exitCode = processResult.Process.ExitCode
        let allowedExitCodes = [0; 130; 137]
        let allowed = allowedExitCodes |> List.contains exitCode
        let stdErr = sprintf "========== stderr start ==========\n%s\n========== stderr end ==================" (processResult.StdErr.ToString())
        Assert.That(allowed, sprintf "Process exited with exit code %d. Standard error was:\n%s" exitCode stdErr)

    let executeProcess (processName: string) (processArgs: string) (workDir: string) (execMode: ExecutionMode) (useShell: bool) =
        let psi = new ProcessStartInfo(processName) 
        psi.Arguments <- processArgs
        psi.WorkingDirectory <- workDir
        psi.UseShellExecute <- useShell
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.CreateNoWindow <- true        
        let proc = Process.Start(psi)
        proc.EnableRaisingEvents <- true
        let output = new StringBuilder()
        let error = new StringBuilder()
        proc.OutputDataReceived.Add(fun args -> output.Append(args.Data) |> ignore; NUnit.Framework.TestContext.Progress.WriteLine(args.Data))
        proc.ErrorDataReceived.Add(fun args -> error.Append(args.Data) |> ignore; NUnit.Framework.TestContext.Error.WriteLine(args.Data))
        proc.BeginErrorReadLine()
        proc.BeginOutputReadLine()
        
        let result = { Process = proc; StdOut = output; StdErr = error }
        
        proc.Exited.AddHandler(new System.EventHandler(fun sender e -> FailIfNonZeroExitCode result sender e))
        
        match execMode with
        | Sync ->  proc.WaitForExit()
        | Async -> ()

        result
    let executeDotnetCommandOSAgnostic (args: string) (path: string) (execType: ExecutionMode) =
        match System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows) with
        | true -> executeProcess "dotnet" args path execType
        | false -> executeProcess "/bin/dotnet" args path execType

    let checkAPIRunning (url: string) =
        async {
            let! response = Http.AsyncRequest(url)

            if response.StatusCode = (int)HttpStatusCode.OK then
                return true
            else
                return false

        } |> Async.RunSynchronously

    let WaitForStateManager (pr: ProcessResult) : Task =
        let isStateManagerRunning () =
            pr.StdOut.ToString().Contains("Now listening")
        
        let checkIfStateManagerIsRunning = 
            while not (isStateManagerRunning ()) do
                Thread.Sleep(1000)
        
        Task.Run(fun () -> checkIfStateManagerIsRunning)

    let killAllProcessesInProcessResultList (lst: List<ProcessResult>) =
        for pr in lst do
            try 
                pr.Process.Kill(true)
                NUnit.Framework.TestContext.Progress.WriteLine(sprintf "Killed process with pid %d together with child processes." pr.Process.Id);
            with 
                | :? InvalidOperationException -> ()
        NUnit.Framework.TestContext.Progress.WriteLine("Killed all processes associated with test case.")

    let private runJolieService (path: string) (serviceName: string) : ProcessResult =
        match System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) with
        | true ->
            executeProcess "jolie" (sprintf "-s %s %s" serviceName (Path.GetFileName path)) (Path.GetDirectoryName path) Async false
        | false ->
            executeProcess "/bin/jolie" (sprintf "-s %s %s" serviceName (Path.GetFileName path)) (Path.GetDirectoryName path) Async false

    let private makeServiceName (pathToService: string) : string =
        let capitalize (str: string) : string =
            str.[0..0].ToUpper() + str.[1..]

        let extractRoleName (name: string) : string =
            name.Replace("_service", "")

        let roleToServiceName (roleName: string) : string =
            roleName + "Service"

        Path.GetFileNameWithoutExtension pathToService 
            |> extractRoleName
            |> capitalize
            |> roleToServiceName

    let spawnJolieServices (pathToCode: string) : List<ProcessResult> =
        let pathToServices = Directory.EnumerateFiles(pathToCode, "*_service.ol", SearchOption.AllDirectories)

        pathToServices |> Seq.map (fun path -> Thread.Sleep(1000); runJolieService path (makeServiceName path)) |> List.ofSeq

    let runStateManager : ProcessResult = 
        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Running StateManager.")

        let workDir = Path.Join(TESTS_PROJECT_DIR, "../DCR.StateManager/")
        let processResult = executeDotnetCommandOSAgnostic "run --no-build" workDir Async false

        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Waiting for StateManager to be ready..")
        let task = WaitForStateManager processResult
        Task.WaitAll [|task|]
        NUnit.Framework.TestContext.Progress.WriteLine("[Test] StateManager is now running on port 5105.")

        let stateManagerUrl = "http://127.0.0.1:5105/state/get"
        Assert.That(checkAPIRunning stateManagerUrl, "StateManager API is not running.")

        processResult

    let WaitForJolieServices (prList: List<ProcessResult>) =
        let isJolieServiceRunning (pr: ProcessResult) =
            pr.StdOut.ToString().Contains("started")
        
        let checkIfJolieServiceIsRunning (pr: ProcessResult) = 
            while not (isJolieServiceRunning (pr)) do
                Thread.Sleep(1000)

        let tasks = [|for pr in prList -> Task.Run(fun () -> checkIfJolieServiceIsRunning pr)|]

        Task.WaitAll tasks
        NUnit.Framework.TestContext.Progress.WriteLine("All Jolie services are now running.")


    let generateFilesFromChoreography (choreographyPath: string) : Map<Role,ServiceInfo> = 

        let dcrGraph: DCRGraphDTO =
            choreographyPath |> jsonToDCRGraphDTO |> flattenDCRGraphDTO

        let g = dcrGraph |> convertDCRGraphDTOToG 
        let ePP = endpointProjectGraph g false

        //printEndpointProjections ePP

        GenerateEverything g ePP (Path.GetDirectoryName choreographyPath) false
        
    let serviceEventExecuter (httpClient: HttpClient) (serviceInfoMap: Map<Role, ServiceInfo>) =
        let executeServiceEvent (role: Role) (eventName: string) : Task<unit> =
            let serviceBaseUrl = match (serviceInfoMap.TryFind role) with
                                    | Some(serviceInfo) -> serviceInfo.url
                                    | None -> failwith "Role does not exist in serviceInfoMap."
            let eventUrl: string = sprintf "%s/%s" serviceBaseUrl eventName
            task {
                try
                    use! response = httpClient.GetAsync(eventUrl)
                    response.EnsureSuccessStatusCode() |> ignore
                    NUnit.Framework.TestContext.Progress.WriteLine(sprintf "%s" (response.StatusCode.ToString()))
                with
                | :? HttpRequestException as e ->
                    NUnit.Framework.TestContext.Error.WriteLine(sprintf "\nException Caught!")
                    NUnit.Framework.TestContext.Error.WriteLine(sprintf "Message :%s" e.Message)
            }
        executeServiceEvent

    let startStateManager (httpClient: HttpClient) (path: string) = 
        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Starting StateManager")
        let main =
            task {
                try
                    let content = 
                        new StringContent(sprintf "{\"path\": \"%s\"}" path, 
                            Encoding.UTF8, 
                            "application/json"
                        )
                    use! response = httpClient.PostAsync("http://localhost:5105/State/initializeFromChoreo", content)
                    response.EnsureSuccessStatusCode() |> ignore
                    let! responseBody = response.Content.ReadAsStringAsync()
                    NUnit.Framework.TestContext.Progress.WriteLine(sprintf "[StateManager]%s" responseBody)
                with
                | :? HttpRequestException as e ->
                    NUnit.Framework.TestContext.Error.WriteLine(sprintf "\nException Caught!")
                    NUnit.Framework.TestContext.Error.WriteLine(sprintf "Message :%s" e.Message)
            }

        main.Wait()
        NUnit.Framework.TestContext.Progress.WriteLine("[Test] StateManager now running on port 5105")