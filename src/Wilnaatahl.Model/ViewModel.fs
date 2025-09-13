namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model.Initial
open Wilnaatahl.ViewModel.NodeState
open Wilnaatahl.ViewModel.UndoableState

type Branch =
    { id: string
      parents: string * string
      children: string list }

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
    | DeselectAll
    | StartDrag of nodeId: string * pointer: float * float * float
    | DragTo of pointer: float * float * float
    | EndDrag
    | Undo
    | Redo

type ViewState =
    { history: UndoableState<NodeState>
      branches: Branch list
      drag: DragState }

    static member Update state msg =
        let nodes = current state.history
        let commit nodeState = state.history |> setCurrent nodeState

        match msg with
        | SelectNode nodeId ->
            if nodes |> isSelected nodeId then
                match state.drag with
                | NotDragging ->
                    // De-select currently selected node.
                    { state with
                        history = nodes |> deselect nodeId |> commit
                        drag = NotDragging }
                | Tentative _ ->
                    // De-select currently selected node.
                    // A drag was maybe about to start, but we're going to pretend it wasn't.
                    // That includes an automatic undo to not clutter the undo history.
                    { state with
                        history =
                            nodes
                            |> deselect nodeId
                            |> commit
                            |> discardLastUndo
                        drag = NotDragging }
                | DragEnding ->
                    // Ignore the click that ended the drag, as it was not a selection change.
                    { state with drag = NotDragging }
                | Dragging _ -> state // Shouldn't happen, so ignore it.
            else
                // Select new node, either for the first time or replacing previous selection
                { state with
                    history = nodes |> select nodeId |> commit
                    drag = NotDragging }
        | DeselectAll ->
            { state with
                history = nodes |> deselectAll |> commit
                drag = NotDragging }
        | StartDrag (nodeId, px, py, pz) ->
            if nodes |> isSelected nodeId then
                let node = nodes |> findNode nodeId
                let nx, ny, nz = node.position
                let offset = nx - px, ny - py, nz - pz

                // Use this opportunity to save the current node positions before
                // they start changing for undo/redo.
                { state with
                    history = state.history |> copyCurrentToUndo
                    drag =
                        Tentative
                            { nodeId = nodeId
                              position = node.position
                              offset = offset } }
            else
                state // Ignore drag attempts on unselected nodes.
        | DragTo (px, py, _) ->
            let updateNodePosition (node: TreeNode) drag (nodeState: NodeState) =
                let ox, oy, _ = drag.offset
                let _, _, nz = node.position
                let newPos = px + ox, py + oy, nz // Keep z fixed at original value
                let updatedNode = { node with position = newPos }
                let updatedNodes = nodeState |> setNode node.id updatedNode
                newPos, updatedNodes

            match state.drag with
            | Tentative drag ->
                let node = nodes |> findNode drag.nodeId
                let newPos, updatedNodes = updateNodePosition node drag nodes

                // Calculate distance in 3D (including z) between newPos and the original node position.
                let origX, origY, origZ = drag.position
                let newX, newY, newZ = newPos
                let dx, dy, dz = newX - origX, newY - origY, newZ - origZ
                let dist = sqrt (dx * dx + dy * dy + dz * dz)
                let threshold = 0.1 // ~10cm in world units; adjust as needed

                if dist > threshold then
                    // Transition to Dragging state.
                    { state with
                        history = commit updatedNodes
                        drag = Dragging drag }
                else
                    // Stay in Tentative state, with the updated node position.
                    { state with history = commit updatedNodes }
            | Dragging drag ->
                let node = nodes |> findNode drag.nodeId
                let _, updatedNodes = updateNodePosition node drag nodes
                { state with history = commit updatedNodes }
            | DragEnding
            | NotDragging -> state
        | EndDrag ->
            match state.drag with
            | Tentative _ ->
                // If we were in Tentative state, reset to NotDragging and
                // do an automatic undo to not clutter the undo history.
                { state with
                    history = discardLastUndo state.history
                    drag = NotDragging }
            | _ ->
                // If the drag really is ending, we can flush the redo history
                // to avoid massive time-travel confusion for the user.
                { state with
                    history = clearRedo state.history
                    drag = DragEnding }
        | Undo -> { state with history = undo state.history }
        | Redo -> { state with history = redo state.history }

type IViewModel =
    abstract CanRedo: ViewState -> bool
    abstract CanUndo: ViewState -> bool
    abstract CreateInitialViewState: Map<string, TreeNode> -> seq<Branch> -> ViewState
    abstract EnumerateBranches: ViewState -> seq<Branch>
    abstract EnumerateChildren: ViewState -> Branch -> seq<TreeNode>
    abstract EnumerateParents: ViewState -> Branch -> TreeNode * TreeNode
    abstract EnumerateTreeNodes: ViewState -> seq<TreeNode * bool>
    abstract GetDraggingNodeId: ViewState -> string option
    abstract ShouldEnableOrbitControls: ViewState -> bool
    abstract Update: ViewState -> Msg -> ViewState

type ViewModel() =
    interface IViewModel with
        member _.CanRedo state = canRedo state.history
        member _.CanUndo state = canUndo state.history

        member _.CreateInitialViewState nodes branches =
            { history = createNodeState nodes |> createUndoableState
              branches = List.ofSeq branches
              drag = NotDragging }

        member _.EnumerateBranches state = state.branches

        member _.EnumerateChildren state branch =
            let nodes = current state.history

            branch.children
            |> List.map (fun childId -> nodes |> findNode childId)
            |> List.toSeq

        member _.EnumerateParents state branch =
            let nodes = current state.history
            let parent1Id, parent2Id = branch.parents
            let parent1 = nodes |> findNode parent1Id
            let parent2 = nodes |> findNode parent2Id
            parent1, parent2

        member _.EnumerateTreeNodes state = state.history |> current |> values

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
