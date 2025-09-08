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

type ViewState =
    { nodes: Map<string, TreeNode>
      branches: Branch list
      selectedNodeId: string option
      drag: DragState }
    static member Empty =
        { nodes = Map.empty
          branches = []
          selectedNodeId = None
          drag = NotDragging }

    static member Update state msg =
        match msg with
        | SelectNode nodeId ->
            match state.selectedNodeId with
            | Some selected when selected = nodeId ->
                match state.drag with
                | NotDragging
                | Tentative _ ->
                    // De-select currently selected node.
                    // If a drag was maybe about to start, no it wasn't.
                    { state with
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
        | StartDrag (nodeId, px, py, pz) ->
            match Map.tryFind nodeId state.nodes with
            | Some node ->
                let nx, ny, nz = node.position
                let offset = nx - px, ny - py, nz - pz

                { state with
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
                let updatedNodes = state.nodes |> Map.add node.id updatedNode
                newPos, updatedNodes

            match state.drag with
            | Tentative drag ->
                match Map.tryFind drag.nodeId state.nodes with
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
                            nodes = updatedNodes
                            drag = Dragging drag }
                    else
                        // Stay in Tentative state, with the updated node position.
                        { state with nodes = updatedNodes }
                | None -> state
            | Dragging drag ->
                match Map.tryFind drag.nodeId state.nodes with
                | Some node ->
                    let _, updatedNodes = updateNodePosition node drag state
                    { state with nodes = updatedNodes }
                | None -> state
            | DragEnding
            | NotDragging -> state
        | EndDrag ->
            match state.drag with
            | Tentative _ ->
                // If we were in Tentative state, just reset to NotDragging.
                { state with drag = NotDragging }
            | _ -> { state with drag = DragEnding }
        | DeselectNode ->
            match state.selectedNodeId with
            | Some _ ->
                { state with
                    selectedNodeId = None
                    drag = NotDragging }
            | None -> state

type IViewModel =
    abstract CreateInitialViewState: Map<string, TreeNode> -> seq<Branch> -> ViewState
    abstract EnumerateBranches: ViewState -> seq<Branch>
    abstract EnumerateChildren: ViewState -> Branch -> seq<TreeNode>
    abstract EnumerateTreeNodes: ViewState -> seq<TreeNode>
    abstract GetDraggingNodeId: ViewState -> string option
    abstract ShouldEnableOrbitControls: ViewState -> bool
    abstract Update: ViewState -> Msg -> ViewState

type ViewModel() =
    interface IViewModel with
        member _.CreateInitialViewState nodes branches =
            { ViewState.Empty with
                nodes = nodes
                branches = List.ofSeq branches }

        member _.EnumerateBranches state = state.branches

        member _.EnumerateChildren state branch =
            branch.children
            |> List.choose (fun childId -> Map.tryFind childId state.nodes)
            |> List.toSeq

        member _.EnumerateTreeNodes state =
            state.nodes |> Map.values :> seq<TreeNode>

        member _.GetDraggingNodeId state =
            match state.drag with
            | Tentative drag
            | Dragging drag -> Some drag.nodeId
            | _ -> None

        member _.ShouldEnableOrbitControls state = state.drag.ShouldEnableOrbitControls
        member _.Update state msg = ViewState.Update state msg

module Initial =
    let private nodes =
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

    let private branches =
        [ { id = "branch"
            parents = "root1", "root2"
            children = [ "child1"; "child2"; "child3" ] } ]

    let state: ViewState =
        { ViewState.Empty with
            nodes = nodes
            branches = branches }
