module Wilnaatahl.Tests.ViewModelTests

open Xunit
open Swensen.Unquote
open Wilnaatahl.ViewModel
open Wilnaatahl.Tests.NodeStateTests

let family =
    { Parents = NodeId 1, NodeId 2
      Children = [ NodeId 3; NodeId 4 ] }

let families = [ family ]

let viewModel = ViewModel() :> IViewModel

let initialState = viewModel.CreateInitialViewState(initialNodes, families)

let update msg state = viewModel.Update state msg

[<Fact>]
let ``CanUndo and CanRedo reflect undo/redo state`` () =
    viewModel.CanUndo initialState =! false
    viewModel.CanRedo initialState =! false
    // Only drag operations are undoable
    let state =
        initialState
        |> update (TouchNode(NodeId 1))
        |> update (StartDrag(1.0, 1.0, 0.0))
        |> update (DragTo(2.0, 2.0, 0.0))
        |> update EndDrag

    viewModel.CanUndo state =! true
    let state' = state |> update Undo
    viewModel.CanRedo state' =! true

[<Fact>]
let ``CreateInitialViewState sets up nodes and families`` () =
    let state = initialState

    let nodes =
        viewModel.EnumerateUnselectedTreeNodes state
        |> Seq.toList

    let fams = viewModel.EnumerateFamilies state |> Seq.toList
    nodes =! initialNodes
    fams =! families

[<Fact>]
let ``EnumerateSelectedTreeNodes and EnumerateUnselectedTreeNodes reflect selection`` () =
    let state = initialState |> update (SelectNode(NodeId 1))

    let selected =
        viewModel.EnumerateSelectedTreeNodes state
        |> Seq.toList

    let unselected =
        viewModel.EnumerateUnselectedTreeNodes state
        |> Seq.toList

    selected =! [ node1 ]
    Set.ofList unselected =! Set.ofList [ node2; node3; node4 ]

[<Fact>]
let ``EnumerateFamilies returns all families`` () =
    let fams =
        viewModel.EnumerateFamilies initialState
        |> Seq.toList

    fams =! families

[<Fact>]
let ``EnumerateChildren returns correct children`` () =
    let children =
        viewModel.EnumerateChildren initialState family
        |> Seq.toList

    children =! [ node3; node4 ]

[<Fact>]
let ``EnumerateParents returns correct parents`` () =
    let p1, p2 = viewModel.EnumerateParents initialState family
    p1 =! node1
    p2 =! node2

[<Fact>]
let ``IsSingleSelectEnabled reflects selection mode`` () =
    let state = initialState
    viewModel.IsSingleSelectEnabled state =! true
    let state' = state |> update (ToggleSelection MultiSelect)
    viewModel.IsSingleSelectEnabled state' =! false

[<Fact>]
let ``ShouldEnableOrbitControls reflects drag state`` () =
    viewModel.ShouldEnableOrbitControls initialState =! true

    let state =
        initialState
        |> update (TouchNode(NodeId 1))
        |> update (StartDrag(1.0, 1.0, 0.0))

    viewModel.ShouldEnableOrbitControls state =! false

[<Fact>]
let ``SelectNode selects and deselects nodes correctly`` () =
    // Select node 1
    let state1 = initialState |> update (SelectNode(NodeId 1))

    viewModel.EnumerateSelectedTreeNodes state1 |> Seq.toList =! [ node1 ]

    // Deselect node 1 by selecting again
    let state2 = state1 |> update (SelectNode(NodeId 1))

    viewModel.EnumerateSelectedTreeNodes state2 |> Seq.length =! 0

    // MultiSelect mode: select node 1, then node 2
    let state3 =
        initialState
        |> update (ToggleSelection MultiSelect)
        |> update (SelectNode(NodeId 1))
        |> update (SelectNode(NodeId 2))

    let selected =
        viewModel.EnumerateSelectedTreeNodes state3
        |> Seq.map _.Id
        |> Set.ofSeq

    selected =! Set.ofList [ NodeId 1; NodeId 2 ]

[<Fact>]
let ``DeselectAll clears all selections`` () =
    let state =
        initialState
        |> update (ToggleSelection MultiSelect)
        |> update (SelectNode(NodeId 1))
        |> update (SelectNode(NodeId 2))
        |> update DeselectAll
    
    viewModel.EnumerateSelectedTreeNodes state |> Seq.length =! 0

[<Fact>]
let ``StartDrag sets drag state and enables undo`` () =
    let state =
        initialState
        |> update (SelectNode(NodeId 1))
        |> update (TouchNode(NodeId 1))
        |> update (StartDrag(1.0, 1.0, 0.0))
    viewModel.ShouldEnableOrbitControls state =! false
    viewModel.CanUndo state =! true

[<Fact>]
let ``DragTo updates selected node positions`` () =
    let state =
        initialState
        |> update (SelectNode(NodeId 1))
        |> update (TouchNode(NodeId 1))
        |> update (StartDrag(1.0, 1.0, 0.0))
        |> update (DragTo(2.0, 2.0, 0.0))
    let selected = viewModel.EnumerateSelectedTreeNodes state |> Seq.toList

    let position =
        selected
            |> List.filter (fun n -> n.Id = NodeId 1)
            |> List.map _.Position
            |> List.tryHead
    
    position =! Some(2.0, 2.0, 0.0)

[<Fact>]
let ``EndDrag sets DragEnding and clears redo`` () =
    let state =
        initialState
        |> update (SelectNode(NodeId 1))
        |> update (TouchNode(NodeId 1))
        |> update (StartDrag(1.0, 1.0, 0.0))
        |> update (DragTo(2.0, 2.0, 0.0))
        |> update EndDrag
    viewModel.CanRedo state =! false
    viewModel.ShouldEnableOrbitControls state =! false

[<Fact>]
let ``ToggleSelection changes selection mode and clears selection`` () =
    let state =
        initialState
        |> update (SelectNode(NodeId 1))
        |> update (ToggleSelection MultiSelect)
    viewModel.IsSingleSelectEnabled state =! false
    viewModel.EnumerateSelectedTreeNodes state |> Seq.length =! 0

[<Fact>]
let ``Undo reverts to previous state`` () =
    let state1 =
        initialState
        |> update (SelectNode(NodeId 1))
        |> update (TouchNode(NodeId 1))
        |> update (StartDrag(1.0, 1.0, 0.0))
        |> update (DragTo(2.0, 2.0, 0.0))
        |> update EndDrag

    let state2 = state1 |> update Undo

    // After undo, node should be at original position.
    let selected = viewModel.EnumerateSelectedTreeNodes state2 |> Seq.toList
    let position =
        selected
        |> List.filter (fun n -> n.Id = NodeId 1)
        |> List.map _.Position
        |> List.tryHead
    position =! Some(1.0, 1.0, 0.0)

[<Fact>]
let ``Redo advances to next state`` () =
    let state1 =
        initialState
        |> update (SelectNode(NodeId 1))
        |> update (TouchNode(NodeId 1))
        |> update (StartDrag(1.0, 1.0, 0.0))
        |> update (DragTo(2.0, 2.0, 0.0))
        |> update EndDrag
        |> update Undo

    let state2 = state1 |> update Redo

    // After redo, node should be at updated position.
    let selected = viewModel.EnumerateSelectedTreeNodes state2 |> Seq.toList
    let position =
        selected
        |> List.filter (fun n -> n.Id = NodeId 1)
        |> List.map _.Position
        |> List.tryHead
    position =! Some(2.0, 2.0, 0.0)

[<Fact>]
let ``SelectNode when DragEnding does not change selection, resets drag`` () =
    // Setup state with DragEnding and node selected
    let state =
        initialState
        |> update (SelectNode(NodeId 1))
        |> update (TouchNode(NodeId 1))
        |> update (StartDrag(1.0, 1.0, 0.0))
        |> update (DragTo(2.0, 2.0, 0.0))
        |> update EndDrag
        |> update (SelectNode(NodeId 1)) // triggers DragEnding sub-case
    // Drag should be reset to NotDragging, selection unchanged (in terms of ID, not co-ordinates)
    viewModel.EnumerateSelectedTreeNodes state |> Seq.toList |> List.map _.Id =! [ node1.Id ]
    viewModel.ShouldEnableOrbitControls state =! true

[<Fact>]
let ``SelectNode when Dragging does nothing`` () =
    // Setup state with Dragging and node selected
    let state =
        initialState
        |> update (SelectNode(NodeId 1))
        |> update (TouchNode(NodeId 1))
        |> update (StartDrag(1.0, 1.0, 0.0))
        |> update (SelectNode(NodeId 1)) // triggers Dragging sub-case
    // Should be unchanged from before
    viewModel.EnumerateSelectedTreeNodes state |> Seq.toList =! [ node1 ]
    viewModel.ShouldEnableOrbitControls state =! false

[<Fact>]
let ``StartDrag without initial TouchNode does nothing`` () =
    // Setup state with no last touched node (no TouchNode sent).
    let state = update (StartDrag(1.0, 1.0, 0.0)) initialState
    // State should be unchanged
    state =! initialState

[<Fact>]
let ``DragTo when NotDragging does nothing`` () =
    // Setup state with NotDragging
    let state = update (DragTo(2.0, 2.0, 0.0)) initialState
    // State should be unchanged
    state =! initialState

[<Fact>]
let ``EndDrag when NotDragging does nothing`` () =
    // Setup state with NotDragging
    let state = update EndDrag initialState
    // State should be unchanged
    state =! initialState
