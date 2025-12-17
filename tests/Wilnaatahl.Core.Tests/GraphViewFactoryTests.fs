module Wilnaatahl.Tests.GraphViewFactoryTests

open Xunit
open Swensen.Unquote
open Wilnaatahl.ViewModel
open Wilnaatahl.Model.FamilyGraph
open Wilnaatahl.Tests.ModelTests

[<Fact>]
let ``FirstWilp returns a wilp that exists in the graph huwilp set`` () =
    let factory = GraphViewFactory() :> IGraphViewFactory
    let graph = createFamilyGraph peopleAndParents

    let wilp = factory.FirstWilp graph

    // Simple inspection: the returned wilp should be one of the huwilp values
    huwilp graph |> Set.contains wilp =! true
