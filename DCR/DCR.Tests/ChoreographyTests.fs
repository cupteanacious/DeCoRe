namespace Tests

open NUnit.Framework
open DCRGraph.DCRGraphDTO
open DCRGraph.State
open DCRGraph.DCRMapping
open DCRGraph.DCRGraph
open System.Diagnostics

[<TestFixture>]
type OrcehstrationTests() =
    [<Test>]
    member this.TestBuyerShipperSeller() =
        Trace.Listeners.Add(new ConsoleTraceListener()) |> ignore

        let executeAndAssert (event: string) (g: G) : G =
            Assert.That(isIncluded event g, sprintf "Event %s is not included" event)
            let isEnabledAnswer = isEnabled event g
            Assert.That(isEnabledAnswer.value, sprintf "Event %s is not enabled. Rules violated: %A" event isEnabledAnswer.msg)
            let g_after_event = execute event g
            Assert.That(isExecuted event g_after_event, sprintf "Failed to execute %s" event)

            g_after_event

        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs")

        let jsonFile = System.IO.Path.Join(jsonPath, "buyer-seller-shipper.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile |> flattenDCRGraphDTO

        //let graphvizFigure: GraphvizFigure = DCRGraphToGraphviz dcrGraph
        //NUnit.Framework.TestContext.Progress.WriteLine(graphvizFigure.Codegen)

        let g = convertDCRGraphDTOToG dcrGraph
        
        let g_after_buyerAsksSellers = executeAndAssert "BuyerAsksSellers" g
        let g_after_seller1QuotesBuyer = executeAndAssert "Seller1QuotesBuyer" g_after_buyerAsksSellers
        let g_after_seller2QuotesBuyer = executeAndAssert "Seller2QuotesBuyer" g_after_seller1QuotesBuyer
        let g_after_buyerAcceptsSeller1 = executeAndAssert "BuyerAcceptsSeller1" g_after_seller2QuotesBuyer
        let g_after_seller1OrdersShipper = executeAndAssert "Seller1OrdersShipper" g_after_buyerAcceptsSeller1
        let g_after_shipperDetailsBuyer = executeAndAssert "ShipperDetailsBuyer" g_after_seller1OrdersShipper

        Assert.True(true)