namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model
open Wilnaatahl.Model.FamilyGraph
open Wilnaatahl.Model.Initial
open Wilnaatahl.ViewModel.NodeState
open Wilnaatahl.ViewModel.UndoableState
#if FABLE_COMPILER
open Fable.Core
#endif

type DragData = {
    /// The position of the node that started the drag;
    /// Used to calculate new positions during the drag.
    Origin: float * float * float

    /// Last node to be touched before the drag operation started.
    LastTouchedNodeId: NodeId
}

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
    | StartDrag
    | DragBy of float * float * float
    | EndDrag
    | ToggleSelection of SelectionMode
    | TouchNode of NodeId // In this context, "touch" means "pointer down".
    | Undo
    | Redo
    | Animate of NodeId * float * float * float

module ViewState =
    type ViewState = private {
        History: UndoableState<NodeState>
        Families: RenderedFamily list
        Drag: DragState
        LastTouchedNodeId: NodeId option
        SelectionMode: SelectionMode
    }

    let createViewState nodes families = {
        History = createNodeState nodes |> createUndoableState
        Families = List.ofSeq families
        Drag = NotDragging
        LastTouchedNodeId = None
        SelectionMode = SingleSelect
    }

    let private areAnyNodesAnimating state =
        state.History |> current |> all |> Seq.exists (fun n -> n.IsAnimating)

    let canRedo state =
        canRedo state.History && not (areAnyNodesAnimating state)

    let canUndo state =
        canUndo state.History && not (areAnyNodesAnimating state)

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
        | Animate(nodeId, x, y, z) ->
            let newPosition = x, y, z
            let node = nodes |> findNode nodeId
            let delta = 0.01

            let isAnimationFinished =
                let tx, ty, tz = node.TargetPosition
                abs (x - tx) < delta && abs (y - ty) < delta && abs (z - tz) < delta

            let newNode = {
                node with
                    Position =
                        if isAnimationFinished then
                            node.TargetPosition
                        else
                            newPosition
                    IsAnimating = not isAnimationFinished
            }

            let updatedNodes = nodes |> replace nodeId newNode

            { state with History = commit updatedNodes }
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
                | SingleSelect -> {
                    state with
                        History = nodes |> deselectAll |> select nodeId |> commit
                        Drag = NotDragging
                  }
                | MultiSelect -> {
                    state with
                        History = nodes |> select nodeId |> commit
                        Drag = NotDragging
                  }
        | DeselectAll -> {
            state with
                History = nodes |> deselectAll |> commit
                Drag = NotDragging
          }
        | StartDrag ->
            match state.LastTouchedNodeId with
            | Some nodeId ->
                let node = nodes |> findNode nodeId

                // Dragging is not allowed on animating nodes since it does
                // weird things like snapshot nodes in the middle of automatic layout.
                if node.IsAnimating then
                    state
                else
                    // Use this opportunity to save the current node positions before
                    // they start changing for undo/redo.
                    {
                        state with
                            History = state.History |> saveCurrentForUndo
                            Drag = Dragging { Origin = node.Position; LastTouchedNodeId = nodeId }
                    }
            | None -> state // Shouldn't happen; Do nothing.
        | DragBy(moveX, moveY, moveZ) ->
            match state.Drag with
            | Dragging { Origin = originX, originY, originZ; LastTouchedNodeId = nodeId } ->

                // Find the previous position of the dragged node
                let node = nodes |> findNode nodeId
                let oldX, oldY, oldZ = node.Position
                let newX, newY, newZ = originX + moveX, originY + moveY, originZ + moveZ
                let dx, dy, dz = newX - oldX, newY - oldY, newZ - oldZ

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
                { state with History = clearRedo state.History; Drag = DragEnding }
            | DragEnding
            | NotDragging -> state // This can happen on de-selection clicks, so ignore it.
        | ToggleSelection mode ->
            // We clear the selection when toggling selection mode so you don't end up
            // confusing the user by having multiple nodes selected when in single-selection mode.
            {
                state with
                    History = nodes |> deselectAll |> commit
                    SelectionMode = mode
            }
        | TouchNode nodeId -> { state with LastTouchedNodeId = Some nodeId }
        | Undo ->
            let undoneHistory = undo state.History

            let newNodes =
                current state.History |> animateToNewNodePositions (current undoneHistory)

            { state with History = undoneHistory |> setCurrent newNodes }
        | Redo ->
            let redoneHistory = redo state.History

            let newNodes =
                current state.History |> animateToNewNodePositions (current redoneHistory)

            { state with History = redoneHistory |> setCurrent newNodes }

open ViewState

// Wrap ViewState functionality in an interface for easier consumption from TypeScript.
type IViewModel =
    abstract CanRedo: ViewState -> bool
    abstract CanUndo: ViewState -> bool
    abstract CreateInitialViewState: (seq<TreeNode> * seq<RenderedFamily>) -> ViewState
    abstract EnumerateFamilies: ViewState -> seq<RenderedFamily>
    abstract EnumerateChildren: ViewState -> RenderedFamily -> seq<TreeNode>
    abstract EnumerateParents: ViewState -> RenderedFamily -> TreeNode * TreeNode
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

// Functionality to initialize the core ViewModel data structures in an interface for easier consumption from TypeScript.
type IGraphViewFactory =
    abstract ExtractFamilies: FamilyGraph -> seq<TreeNode> -> seq<RenderedFamily>
    abstract FirstWilp: FamilyGraph -> WilpName
    abstract LayoutGraph: FamilyGraph -> WilpName -> seq<TreeNode>
    abstract LoadGraph: unit -> FamilyGraph

type GraphViewFactory() =
    interface IGraphViewFactory with
        member _.ExtractFamilies familyGraph nodes = Scene.extractFamilies familyGraph nodes

        member _.FirstWilp familyGraph = familyGraph |> huwilp |> Seq.head // ASSUMPTION: At least one Wilp is represented in the input data.

        member _.LayoutGraph familyGraph focusedWilp =
            let defaultSpacing = {
                X = Scene.defaultXSpacing
                Y = Scene.defaultYSpacing
                Z = Scene.defaultZSpacing
            }

            familyGraph |> Scene.layoutGraph defaultSpacing focusedWilp

        member _.LoadGraph() = createFamilyGraph peopleAndParents
