namespace Tests

open NUnit.Framework
open DCRGraph.DCRGraphDTO
open DCRGraph.State
open DCRGraph.DCRMapping

[<TestFixture>]
type DependencyRule1() =
    // e = e'
    [<Test>]
    member this.eIsEPTest() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "dependencies", "true")

        let jsonFile = System.IO.Path.Join(jsonPath, "eIsEP.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        let isDD = isDirectDependency "eNullNull" "eNullNull" g

        // Assert that the direct dependency is due to #1:
        // e' is e.
        Assert.That(isDD.msg, Is.EqualTo((true, false, false, false)))

        // Assert that there is a direct dependency
        Assert.That(isDD.value, Is.EqualTo(true))

[<TestFixture>]
type DependencyRule2() =

    // e' --Condition--> e
    [<Test>]
    member this.ConditionTest() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "dependencies", "true")

        let jsonFile = System.IO.Path.Join(jsonPath, "ePConditionE.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        let isDD = isDirectDependency "ePConditione" "eNullNull" g

        // Assert that the direct dependency is due to #2:
        // If there is any relation between them
        Assert.That(isDD.msg, Is.EqualTo((false, true, false, false)))

        // Assert that there is a direct dependency
        Assert.That(isDD.value, Is.EqualTo(true))

    // e' --Response--> e
    [<Test>]
    member this.ResponseTest() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "dependencies", "true")

        let jsonFile = System.IO.Path.Join(jsonPath, "ePResponseE.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        let isDD = isDirectDependency "ePResponsee" "eNullNull" g

        // Assert that the direct dependency is due to #2:
        // If there is any relation between them
        Assert.That(isDD.msg, Is.EqualTo((false, true, false, false)))

        // Assert that there is a direct dependency
        Assert.That(isDD.value, Is.EqualTo(true))

    // e' --Milestone--> e
    [<Test>]
    member this.MilestoneTest() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "dependencies", "true")

        let jsonFile = System.IO.Path.Join(jsonPath, "ePMilestoneE.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        let isDD = isDirectDependency "ePMilestonee" "eNullNull" g

        // Assert that the direct dependency is due to #2:
        // If there is any relation between them
        Assert.That(isDD.msg, Is.EqualTo((false, true, false, false)))

        // Assert that there is a direct dependency
        Assert.That(isDD.value, Is.EqualTo(true))

    // e' --Inclusion--> e
    [<Test>]
    member this.InclusionTest() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "dependencies", "true")

        let jsonFile = System.IO.Path.Join(jsonPath, "ePInclusionE.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        let isDD = isDirectDependency "ePInclusione" "eNullNull" g

        // Assert that the direct dependency is due to #2:
        // If there is any relation between them
        Assert.That(isDD.msg, Is.EqualTo((false, true, false, false)))

        // Assert that there is a direct dependency
        Assert.That(isDD.value, Is.EqualTo(true))

    // e' --Exclusion--> e
    [<Test>]
    member this.ExclusionTest() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "dependencies", "true")

        let jsonFile = System.IO.Path.Join(jsonPath, "ePExclusionE.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        let isDD = isDirectDependency "ePExclusione" "eNullNull" g

        // Assert that the direct dependency is due to #2:
        // If there is any relation between them
        Assert.That(isDD.msg, Is.EqualTo((false, true, false, false)))

        // Assert that there is a direct dependency
        Assert.That(isDD.value, Is.EqualTo(true))


[<TestFixture>]
type DependencyRule3() =

    // e' --Include--> e'' --Condition--> e
    [<Test>]
    member this.IncludeConditionTest() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "dependencies", "true")

        let jsonFile = System.IO.Path.Join(jsonPath, "ePIncludeEPPConditionE.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        let isDD = isDirectDependency "ePIncludeePP" "eNullNull" g

        // Assert that the direct dependency is due to #3:
        // If e' includes or excludes an event e'', which is a condition or milestone for e
        Assert.That(isDD.msg, Is.EqualTo((false, false, true, false)))

        // Assert that there is a direct dependency
        Assert.That(isDD.value, Is.EqualTo(true))

[<TestFixture>]
type DepedencyRule4() =
    // e = e'
    [<Test>]
    member this.ePResponseEPPMilestoneETest() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "dependencies", "true")

        let jsonFile = System.IO.Path.Join(jsonPath, "ePResponseEPPMilestoneE.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        let isDD = isDirectDependency "ePResponseePP" "eNullNull" g

        // Assert that the direct dependency is due to #4:
        // If e' has a response relation to e'', and e'' is a milestone for e
        Assert.That(isDD.msg, Is.EqualTo((false, false, false, true)))

        // Assert that there is a direct dependency
        Assert.That(isDD.value, Is.EqualTo(true))


[<TestFixture>]
type NoDependencyTests() =

    // e' --Milestone--> e'' --Milestone--> e
    [<Test>]
    member this.MilestoneMilestoneTest() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "dependencies", "false")

        let jsonFile = System.IO.Path.Join(jsonPath, "ePMilestoneEPPMilestoneE.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        let isDD = isDirectDependency "ePMilestoneePP" "eNullNull" g

        // Assert that there is no direct dependency
        Assert.That(isDD.value, Is.EqualTo(false))
