namespace Tests

open NUnit.Framework
open DCRGraph.State
open DCRGraph.Utils
open DCRGraph.EndpointProjection

[<TestFixture>]
type DeadlockTests() = 
    member val deadlocksGLoader : GLoader = { basePath = System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs", "deadlocks") } with get, set
    member val graphsGLoader : GLoader = { basePath = System.IO.Path.Join(__SOURCE_DIRECTORY__, "graphs") } with get, set

    [<Test>]
    member this.DeadlockExample1NotDeadlockFreeTest() =
        (*
            [1] R. R. Mukkamala, A formal model for declarative workflows: Dynamic Condition Response Graphs, thesis, IT University of Copenhagen, Theoretical Computer Science, Copenhagen, 2012 
            page 164 Chapter 6. Formal Verification, Tools and Implementation
            Figure 6.1: A non-deadlock free DCR Graph

            the example1.json is based on the example in the thesis
        *)
        let g = this.deadlocksGLoader.Load "example1.json"

        // Assert that the DCR Graph has a potential deadlock in it.
        Assert.That(not (isDeadlockFree g))

    [<Test>]
    member this.DeadlockExample1DeadlockFreeTest() =
        (*
            [1] R. R. Mukkamala, A formal model for declarative workflows: Dynamic Condition Response Graphs, thesis, IT University of Copenhagen, Theoretical Computer Science, Copenhagen, 2012 
            page 164 Chapter 6. Formal Verification, Tools and Implementation
            Figure 6.1: A non-deadlock free DCR Graph

            the example1.json is based on the example in the thesis
        *)
        let g = this.deadlocksGLoader.Load "example1_deadlock_free.json"

        // Assert that the DCR Graph has a potential deadlock in it.
        Assert.That(isDeadlockFree g)

    [<Test>]
    member this.DeadlockExample1NotStronglyDeadlockFreeTest() =
        (*
            [1] R. R. Mukkamala, A formal model for declarative workflows: Dynamic Condition Response Graphs, thesis, IT University of Copenhagen, Theoretical Computer Science, Copenhagen, 2012 
            page 164 Chapter 6. Formal Verification, Tools and Implementation
            Figure 6.1: A non-deadlock free DCR Graph

            the example1.json is based on the example in the thesis
        *)
        let g = this.deadlocksGLoader.Load "example1_deadlock_free.json"

        // Assert that the DCR Graph has a potential deadlock in it.
        Assert.That(not (isStronglyDeadlockFree g))

    [<Test>]
    member this.DeadlockExample2DeadlockFreeTest() =
        let g = this.deadlocksGLoader.Load "example2.json"

        // Assert that the DCR Graph is deadlock free.
        Assert.That(isDeadlockFree g)

    [<Test>]
    member this.DeadlockExample2NotStronglyDeadlockFreeTest() =
        let g = this.deadlocksGLoader.Load "example2.json"

        // Assert that the DCR Graph is deadlock free.
        Assert.That(not (isStronglyDeadlockFree g))

    [<Test>]
    member this.BuyerSellerExampleIsDeadlockFree() =
        let g = this.graphsGLoader.Load "buyer-seller-shipper.json"

        Assert.That(isStronglyDeadlockFree g)

    [<Test>]
    member this.e2ro3re2IsDeadlockFree() =
        let g = this.deadlocksGLoader.Load "e2ro3re2.json"

        Assert.That(isDeadlockFree g)

