namespace Tests

open NUnit.Framework
open DCRGraph.DCRGraphDTO

[<TestFixture>]
type FlattenNestings() =
    [<Test>]
    member this.FlattenNestings() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "endpointprojectable")

        let jsonFile = System.IO.Path.Join(jsonPath, "graph.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile
        Assert.That(dcrGraph.Nestings.Length > 0)
        let dcrGraphFlattened = flattenDCRGraphDTO dcrGraph
        Assert.That(dcrGraphFlattened.Nestings.Length = 0)
        