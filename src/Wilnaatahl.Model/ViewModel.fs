namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model.Initial
open Wilnaatahl.ViewModel.NodeState
open Wilnaatahl.ViewModel.UndoableState
#if FABLE_COMPILER
open Fable.Core
#endif

type Family =
    { Parents: NodeId * NodeId
      Children: NodeId list }

type DragData =
    { Offset: float * float * float
      LastTouchedNodeId: NodeId }

type DragState =
    | Dragging of DragData
    // Captures the state between pointer up and the final click, which should be ignored.
    | DragEnding
    | NotDragging
    member this.ShouldEnableOrbitControls =
        match this with
        | NotDragging -> true
        | DragEnding
        | Dragging _ -> false

#if FABLE_COMPILER
[<StringEnum>]
#endif
type SelectionMode =
    | SingleSelect
    | MultiSelect
    member this.IsSingleSelectEnabled =
        match this with
        | SingleSelect -> true
        | MultiSelect -> false

type Msg =
    | SelectNode of NodeId
    | DeselectAll
    | StartDrag of origin: float * float * float
    | DragTo of position: float * float * float
    | EndDrag
    | ToggleSelection of SelectionMode
    | TouchNode of NodeId // In this context, "touch" means "pointer down".
    | Undo
    | Redo

module ViewState =
    type ViewState =
        private
            { History: UndoableState<NodeState>
              Families: Family list
              Drag: DragState
              LastTouchedNodeId: NodeId option
              SelectionMode: SelectionMode }

    let createViewState nodes families =
        { History = createNodeState nodes |> createUndoableState
          Families = List.ofSeq families
          Drag = NotDragging
          LastTouchedNodeId = None
          SelectionMode = SingleSelect }

    let canRedo state = canRedo state.History
    let canUndo state = canUndo state.History
    let enumerateFamilies state = state.Families

    let enumerateChildren state family =
        let nodes = current state.History

        family.Children
        |> List.map (fun childId -> nodes |> findNode childId)
        |> List.toSeq

    let enumerateParents state family =
        let nodes = current state.History
        let parent1Id, parent2Id = family.Parents
        let parent1 = nodes |> findNode parent1Id
        let parent2 = nodes |> findNode parent2Id
        parent1, parent2

    let enumerateSelectedTreeNodes state = state.History |> current |> selected
    let enumerateUnselectedTreeNodes state = state.History |> current |> unselected

    let isSingleSelectEnabled state =
        state.SelectionMode.IsSingleSelectEnabled

    let shouldEnableOrbitControls state = state.Drag.ShouldEnableOrbitControls

    let update state msg =
        let nodes = current state.History
        let commit nodeState = state.History |> setCurrent nodeState

        match msg with
        | SelectNode nodeId ->
            if nodes |> isSelected nodeId then
                match state.Drag with
                | NotDragging ->
                    // De-select currently selected node.
                    { state with History = nodes |> deselect nodeId |> commit }
                | DragEnding ->
                    // Ignore the click that ended the drag, as it was not a selection change.
                    { state with Drag = NotDragging }
                | Dragging _ -> state // Shouldn't happen, so ignore it.
            else
                // Select new node:
                // - In SingleSelect mode, this either selects a node for the first time or replaces the previous selection.
                // - In MultiSelect mode, this adds to the current selection.
                match state.SelectionMode with
                | SingleSelect ->
                    { state with
                        History = nodes |> deselectAll |> select nodeId |> commit
                        Drag = NotDragging }
                | MultiSelect ->
                    { state with
                        History = nodes |> select nodeId |> commit
                        Drag = NotDragging }
        | DeselectAll ->
            { state with
                History = nodes |> deselectAll |> commit
                Drag = NotDragging }
        | StartDrag (px, py, pz) ->
            match state.LastTouchedNodeId with
            | Some nodeId ->
                // Calculate the offset between the click point and the co-ordinates
                // of the node that was dragged.
                let node = nodes |> findNode nodeId
                let nx, ny, nz = node.Position
                let offset = nx - px, ny - py, nz - pz

                // Use this opportunity to save the current node positions before
                // they start changing for undo/redo.
                { state with
                    History = state.History |> saveCurrentForUndo
                    Drag =
                        Dragging
                            { Offset = offset
                              LastTouchedNodeId = nodeId } }
            | None -> state // Shouldn't happen; Do nothing.
        | DragTo (px, py, pz) ->
            match state.Drag with
            | Dragging { Offset = ox, oy, oz
                         LastTouchedNodeId = nodeId } ->

                // Find the original position of the dragged node
                let origNode = nodes |> findNode nodeId
                let origX, origY, origZ = origNode.Position
                let newX, newY, newZ = px + ox, py + oy, pz + oz
                let dx, dy, dz = newX - origX, newY - origY, newZ - origZ

                let updateNodePosition node =
                    let nx, ny, nz = node.Position
                    let newPos = nx + dx, ny + dy, nz + dz
                    { node with Position = newPos }

                let updatedNodes = nodes |> mapSelected updateNodePosition
                { state with History = commit updatedNodes }
            | DragEnding
            | NotDragging -> state
        | EndDrag ->
            match state.Drag with
            | Dragging _ ->
                // Drag is ending; Flush the redo history to avoid massive time-travel
                // confusion for the user.
                { state with
                    History = clearRedo state.History
                    Drag = DragEnding }
            | DragEnding
            | NotDragging -> state // This can happen on de-selection clicks, so ignore it.
        | ToggleSelection mode ->
            // We clear the selection when toggling selection mode so you don't end up
            // confusing the user by having multiple nodes selected when in single-selection mode.
            { state with
                History = nodes |> deselectAll |> commit
                SelectionMode = mode }
        | TouchNode nodeId -> { state with LastTouchedNodeId = Some nodeId }
        | Undo -> { state with History = undo state.History }
        | Redo -> { state with History = redo state.History }

open ViewState

// Wrap ViewState functionality in an interface for easier consumption from TypeScript.
type IViewModel =
    abstract CanRedo: ViewState -> bool
    abstract CanUndo: ViewState -> bool
    abstract CreateInitialViewState: (seq<TreeNode> * seq<Family>) -> ViewState
    abstract EnumerateFamilies: ViewState -> seq<Family>
    abstract EnumerateChildren: ViewState -> Family -> seq<TreeNode>
    abstract EnumerateParents: ViewState -> Family -> TreeNode * TreeNode
    abstract EnumerateSelectedTreeNodes: ViewState -> seq<TreeNode>
    abstract EnumerateUnselectedTreeNodes: ViewState -> seq<TreeNode>
    abstract IsSingleSelectEnabled: ViewState -> bool
    abstract ShouldEnableOrbitControls: ViewState -> bool
    abstract Update: ViewState -> Msg -> ViewState

type ViewModel() =
    interface IViewModel with
        member _.CanRedo state = canRedo state
        member _.CanUndo state = canUndo state

        // This is intentionally a single argument of tuple type so that useReducer can pass in a single value.
        member _.CreateInitialViewState((nodes, families)) = createViewState nodes families
        member _.EnumerateFamilies state = enumerateFamilies state
        member _.EnumerateChildren state family = enumerateChildren state family
        member _.EnumerateParents state family = enumerateParents state family
        member _.EnumerateSelectedTreeNodes state = enumerateSelectedTreeNodes state
        member _.EnumerateUnselectedTreeNodes state = enumerateUnselectedTreeNodes state
        member _.IsSingleSelectEnabled state = isSingleSelectEnabled state
        member _.ShouldEnableOrbitControls state = shouldEnableOrbitControls state
        member _.Update state msg = update state msg

module Initial =
    let nodes =
        [ { Id = NodeId 0
            Position = -1.0, 0.0, 0.0
            Person = people[0] }
          { Id = NodeId 1
            Position = 1.0, 0.0, 0.0
            Person = people[1] }
          { Id = NodeId 2
            Position = -2.0, -2.0, 0.0
            Person = people[2] }
          { Id = NodeId 3
            Position = 0.0, -2.0, 0.0
            Person = people[3] }
          { Id = NodeId 4
            Position = 2.0, -2.0, 0.0
            Person = people[4] } ]
        |> Seq.ofList

    let families =
        [ { Parents = NodeId 0, NodeId 1
            Children = [ NodeId 2; NodeId 3; NodeId 4 ] } ]
