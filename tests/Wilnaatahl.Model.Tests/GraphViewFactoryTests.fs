module Wilnaatahl.Tests.GraphViewFactoryTests

open Xunit
open Swensen.Unquote
open Wilnaatahl.ViewModel
open Wilnaatahl.Model
open Wilnaatahl.Model.FamilyGraph
open Wilnaatahl.Tests.ModelTests

[<Fact>]
let ``ExtractFamilies produces correct results`` () =
    let factory = GraphViewFactory() :> IGraphViewFactory
    let graph = createFamilyGraph peopleAndParents

    let families = factory.ExtractFamilies graph |> Seq.toList
    families.Length =! 1
    let fam = families.Head
    fam.Parents =! (PersonId 0, PersonId 1)

    Set.ofList fam.Children
    =! Set.ofList [ PersonId 2
                    PersonId 3
                    PersonId 4 ]
