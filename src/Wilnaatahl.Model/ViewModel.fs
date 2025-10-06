namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model.Initial
open Wilnaatahl.ViewModel.NodeState
open Wilnaatahl.ViewModel.UndoableState
#if FABLE_COMPILER
open Fable.Core
#endif

type Family =
    { parents: NodeId * NodeId
      children: NodeId list }

type DragState =
    | Dragging of offset: float * float * float
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
            { history: UndoableState<NodeState>
              families: Family list
              drag: DragState
              lastTouchedNodeId: NodeId option
              selectionMode: SelectionMode }

    let createViewState nodes families =
        { history = createNodeState nodes |> createUndoableState
          families = List.ofSeq families
          drag = NotDragging
          lastTouchedNodeId = None
          selectionMode = SingleSelect }

    let canRedo state = canRedo state.history
    let canUndo state = canUndo state.history
    let enumerateFamilies state = state.families

    let enumerateChildren state family =
        let nodes = current state.history

        family.children
        |> List.map (fun childId -> nodes |> findNode childId)
        |> List.toSeq

    let enumerateParents state family =
        let nodes = current state.history
        let parent1Id, parent2Id = family.parents
        let parent1 = nodes |> findNode parent1Id
        let parent2 = nodes |> findNode parent2Id
        parent1, parent2

    let enumerateSelectedTreeNodes state = state.history |> current |> selected
    let enumerateUnselectedTreeNodes state = state.history |> current |> unselected

    let isSingleSelectEnabled state =
        state.selectionMode.IsSingleSelectEnabled

    let shouldEnableOrbitControls state = state.drag.ShouldEnableOrbitControls

    let update state msg =
        let nodes = current state.history
        let commit nodeState = state.history |> setCurrent nodeState

        match msg with
        | SelectNode nodeId ->
            if nodes |> isSelected nodeId then
                match state.drag with
                | NotDragging ->
                    // De-select currently selected node.
                    { state with history = nodes |> deselect nodeId |> commit }
                | DragEnding ->
                    // Ignore the click that ended the drag, as it was not a selection change.
                    { state with drag = NotDragging }
                | Dragging _ -> state // Shouldn't happen, so ignore it.
            else
                // Select new node:
                // - In SingleSelect mode, this either selects a node for the first time or replaces the previous selection.
                // - In MultiSelect mode, this adds to the current selection.
                match state.selectionMode with
                | SingleSelect ->
                    { state with
                        history = nodes |> deselectAll |> select nodeId |> commit
                        drag = NotDragging }
                | MultiSelect ->
                    { state with
                        history = nodes |> select nodeId |> commit
                        drag = NotDragging }
        | DeselectAll ->
            { state with
                history = nodes |> deselectAll |> commit
                drag = NotDragging }
        | StartDrag (px, py, pz) ->
            match state.lastTouchedNodeId with
            | Some nodeId ->
                // Calculate the offset between the click point and the co-ordinates
                // of the node that was dragged.
                let node = nodes |> findNode nodeId
                let nx, ny, nz = node.position
                let offset = nx - px, ny - py, nz - pz

                // Use this opportunity to save the current node positions before
                // they start changing for undo/redo.
                { state with
                    history = state.history |> saveCurrentForUndo
                    drag = Dragging offset }
            | None -> state // Shouldn't happen; Do nothing.
        | DragTo (px, py, pz) ->
            match state.drag with
            | Dragging (ox, oy, oz) ->
                match state.lastTouchedNodeId with
                | Some nodeId ->
                    // Find the original position of the dragged node
                    let origNode = nodes |> findNode nodeId
                    let origX, origY, origZ = origNode.position
                    let newX, newY, newZ = px + ox, py + oy, pz + oz
                    let dx, dy, dz = newX - origX, newY - origY, newZ - origZ

                    let updateNodePosition node =
                        let nx, ny, nz = node.position
                        let newPos = nx + dx, ny + dy, nz + dz
                        { node with position = newPos }

                    let updatedNodes = nodes |> mapSelected updateNodePosition
                    { state with history = commit updatedNodes }
                | None -> state
            | DragEnding
            | NotDragging -> state
        | EndDrag ->
            match state.drag with
            | Dragging _ ->
                // Drag is ending; Flush the redo history to avoid massive time-travel
                // confusion for the user.
                { state with
                    history = clearRedo state.history
                    drag = DragEnding }
            | DragEnding
            | NotDragging -> state // This can happen on de-selection clicks, so ignore it.
        | ToggleSelection mode ->
            // We clear the selection when toggling selection mode so you don't end up
            // confusing the user by having multiple nodes selected when in single-selection mode.
            { state with
                history = nodes |> deselectAll |> commit
                selectionMode = mode }
        | TouchNode nodeId -> { state with lastTouchedNodeId = Some nodeId }
        | Undo -> { state with history = undo state.history }
        | Redo -> { state with history = redo state.history }

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
        [ { id = NodeId 0
            position = -1.0, 0.0, 0.0
            person = people[0] }
          { id = NodeId 1
            position = 1.0, 0.0, 0.0
            person = people[1] }
          { id = NodeId 2
            position = -2.0, -2.0, 0.0
            person = people[2] }
          { id = NodeId 3
            position = 0.0, -2.0, 0.0
            person = people[3] }
          { id = NodeId 4
            position = 2.0, -2.0, 0.0
            person = people[4] } ]
        |> Seq.ofList

    let families =
        [ { parents = NodeId 0, NodeId 1
            children = [ NodeId 2; NodeId 3; NodeId 4 ] } ]
