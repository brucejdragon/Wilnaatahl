module Wilnaatahl.Tests.ModelTests

open Xunit
open Swensen.Unquote
open System.Collections.Generic
open Wilnaatahl.Model
open Wilnaatahl.Model.FamilyGraph

let private person id name shape wilp =
    { Person.Empty with
        Id = PersonId id
        Label = Some name
        Wilp = wilp
        Shape = shape }

// Test data is public because they are shared by other tests.
let p0 = person 0 "Mother" Sphere (Some(WilpName "H"))
let p1 = person 1 "Father" Cube None
let p2 = person 2 "Child1" Sphere (Some(WilpName "H"))
let p3 = person 3 "Child2" Cube (Some(WilpName "L"))
let p4 = person 4 "Child3" Cube (Some(WilpName "H"))

let coParents = { Mother = PersonId 0; Father = PersonId 1 }

let peopleAndParents =
    [ p0, None
      p1, None
      p2, Some coParents
      p3, Some coParents
      p4, Some coParents ]

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
