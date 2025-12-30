module Wilnaatahl.Tests.TestData

open Wilnaatahl.Model
open Wilnaatahl.ViewModel

let private person id name shape wilp = {
    Person.Empty with
        Id = PersonId id
        Label = Some name
        Wilp = wilp
        Shape = shape
}

// Test data is public because they are shared by other tests.
let p0 = person 0 "Mother" Sphere (Some(WilpName "H"))
let p1 = person 1 "Father" Cube None
let p2 = person 2 "Child1" Sphere (Some(WilpName "H"))
let p3 = person 3 "Child2" Cube (Some(WilpName "L"))
let p4 = person 4 "Child3" Cube (Some(WilpName "H"))

let coParents = { Mother = PersonId 0; Father = PersonId 1 }

let peopleAndParents = [
    p0, None
    p1, None
    p2, Some coParents
    p3, Some coParents
    p4, Some coParents
]

let private treeNode id =
    let person = peopleAndParents |> List.find (fun (p, _) -> p.Id = PersonId id) |> fst

    {
        Id = NodeId id
        RenderedInWilp = WilpName "H"
        Position = float id, float id, 0.0
        TargetPosition = 0.0, 0.0, 0.0
        IsAnimating = false
        Person = person
    }

// Test data is public because they are shared by other tests.
let node0 = treeNode 0
let node1 = treeNode 1
let node2 = treeNode 2
let node3 = treeNode 3
let node4 = treeNode 4

let initialNodes = [ node0; node1; node2; node3; node4 ]
