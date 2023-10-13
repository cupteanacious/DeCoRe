namespace Tests

open NUnit.Framework
open System.Diagnostics
open System
open System.IO
open IntegrationTestUtils
open System.Threading
open System.Net.Http


[<TestFixture>]
type IntegrationTests() =
    member val TESTS_PROJECT_DIR = __SOURCE_DIRECTORY__
    member val processResultList_local : List<ProcessResult> = [] with get, set
    member val processResultList_global : List<ProcessResult> = [] with get, set
    member val httpClient = new HttpClient() with get, set

    [<OneTimeSetUp>]
    member this.BeforeAllSetup() =
        Trace.Listeners.Add(new ConsoleTraceListener()) |> ignore

    [<SetUp>]
    member this.BeforeEachSetup() =
        this.processResultList_local <- this.processResultList_local @ [runStateManager]
        let httpClientHandler = new HttpClientHandler()
        // Accept any certificate, to allow for requests to localhost (more secure would probably be accepting a self-signed certificate?)
        httpClientHandler.ServerCertificateCustomValidationCallback <- HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        this.httpClient <- new HttpClient(httpClientHandler)

    [<TearDown>]
    member this.AfterEachTeardown() =
        killAllProcessesInProcessResultList this.processResultList_local 
        NUnit.Framework.TestContext.Progress.WriteLine("Killed all test-local processes")

    [<OneTimeTearDown>]
    member this.AfterAllTeardown() =
        killAllProcessesInProcessResultList this.processResultList_global
        NUnit.Framework.TestContext.Progress.WriteLine("Killed all test-global processes")
        Trace.Flush()

    [<Test>]
    member this.TestBuyerShipperSeller () =
        let testDir = Path.Join(this.TESTS_PROJECT_DIR, "integrationTestFiles", "buyer-shipper-seller")
        let choreographyPath = Path.Join(testDir, "choreography.json")

        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Running build pipeline on choreography file.")
        let serviceInfoMap = generateFilesFromChoreography choreographyPath
        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Build pipeline finished.")

        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Spinning up Jolie services.")
        let jolieServicesProcessResults = spawnJolieServices (Path.Join(testDir, "code"))
        this.processResultList_local <- this.processResultList_local @ jolieServicesProcessResults
        WaitForJolieServices jolieServicesProcessResults
        NUnit.Framework.TestContext.Progress.WriteLine("[Test] All Jolie services are ready.")
        
        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Initialising StateManager with choreography.")
        startStateManager this.httpClient choreographyPath
        NUnit.Framework.TestContext.Progress.WriteLine("[Test] StateManager initialised.")


        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Starting choreography dance.")
        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Hitting 'BuyerAsksSellers' at Buyer")
        let executeServiceEvent = serviceEventExecuter this.httpClient serviceInfoMap
        let executeBuyerAsksSellers = executeServiceEvent "Buyer" "BuyerAsksSellers"
        executeBuyerAsksSellers.Wait()
        Thread.Sleep(1000)

        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Hitting 'Seller1QuotesBuyer' at Seller1")
        let executeSeller1QuotesBuyer = executeServiceEvent "Seller1" "Seller1QuotesBuyer"
        executeSeller1QuotesBuyer.Wait()
        Thread.Sleep(1000)

        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Hitting 'Seller2QuotesBuyer' at Seller2")
        let executeSeller2QuotesBuyer = executeServiceEvent "Seller2" "Seller2QuotesBuyer"
        executeSeller2QuotesBuyer.Wait()
        Thread.Sleep(1000)

        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Hitting 'BuyerAcceptsSeller1' at Buyer")
        let executeBuyerAcceptsSeller1 = executeServiceEvent "Buyer" "BuyerAcceptsSeller1"
        executeBuyerAcceptsSeller1.Wait()
        Thread.Sleep(1000)

        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Hitting 'Seller1OrdersShipper' at Seller1")
        let executeSeller1OrdersShipper = executeServiceEvent "Seller1" "Seller1OrdersShipper"
        executeSeller1OrdersShipper.Wait()
        Thread.Sleep(1000)

        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Hitting 'ShipperDetailsBuyer' at Shipper")
        let executeShipperDetailsBuyer = executeServiceEvent "Shipper" "ShipperDetailsBuyer"
        executeSeller1OrdersShipper.Wait()
        Thread.Sleep(1000)
        NUnit.Framework.TestContext.Progress.WriteLine("[Test] Choreography completed.")
        Assert.Pass()


    [<Test>]
    member this.TestDeadlockExample () =
        //let testDir = Path.Join(this.TESTS_PROJECT_DIR, "integrationTestFiles", "deadlocks", "example1")
        //let choreographyPath = Path.Join(testDir, "choreography.json")

        //NUnit.Framework.TestContext.Progress.WriteLine("Running build pipeline on choreography file.")
        //let serviceInfoMap = generateFilesFromChoreography choreographyPath // should fail here
        
        //let jolieServicesProcessResults = spawnJolieServices (Path.Join(testDir, "code"))
        //this.processResultList_local <- this.processResultList_local @ jolieServicesProcessResults
        
        //WaitForJolieServices jolieServicesProcessResults
        
        //startStateManager this.httpClient choreographyPath

        // let executeServiceEvent = serviceEventExecuter this.httpClient serviceInfoMap
        // let executeBuyerAsksSellers = executeServiceEvent "Buyer" "BuyerAsksSellers"
        // executeBuyerAsksSellers.Wait()
        // Thread.Sleep(1000)

        // let executeSeller1QuotesBuyer = executeServiceEvent "Seller1" "Seller1QuotesBuyer"
        // executeSeller1QuotesBuyer.Wait()
        // Thread.Sleep(1000)
        
        Assert.Ignore("Not implemented.")