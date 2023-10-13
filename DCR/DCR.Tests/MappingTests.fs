namespace Tests

open NUnit.Framework
open DCRGraph.DCRMapping
open DCRGraph.DCRGraphDTO
open DCRGraph.EndpointProjection
open DCRGraph.DCRGraph

[<TestFixture>]
type MappingTests() =
    [<Test>]
    member this.ConvertGtoDcrGraphDtoTest() =
        let jsonFilePath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "buyer-seller-shipper.json")

        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFilePath
        let dcrGraphFlattened = flattenDCRGraphDTO dcrGraph // Flattens the nestings.

        let g1: G = convertDCRGraphDTOToG dcrGraphFlattened // Flattens the "relations" field out of DcrRelationshipDTO. 

        let dto1: DCRGraphDTO = convertGtoDcrGraphDto g1

        let g2: G = convertDCRGraphDTOToG dto1
        
        let GTotalNumberOfRelations = g1.TotalNumberOfRelations
        let Dto1TotalNumberOfRelations = totalNumberOfRelations dto1
        let G2TotalNumberOfRelations = g2.TotalNumberOfRelations

        Assert.AreEqual(GTotalNumberOfRelations, Dto1TotalNumberOfRelations, "Number of G relations to DTO relations not the same.")
        Assert.AreEqual(Dto1TotalNumberOfRelations, G2TotalNumberOfRelations, "Number of DTO relations to G relations not the same.")