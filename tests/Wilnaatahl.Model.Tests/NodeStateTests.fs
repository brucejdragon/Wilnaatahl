module Wilnaatahl.Tests.NodeStateTests

open Expecto
open Wilnaatahl.Model
open Wilnaatahl.ViewModel
open Wilnaatahl.ViewModel.NodeState

let samplePerson id name =
    { id = NodeId id
      position = float id, float id, 0.0
      person =
        { label = Some name
          shape = Sphere
          mother = None
          father = None
          dateOfBirth = None
          dateOfDeath = None } }

let node1 = samplePerson 1 "Alice"
let node2 = samplePerson 2 "Bob"
let node3 = samplePerson 3 "Carol"
let node4 = samplePerson 4 "Dave"

let initialNodes = [ node1; node2; node3; node4 ]

let tests =
    testList
        "NodeState"
        [ test "createNodeState initializes nodes and empty selection" {
              let state = createNodeState initialNodes
              Expect.equal (unselected state |> Seq.length) 4 "All nodes unselected initially"
              Expect.equal (selected state |> Seq.length) 0 "No nodes selected initially"
          }

          test "select moves node from unselected to selected" {
              let state = createNodeState initialNodes
              let state' = select (NodeId 1) state
              Expect.isTrue (isSelected (NodeId 1) state') "Node 1 is selected"
              Expect.equal (selected state' |> Seq.length) 1 "One node selected"
              Expect.equal (unselected state' |> Seq.length) 3 "Three nodes unselected"
          }

          test "select is idempotent for already selected node" {
              let state = createNodeState initialNodes |> select (NodeId 1)
              let state' = select (NodeId 1) state
              Expect.equal (selected state' |> Seq.length) 1 "Selecting again does not duplicate"
          }

          test "deselect moves node from selected to unselected" {
              let state = createNodeState initialNodes |> select (NodeId 1)
              let state' = deselect (NodeId 1) state
              Expect.isFalse (isSelected (NodeId 1) state') "Node 1 is deselected"
              Expect.equal (selected state' |> Seq.length) 0 "No nodes selected"
              Expect.equal (unselected state' |> Seq.length) 4 "All nodes unselected again"
          }

          test "deselect is idempotent for unselected node" {
              let state = createNodeState initialNodes
              let state' = deselect (NodeId 1) state
              Expect.equal (selected state' |> Seq.length) 0 "Deselecting unselected node does nothing"
          }

          test "deselectAll moves all selected nodes to unselected" {
              let state =
                  createNodeState initialNodes
                  |> select (NodeId 1)
                  |> select (NodeId 2)

              let state' = deselectAll state
              Expect.equal (selected state' |> Seq.length) 0 "All nodes deselected"
              Expect.equal (unselected state' |> Seq.length) 4 "All nodes unselected"
          }

          test "findNode finds selected and unselected nodes" {
              let state = createNodeState initialNodes |> select (NodeId 1)
              let n1 = findNode (NodeId 1) state
              let n2 = findNode (NodeId 2) state
              Expect.equal n1 node1 "findNode returns correct selected node"
              Expect.equal n2 node2 "findNode returns correct unselected node"
          }

          test "isSelected returns correct status" {
              let state = createNodeState initialNodes |> select (NodeId 1)
              Expect.isTrue (isSelected (NodeId 1) state) "Node 1 selected"
              Expect.isFalse (isSelected (NodeId 2) state) "Node 2 not selected"
          }

          test "mapSelected applies function to all selected nodes" {
              let state =
                  createNodeState initialNodes
                  |> select (NodeId 1)
                  |> select (NodeId 2)

              let state' = mapSelected (fun n -> { n with position = 0.0, 0.0, 0.0 }) state

              let allZero =
                  selected state'
                  |> Seq.forall (fun n -> n.position = (0.0, 0.0, 0.0))

              Expect.isTrue allZero "All selected nodes have updated position"
          }

          test "selected and unselected return correct sets" {
              let state =
                  createNodeState initialNodes
                  |> select (NodeId 1)
                  |> select (NodeId 2)

              let sel =
                  selected state
                  |> Seq.map (fun n -> n.id)
                  |> Set.ofSeq

              let unsel =
                  unselected state
                  |> Seq.map (fun n -> n.id)
                  |> Set.ofSeq

              Expect.equal sel (Set.ofList [ NodeId 1; NodeId 2 ]) "Selected set correct"
              Expect.equal unsel (Set.ofList [ NodeId 3; NodeId 4 ]) "Unselected set correct"
          } ]
