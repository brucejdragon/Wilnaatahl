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
              let state = initialState
              Expect.isFalse (viewModel.CanUndo state) "Cannot undo initially"
              Expect.isFalse (viewModel.CanRedo state) "Cannot redo initially"
              // Only drag operations are undoable
              let state' =
                  state
                  |> update (TouchNode(NodeId 1))
                  |> update (StartDrag(1.0, 1.0, 0.0))
                  |> update (DragTo(2.0, 2.0, 0.0))
                  |> update EndDrag

              Expect.isTrue (viewModel.CanUndo state') "Can undo after drag operation"
              let state'' = state' |> update Undo
              Expect.isTrue (viewModel.CanRedo state'') "Can redo after undo"
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
              let state = initialState
              Expect.isTrue (viewModel.ShouldEnableOrbitControls state) "Orbit controls enabled initially"

              let state' =
                  state
                  |> update (TouchNode(NodeId 1))
                  |> update (StartDrag(1.0, 1.0, 0.0))

              Expect.isFalse (viewModel.ShouldEnableOrbitControls state') "Orbit controls disabled when dragging"
          }

          // TODO: Improve coverage. These are currently highly duplicative of the other test cases (blame Copilot, not me).
          test "Update handles SelectNode, DeselectAll, Undo, Redo, ToggleSelection, TouchNode, DragTo, EndDrag" {
              let state = initialState
              let state1 = state |> update (SelectNode(NodeId 1))

              Expect.equal
                  (viewModel.EnumerateSelectedTreeNodes state1
                   |> Seq.toList)
                  [ node1 ]
                  "Node 1 selected"

              let state2 = state1 |> update DeselectAll

              Expect.equal
                  (viewModel.EnumerateSelectedTreeNodes state2
                   |> Seq.length)
                  0
                  "All nodes deselected"
              // Undo/Redo only affect drag operations
              let dragState =
                  state
                  |> update (TouchNode(NodeId 1))
                  |> update (StartDrag(1.0, 1.0, 0.0))
                  |> update (DragTo(2.0, 2.0, 0.0))
                  |> update EndDrag

              let state3 = dragState |> update Undo
              Expect.isTrue (viewModel.CanRedo state3) "Can redo after undoing drag"
              let state4 = state3 |> update Redo
              Expect.isTrue (viewModel.CanUndo state4) "Can undo after redoing drag"
              let state5 = state |> update (ToggleSelection MultiSelect)
              Expect.isFalse (viewModel.IsSingleSelectEnabled state5) "Selection mode toggled to MultiSelect"
              let state6 = state |> update (TouchNode(NodeId 2))
              // Can't directly check lastTouchedNodeId, but can check that a drag works after touching
              let dragState2 =
                  state6
                  |> update (StartDrag(2.0, 2.0, 0.0))
                  |> update (DragTo(3.0, 3.0, 0.0))
                  |> update EndDrag

              Expect.isTrue (viewModel.CanUndo dragState2) "Drag after touch is undoable"
          } ]

Mocha.runTests viewModelTests |> ignore
