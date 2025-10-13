module Wilnaatahl.Tests.GraphViewFactoryTests

open Xunit
open Swensen.Unquote
open Wilnaatahl.ViewModel
open Wilnaatahl.Model
open Wilnaatahl.Model.FamilyGraph
open Wilnaatahl.Tests.ModelTests
open Wilnaatahl.Tests.NodeStateTests

[<Fact>]
let ``ExtractFamilies produces correct results`` () =
    let factory = GraphViewFactory() :> IGraphViewFactory
    let graph = createFamilyGraph peopleAndParents

    let families =
        factory.ExtractFamilies graph initialNodes
        |> Seq.toList

    families.Length =! 1
    let fam = families.Head
    fam.Parents =! (NodeId 0, NodeId 1)

    Set.ofList fam.Children
    =! Set.ofList [ NodeId 2
                    NodeId 3
                    NodeId 4 ]
