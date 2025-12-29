module Wilnaatahl.Tests.GraphViewFactoryTests

open Xunit
open Swensen.Unquote
open Wilnaatahl.ViewModel
open Wilnaatahl.Model.FamilyGraph
open Wilnaatahl.Tests.ModelTests
open Wilnaatahl.Tests.NodeStateTests

[<Fact>]
let ``ExtractFamilies produces correct results`` () =
    let factory = GraphViewFactory() :> IGraphViewFactory
    let graph = createFamilyGraph peopleAndParents

    let families = factory.ExtractFamilies graph initialNodes |> Seq.toList

    families.Length =! 1
    let fam = families.Head
    fam.Parents =! (NodeId 0, NodeId 1)

    Set.ofList fam.Children =! Set.ofList [ NodeId 2; NodeId 3; NodeId 4 ]

[<Fact>]
let ``FirstWilp returns a wilp that exists in the graph huwilp set`` () =
    let factory = GraphViewFactory() :> IGraphViewFactory
    let graph = createFamilyGraph peopleAndParents

    let wilp = factory.FirstWilp graph

    // Simple inspection: the returned wilp should be one of the huwilp values
    huwilp graph |> Set.contains wilp =! true
