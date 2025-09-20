namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model.Initial
open Wilnaatahl.ViewModel.NodeState
open Wilnaatahl.ViewModel.UndoableState

type Branch =
    { id: string
      parents: string * string
      children: string list }

type DragState =
    | Dragging of offset: float * float * float
    // Captures the state between pointer up and the final click, which should be ignored.
    | DragEnding
    | NotDragging
    member this.ShouldEnableOrbitControls =
        match this with
        | DragEnding
        | NotDragging -> true
        | Dragging _ -> false

type Msg =
    | SelectNode of string
    | DeselectAll
    | StartDrag of origin: float * float * float
    | DragTo of position: float * float * float
    | TouchNode of string // In this context, "touch" means "pointer down".
    | EndDrag
    | Undo
    | Redo

type ViewState =
    { history: UndoableState<NodeState>
      branches: Branch list
      drag: DragState
      lastTouchedNodeId: string option }

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
                    history = state.history |> copyCurrentToUndo
                    drag = Dragging offset }
            | None -> state // Shouldn't happen; Do nothing.
        | DragTo (px, py, pz) ->
            match state.drag with
            | Dragging (ox, oy, oz) ->
                let updateNodePosition nodeState node =
                    let newPos = px + ox, py + oy, pz + oz
                    let updatedNode = { node with position = newPos }
                    nodeState |> setNode node.id updatedNode

                let updatedNodes =
                    selected nodes
                    |> Seq.fold updateNodePosition nodes

                { state with history = commit updatedNodes }
            | DragEnding
            | NotDragging -> state
        | EndDrag ->
            // Drag is ending; Flush the redo history to avoid massive time-travel
            // confusion for the user.
            { state with
                history = clearRedo state.history
                drag = DragEnding }
        | TouchNode nodeId -> { state with lastTouchedNodeId = Some nodeId }
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
    abstract ShouldEnableOrbitControls: ViewState -> bool
    abstract Update: ViewState -> Msg -> ViewState

type ViewModel() =
    interface IViewModel with
        member _.CanRedo state = canRedo state.history
        member _.CanUndo state = canUndo state.history

        member _.CreateInitialViewState nodes branches =
            { history = createNodeState nodes |> createUndoableState
              branches = List.ofSeq branches
              drag = NotDragging
              lastTouchedNodeId = None }

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
