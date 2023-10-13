namespace Tests

open NUnit.Framework
open DCRGraph.DCRGraphDTO
open DCRGraph.State
open DCRGraph.DCRMapping

[<TestFixture>]
type ExecuteInclude() =
    [<Test>]
    member this.Action1IncludesAction2() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "execution")

        let jsonFile = System.IO.Path.Join(jsonPath, "Action1IncludesAction2.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        Assert.That(not (isExecuted "AAction1B" g))
        Assert.That(not (isIncluded "AAction2B" g))
        Assert.That(not ((isEnabled "AAction2B" g).value))

        let gP = execute "AAction1B" g

        Assert.That(isExecuted "AAction1B" gP)
        Assert.That(isIncluded "AAction2B" gP)
        Assert.That((isEnabled "AAction2B" gP).value)

[<TestFixture>]
type ExecuteExclude() =
    [<Test>]
    member this.Action1ExcludesItself() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "execution")

        let jsonFile = System.IO.Path.Join(jsonPath, "Action1ExcludesItself.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        Assert.That((isEnabled "AAction1B" g).value)
        Assert.That(isIncluded "AAction1B" g)

        let gP = execute "AAction1B" g

        Assert.That(not (isEnabled "AAction1B" gP).value)
        Assert.That(isExcluded "AAction1B" gP)
        
    [<Test>]
    member this.ActionAExcludesActionB() =
        let jsonPath =
            System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "execution")

        let jsonFile = System.IO.Path.Join(jsonPath, "Action1ExcludesAction2.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        Assert.That((isEnabled "AAction2B" g).value)
        Assert.That(isIncluded "AAction2B" g)

        let gP = execute "AAction1B" g

        Assert.That(not (isEnabled "AAction2B" gP).value)
        Assert.That(isExcluded "AAction2B" gP)


[<TestFixture>]
type ExecutePendingResponse() =
    [<Test>]
    member this.Action1MakesAction2Pending() =
        let jsonPath =
                System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "execution")

        let jsonFile = System.IO.Path.Join(jsonPath, "Action1MakesAction2Pending.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        Assert.That(not(isExecuted "AAction1B" g))
        Assert.That(not(isPending "AAction2B" g))

        let gP = execute "AAction1B" g

        Assert.That(isExecuted "AAction1B" gP)
        Assert.That(isPending "AAction2B" gP)



[<TestFixture>]
type ExecuteMilestone() =
    [<Test>]
    member this.CannotExecuteAction2WhenAction1IsPending() =
        let jsonPath =
                System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "execution")

        let jsonFile = System.IO.Path.Join(jsonPath, "Action1IsMilestoneForAction2.json")
        let dcrGraph: DCRGraphDTO = jsonToDCRGraphDTO jsonFile

        let g = convertDCRGraphDTOToG dcrGraph

        Assert.That(isPending "AAction1B" g)
        Assert.That(not (isEnabled "AAction2B" g).value) // Not enabled because AAction1B is pending and there is a milestone relation.

        // "AAction2B" is not enabled, and thus "executing" it does nothing
        let gP = execute "AAction2B" g
        Assert.That(not (isExecuted "AAction2B" gP))

        let gPP = execute "AAction1B" gP // Make the pending event non-pending
        Assert.That(not (isPending "AAction1B" gPP))

        let exx, ree, inn = gPP.M

        // "AAction2B" is enabled now, and thus executing it changes gPP
        let gPPP = execute "AAction2B" gPP

        let exx, ree, inn = gPPP.M
        Assert.That(isExecuted "AAction2B" gPPP)
