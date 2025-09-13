namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model
open Wilnaatahl.Model.Initial

type Branch =
    { id: string
      parents: string * string
      children: string list }

type TreeNode =
    { id: string
      position: float * float * float
      person: Person }

type DragStart =
    { nodeId: string
      position: float * float * float // Position of the node at drag start
      offset: float * float * float } // Offset from node position to pointer at drag start

type DragState =
    // Represents the time between pointer down and actual start of drag, to avoid spurious clicks.
    | Tentative of DragStart
    // Represents an active drag operation based on pointer movement beyond a threshold.
    | Dragging of DragStart
    // Captures the state between pointer up and the final click, which should be ignored.
    | DragEnding
    | NotDragging
    member this.ShouldEnableOrbitControls =
        match this with
        | NotDragging -> true
        | _ -> false

type Msg =
    | SelectNode of string
    | DeselectNode
    | StartDrag of nodeId: string * pointer: float * float * float
    | DragTo of pointer: float * float * float
    | EndDrag
    | Undo
    | Redo

type ViewState =
    { history: UndoableState<Map<string, TreeNode>>
      branches: Branch list
      selectedNodeId: string option
      drag: DragState }
    static member Empty =
        { history = UndoableState.create Map.empty
          branches = []
          selectedNodeId = None
          drag = NotDragging }

    static member Update state msg =
        match msg with
        | SelectNode nodeId ->
            match state.selectedNodeId with
            | Some selected when selected = nodeId ->
                match state.drag with
                | NotDragging ->
                    // De-select currently selected node.
                    { state with
                        selectedNodeId = None
                        drag = NotDragging }
                | Tentative _ ->
                    // De-select currently selected node.
                    // A drag was maybe about to start, but we're going to pretend it wasn't.
                    // That includes an automatic undo to not clutter the undo history.
                    { state with
                        history = state.history.DiscardLastUndo()
                        selectedNodeId = None
                        drag = NotDragging }
                | DragEnding ->
                    // Ignore the click that ended the drag, as it was not a selection change.
                    { state with drag = NotDragging }
                | Dragging _ -> state // Shouldn't happen, so ignore it.
            | _ ->
                // Select new node, either for the first time or replacing previous selection
                { state with
                    selectedNodeId = Some nodeId
                    drag = NotDragging }
        | DeselectNode ->
            match state.selectedNodeId with
            | Some _ ->
                { state with
                    selectedNodeId = None
                    drag = NotDragging }
            | None -> state
        | StartDrag (nodeId, px, py, pz) ->
            match Map.tryFind nodeId state.history.Current with
            | Some node ->
                let nx, ny, nz = node.position
                let offset = nx - px, ny - py, nz - pz

                // Use this opportunity to save the current node positions before
                // they start changing for undo/redo.
                { state with
                    history = state.history.CopyCurrentToUndo()
                    drag =
                        Tentative
                            { nodeId = nodeId
                              position = node.position
                              offset = offset } }
            | None -> state
        | DragTo (px, py, _) ->
            let updateNodePosition (node: TreeNode) drag state =
                let ox, oy, _ = drag.offset
                let _, _, nz = node.position
                let newPos = px + ox, py + oy, nz // Keep z fixed at original value
                let updatedNode = { node with position = newPos }

                let updatedNodes =
                    state.history.Current
                    |> Map.add node.id updatedNode

                newPos, updatedNodes

            match state.drag with
            | Tentative drag ->
                match Map.tryFind drag.nodeId state.history.Current with
                | Some node ->
                    let newPos, updatedNodes = updateNodePosition node drag state

                    // Calculate distance in 3D (including z) between newPos and the original node position.
                    let origX, origY, origZ = drag.position
                    let newX, newY, newZ = newPos
                    let dx, dy, dz = newX - origX, newY - origY, newZ - origZ
                    let dist = sqrt (dx * dx + dy * dy + dz * dz)
                    let threshold = 0.1 // ~10cm in world units; adjust as needed

                    if dist > threshold then
                        // Transition to Dragging state.
                        { state with
                            history = state.history.SetCurrent updatedNodes
                            drag = Dragging drag }
                    else
                        // Stay in Tentative state, with the updated node position.
                        { state with history = state.history.SetCurrent updatedNodes }
                | None -> state
            | Dragging drag ->
                match Map.tryFind drag.nodeId state.history.Current with
                | Some node ->
                    let _, updatedNodes = updateNodePosition node drag state
                    { state with history = state.history.SetCurrent updatedNodes }
                | None -> state
            | DragEnding
            | NotDragging -> state
        | EndDrag ->
            match state.drag with
            | Tentative _ ->
                // If we were in Tentative state, reset to NotDragging and
                // do an automatic undo to not clutter the undo history.
                { state with
                    history = state.history.DiscardLastUndo()
                    drag = NotDragging }
            | _ ->
                // If the drag really is ending, we can flush the redo history
                // to avoid massive time-travel confusion for the user.
                { state with
                    history = state.history.ClearRedo()
                    drag = DragEnding }
        | Undo -> { state with history = state.history.Undo() }
        | Redo -> { state with history = state.history.Redo() }

type IViewModel =
    abstract CanRedo: ViewState -> bool
    abstract CanUndo: ViewState -> bool
    abstract CreateInitialViewState: Map<string, TreeNode> -> seq<Branch> -> ViewState
    abstract EnumerateBranches: ViewState -> seq<Branch>
    abstract EnumerateChildren: ViewState -> Branch -> seq<TreeNode>
    abstract EnumerateParents: ViewState -> Branch -> TreeNode * TreeNode
    abstract EnumerateTreeNodes: ViewState -> seq<TreeNode>
    abstract GetDraggingNodeId: ViewState -> string option
    abstract ShouldEnableOrbitControls: ViewState -> bool
    abstract Update: ViewState -> Msg -> ViewState

type ViewModel() =
    interface IViewModel with
        member _.CanRedo state = state.history.CanRedo

        member _.CanUndo state = state.history.CanUndo

        member _.CreateInitialViewState nodes branches =
            { ViewState.Empty with
                history = UndoableState.create nodes
                branches = List.ofSeq branches }

        member _.EnumerateBranches state = state.branches

        member _.EnumerateChildren state branch =
            branch.children
            |> List.choose (fun childId -> Map.tryFind childId state.history.Current)
            |> List.toSeq

        member _.EnumerateParents state branch =
            let parent1Id, parent2Id = branch.parents
            let parent1 = Map.find parent1Id state.history.Current
            let parent2 = Map.find parent2Id state.history.Current
            parent1, parent2

        member _.EnumerateTreeNodes state =
            state.history.Current |> Map.values :> seq<TreeNode>

        member _.GetDraggingNodeId state =
            match state.drag with
            | Tentative drag
            | Dragging drag -> Some drag.nodeId
            | _ -> None

        member _.ShouldEnableOrbitControls state = state.drag.ShouldEnableOrbitControls

        member _.Update state msg = ViewState.Update state msg

module Initial =
    let nodes =
        [ "root1",
          { id = "root1"
            position = -1.0, 0.0, 0.0
            person = people[0] }
          "root2",
          { id = "root2"
            position = 1.0, 0.0, 0.0
            person = people[1] }
          "child1",
          { id = "child1"
            position = -2.0, -2.0, 0.0
            person = people[2] }
          "child2",
          { id = "child2"
            position = 0.0, -2.0, 0.0
            person = people[3] }
          "child3",
          { id = "child3"
            position = 2.0, -2.0, 0.0
            person = people[4] } ]
        |> Map.ofList

    let branches =
        [ { id = "branch"
            parents = "root1", "root2"
            children = [ "child1"; "child2"; "child3" ] } ]
