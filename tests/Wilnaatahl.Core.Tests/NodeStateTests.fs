module Wilnaatahl.Tests.NodeStateTests

open Xunit
open Swensen.Unquote
open Wilnaatahl.ViewModel
open Wilnaatahl.ViewModel.NodeState
open Wilnaatahl.Tests.TestData

[<Fact>]
let ``createNodeState initializes nodes and empty selection`` () =
    let state = createNodeState initialNodes

    unselected state |> Seq.length =! List.length initialNodes

    selected state |> Seq.length =! 0

[<Fact>]
let ``select moves node from unselected to selected`` () =
    let state = createNodeState initialNodes
    let state' = select (NodeId 1) state
    isSelected (NodeId 1) state' =! true
    selected state' |> Seq.length =! 1

    unselected state' |> Seq.length =! List.length initialNodes - 1

[<Fact>]
let ``select is idempotent for already selected node`` () =
    let state = createNodeState initialNodes |> select (NodeId 1)

    let state' = select (NodeId 1) state
    selected state' |> Seq.length =! 1

[<Fact>]
let ``deselect moves node from selected to unselected`` () =
    let state = createNodeState initialNodes |> select (NodeId 1)

    let state' = deselect (NodeId 1) state
    isSelected (NodeId 1) state' =! false
    selected state' |> Seq.length =! 0

    unselected state' |> Seq.length =! List.length initialNodes

[<Fact>]
let ``deselect is idempotent for unselected node`` () =
    let state = createNodeState initialNodes
    let state' = deselect (NodeId 1) state
    selected state' |> Seq.length =! 0

[<Fact>]
let ``deselectAll moves all selected nodes to unselected`` () =
    let state = createNodeState initialNodes |> select (NodeId 1) |> select (NodeId 2)

    let state' = deselectAll state
    selected state' |> Seq.length =! 0

    unselected state' |> Seq.length =! List.length initialNodes

[<Fact>]
let ``findNode finds selected and unselected nodes`` () =
    let state = createNodeState initialNodes |> select (NodeId 1)

    let n1 = findNode (NodeId 1) state
    let n2 = findNode (NodeId 2) state
    n1 =! node1
    n2 =! node2

[<Fact>]
let ``isSelected returns correct status`` () =
    let state = createNodeState initialNodes |> select (NodeId 1)

    isSelected (NodeId 1) state =! true
    isSelected (NodeId 2) state =! false

[<Fact>]
let ``mapSelected applies function to all selected nodes`` () =
    let state = createNodeState initialNodes |> select (NodeId 1) |> select (NodeId 2)

    let state' = mapSelected (fun n -> { n with Position = 0.0, 0.0, 0.0 }) state

    selected state' |> Seq.forall (fun n -> n.Position = (0.0, 0.0, 0.0)) =! true

[<Fact>]
let ``selected and unselected return correct sets`` () =
    let state = createNodeState initialNodes |> select (NodeId 1) |> select (NodeId 2)

    let sel = selected state |> Seq.map _.Id |> Set.ofSeq

    let unsel = unselected state |> Seq.map _.Id |> Set.ofSeq

    sel =! Set.ofList [ NodeId 1; NodeId 2 ]

    unsel =! Set.ofList [ NodeId 0; NodeId 3; NodeId 4 ]
