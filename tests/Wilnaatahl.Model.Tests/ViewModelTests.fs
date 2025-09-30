module Wilnaatahl.Tests.ViewModelTests

open Fable.Mocha
open Wilnaatahl.ViewModel
open Wilnaatahl.Tests.NodeStateTests

let family =
    { parents = NodeId 1, NodeId 2
      children = [ NodeId 3; NodeId 4 ] }

let families = [ family ]

let viewModel = ViewModel() :> IViewModel

let initialState = viewModel.CreateInitialViewState(initialNodes, families)

let update msg state = viewModel.Update state msg

let viewModelTests =
    testList
        "ViewModel"
        [ test "CanUndo and CanRedo reflect undo/redo state" {
              Expect.isFalse (viewModel.CanUndo initialState) "Cannot undo initially"
              Expect.isFalse (viewModel.CanRedo initialState) "Cannot redo initially"
              // Only drag operations are undoable
              let state =
                  initialState
                  |> update (TouchNode(NodeId 1))
                  |> update (StartDrag(1.0, 1.0, 0.0))
                  |> update (DragTo(2.0, 2.0, 0.0))
                  |> update EndDrag

              Expect.isTrue (viewModel.CanUndo state) "Can undo after drag operation"
              let state' = state |> update Undo
              Expect.isTrue (viewModel.CanRedo state') "Can redo after undo"
          }

          test "CreateInitialViewState sets up nodes and families" {
              let state = initialState

              let nodes =
                  viewModel.EnumerateUnselectedTreeNodes state
                  |> Seq.toList

              let fams = viewModel.EnumerateFamilies state |> Seq.toList
              Expect.equal nodes initialNodes "Initial nodes match"
              Expect.equal fams families "Initial families match"
          }

          test "EnumerateSelectedTreeNodes and EnumerateUnselectedTreeNodes reflect selection" {
              let state = initialState |> update (SelectNode(NodeId 1))

              let selected =
                  viewModel.EnumerateSelectedTreeNodes state
                  |> Seq.toList

              let unselected =
                  viewModel.EnumerateUnselectedTreeNodes state
                  |> Seq.toList

              Expect.equal selected [ node1 ] "Node 1 selected"
              Expect.equal (Set.ofList unselected) (Set.ofList [ node2; node3; node4 ]) "Other nodes unselected"
          }

          test "EnumerateFamilies returns all families" {
              let fams =
                  viewModel.EnumerateFamilies initialState
                  |> Seq.toList

              Expect.equal fams families "Families returned"
          }

          test "EnumerateChildren returns correct children" {
              let children =
                  viewModel.EnumerateChildren initialState family
                  |> Seq.toList

              Expect.equal children [ node3; node4 ] "Children returned"
          }

          test "EnumerateParents returns correct parents" {
              let p1, p2 = viewModel.EnumerateParents initialState family
              Expect.equal p1 node1 "Parent 1 correct"
              Expect.equal p2 node2 "Parent 2 correct"
          }

          test "IsSingleSelectEnabled reflects selection mode" {
              let state = initialState
              Expect.isTrue (viewModel.IsSingleSelectEnabled state) "SingleSelect enabled initially"
              let state' = state |> update (ToggleSelection MultiSelect)
              Expect.isFalse (viewModel.IsSingleSelectEnabled state') "MultiSelect disables single select"
          }

          test "ShouldEnableOrbitControls reflects drag state" {
              Expect.isTrue (viewModel.ShouldEnableOrbitControls initialState) "Orbit controls enabled initially"

              let state =
                  initialState
                  |> update (TouchNode(NodeId 1))
                  |> update (StartDrag(1.0, 1.0, 0.0))

              Expect.isFalse (viewModel.ShouldEnableOrbitControls state) "Orbit controls disabled when dragging"
          }

          test "SelectNode selects and deselects nodes correctly" {
              // Select node 1
              let state1 = initialState |> update (SelectNode(NodeId 1))

              Expect.equal
                  (viewModel.EnumerateSelectedTreeNodes state1
                   |> Seq.toList)
                  [ node1 ]
                  "Node 1 selected"
              // Deselect node 1 by selecting again
              let state2 = state1 |> update (SelectNode(NodeId 1))

              Expect.equal
                  (viewModel.EnumerateSelectedTreeNodes state2
                   |> Seq.length)
                  0
                  "Node 1 deselected"
              // MultiSelect mode: select node 1, then node 2
              let state3 =
                  initialState
                  |> update (ToggleSelection MultiSelect)
                  |> update (SelectNode(NodeId 1))
                  |> update (SelectNode(NodeId 2))

              let selected =
                  viewModel.EnumerateSelectedTreeNodes state3
                  |> Seq.map _.id
                  |> Set.ofSeq

              Expect.equal selected (Set.ofList [ NodeId 1; NodeId 2 ]) "Nodes 1 and 2 selected in MultiSelect"
          }

          test "DeselectAll clears all selections" {
              let state =
                  initialState
                  |> update (ToggleSelection MultiSelect)
                  |> update (SelectNode(NodeId 1))
                  |> update (SelectNode(NodeId 2))
                  |> update DeselectAll

              Expect.equal
                  (viewModel.EnumerateSelectedTreeNodes state
                   |> Seq.length)
                  0
                  "All nodes deselected"
          }

          test "StartDrag sets drag state and enables undo" {
              let state =
                  initialState
                  |> update (SelectNode(NodeId 1))
                  |> update (TouchNode(NodeId 1))
                  |> update (StartDrag(1.0, 1.0, 0.0))

              Expect.isFalse (viewModel.ShouldEnableOrbitControls state) "Orbit controls disabled when dragging"
              Expect.isTrue (viewModel.CanUndo state) "Can undo after drag started"
          }

          test "DragTo updates selected node positions" {
              let state =
                  initialState
                  |> update (SelectNode(NodeId 1))
                  |> update (TouchNode(NodeId 1))
                  |> update (StartDrag(1.0, 1.0, 0.0))
                  |> update (DragTo(2.0, 2.0, 0.0))

              let selected =
                  viewModel.EnumerateSelectedTreeNodes state
                  |> Seq.toList

              let node = selected |> List.filter (fun n -> n.id = NodeId 1) |> List.head

              Expect.equal node.position (2.0, 2.0, 0.0) "Node 1 position updated by drag"
          }

          test "EndDrag sets DragEnding and clears redo" {
              let state =
                  initialState
                  |> update (SelectNode(NodeId 1))
                  |> update (TouchNode(NodeId 1))
                  |> update (StartDrag(1.0, 1.0, 0.0))
                  |> update (DragTo(2.0, 2.0, 0.0))
                  |> update EndDrag

              Expect.isFalse (viewModel.CanRedo state) "Redo history cleared after drag ends"
              Expect.isFalse (viewModel.ShouldEnableOrbitControls state) "Orbit controls disabled in DragEnding"
          }

          test "ToggleSelection changes selection mode and clears selection" {
              let state =
                initialState
                |> update (SelectNode(NodeId 1))
                |> update (ToggleSelection MultiSelect)

              Expect.isFalse (viewModel.IsSingleSelectEnabled state) "Selection mode is MultiSelect"

              Expect.equal
                  (viewModel.EnumerateSelectedTreeNodes state
                   |> Seq.length)
                  0
                  "Selection cleared on mode toggle"
          }

          test "Undo reverts to previous state" {
              let state1 =
                  initialState
                  |> update (SelectNode(NodeId 1))
                  |> update (TouchNode(NodeId 1))
                  |> update (StartDrag(1.0, 1.0, 0.0))
                  |> update (DragTo(2.0, 2.0, 0.0))
                  |> update EndDrag

              let state2 = state1 |> update Undo
              // After undo, node should be at original position.
              let selected =
                  viewModel.EnumerateSelectedTreeNodes state2
                  |> Seq.toList

              let node =
                  selected
                  |> List.filter (fun n -> n.id = NodeId 1)
                  |> List.head

              Expect.equal node.position (1.0, 1.0, 0.0) ""
          }

          test "Redo advances to next state" {
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
              let selected =
                  viewModel.EnumerateSelectedTreeNodes state2
                  |> Seq.toList

              let node =
                  selected
                  |> List.filter (fun n -> n.id = NodeId 1)
                  |> List.head

              Expect.equal node.position (2.0, 2.0, 0.0) ""
          } ]

Mocha.runTests viewModelTests |> ignore
