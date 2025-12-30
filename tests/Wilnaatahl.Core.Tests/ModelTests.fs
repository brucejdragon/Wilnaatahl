module Wilnaatahl.Tests.ModelTests

open Xunit
open Swensen.Unquote
open System.Collections.Generic
open Wilnaatahl.Model
open Wilnaatahl.Model.FamilyGraph
open Wilnaatahl.Tests.TestData

[<Fact>]
let ``findPerson returns correct person for all ids`` () =
    let graph = createFamilyGraph peopleAndParents
    let expectedPeople = [| p0; p1; p2; p3; p4 |]

    [ 0..4 ]
    |> List.iter (fun id -> findPerson (PersonId id) graph =! expectedPeople[id])

[<Fact>]
let ``findChildren returns correct children set for each parent`` () =
    let graph = createFamilyGraph peopleAndParents

    findChildren (PersonId 0) graph
    =! Set.ofList [ PersonId 2; PersonId 3; PersonId 4 ]

    findChildren (PersonId 1) graph
    =! Set.ofList [ PersonId 2; PersonId 3; PersonId 4 ]

    findChildren (PersonId 2) graph =! Set.empty
    findChildren (PersonId 3) graph =! Set.empty
    findChildren (PersonId 4) graph =! Set.empty

[<Fact>]
let ``createFamilyGraph handles empty input`` () =
    let graph = createFamilyGraph []
    // All public API should return empty/throw as appropriate
    <@ findPerson (PersonId 0) graph |> ignore @> |> raises<KeyNotFoundException>

    findChildren (PersonId 0) graph =! Set.empty
    coparents graph =! Set.empty

[<Fact>]
let ``coparents returns all co-parent relationships`` () =
    let graph = createFamilyGraph peopleAndParents
    let coParentsSet = coparents graph
    coParentsSet =! Set.ofList [ coParents ]

[<Fact>]
let ``huwilp returns all unique huwilp`` () =
    let graph = createFamilyGraph peopleAndParents
    let huwilpSet = huwilp graph
    huwilpSet =! Set.ofList [ WilpName "H"; WilpName "L" ]
