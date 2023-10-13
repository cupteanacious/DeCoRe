namespace Tests

open NUnit.Framework
open DCRGraph.DCRGraphDTO
open DCRGraph.DCRMapping
open DCRGraph.EndpointProjection

[<TestFixture>]
type EndpointProjectability() =
    [<Test>]
    member this.TestNonProjectability() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "endpointprojectable")

        let jsonFile = System.IO.Path.Join(jsonPath, "buyer-seller-shipper-not-epp.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile
        let dcrGraphFlattened = flattenDCRGraphDTO dcrGraph
        Assert.That(dcrGraphFlattened.Nestings.Length = 0)

        let g = convertDCRGraphDTOToG dcrGraphFlattened

        let isEPP1 = isEndpointProjectable "Seller1" g
        let isEPP2 = isEndpointProjectable "Seller2" g

        match isEPP1 with
        | Ok b -> Assert.Fail("Expected graph to not be endpoint projectable for Seller1")
        | Error e -> ()

        match isEPP2 with
        | Ok b -> Assert.Fail("Expected graph to not be endpoint projectable for Seller2")
        | Error e -> ()

        Assert.Pass()
        
    [<Test>]
    member this.TestProjectability() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "endpointprojectable")

        let jsonFile = System.IO.Path.Join(jsonPath, "buyer-seller-shipper-epp.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile
        let dcrGraphFlattened = flattenDCRGraphDTO dcrGraph

        let g = convertDCRGraphDTOToG dcrGraphFlattened

        let isEPP1 = isEndpointProjectable "Seller1" g
        let isEPP2 = isEndpointProjectable "Seller2" g

        match isEPP1 with
        | Ok b -> ()
        | Error e -> Assert.Fail(e)

        match isEPP2 with
        | Ok b -> ()
        | Error e -> Assert.Fail(e)

        Assert.Pass()

    [<Test>]
    member this.TestProjectingG() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "endpointprojectable")

        let jsonFile = System.IO.Path.Join(jsonPath, "buyer-seller-shipper-epp.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile
        let dcrGraphFlattened = flattenDCRGraphDTO dcrGraph

        let g = convertDCRGraphDTOToG dcrGraphFlattened

        try
            endpointProjectGraph g false |> ignore

        with (NotEndpointProjectable e) -> 
            Assert.Fail(e)