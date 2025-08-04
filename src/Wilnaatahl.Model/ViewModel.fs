namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model
open Wilnaatahl.Model.Initial
open Fable.Core.JsInterop

type Node = {
    id: string
    position: float * float * float
    children: string list
    person: Person option
}

type DragState = {
    nodeId: string
    offset: float * float * float // Offset from node position to pointer at drag start
}

type Msg =
    | SelectNode of string
    | StartDrag of nodeId: string * pointer: float * float * float
    | DragTo of pointer: float * float * float
    | EndDrag

type State = {
    nodes: Map<string, Node>
    selectedNodeId: string option
    drag: DragState option
}
with
    static member Empty =
        { nodes = Map.empty; selectedNodeId = None; drag = None }

    static member Update state msg =
        match msg with
        | SelectNode nodeId ->
            { state with selectedNodeId = Some nodeId; drag = None }
        | StartDrag (nodeId, px, py, pz) ->
            match Map.tryFind nodeId state.nodes with
            | Some node ->
                let (nx, ny, nz) = node.position
                let offset = (nx - px, ny - py, 0.0) // Only track offset in x and y; ignore z
                { state with drag = Some { nodeId = nodeId; offset = offset } }
            | None -> state
        | DragTo (px, py, pz) ->
            match state.drag with
            | Some drag ->
                match Map.tryFind drag.nodeId state.nodes with
                | Some node ->
                    let (ox, oy, _) = drag.offset
                    let (nx, ny, nz) = node.position
                    let newPos = (px + ox, py + oy, nz) // Keep z fixed at original value
                    let updatedNode = { node with position = newPos }
                    let updatedNodes = state.nodes |> Map.add node.id updatedNode
                    { state with nodes = updatedNodes }
                | None -> state
            | None -> state
        | EndDrag ->
            { state with drag = None }

module Initial =
    let private nodes =
        [
            ("root1", { id = "root1"; position = (-1.0, 0.0, 0.0); children = []; person = Some people[0] })
            ("root2", { id = "root2"; position = (1.0, 0.0, 0.0); children = []; person = Some people[1] })
            ("branch", { id = "branch"; position = (0.0, -1.0, 0.0); children = ["child1"; "child2"; "child3"]; person = None })
            ("child1", { id = "child1"; position = (-2.0, -2.0, 0.0); children = []; person = Some people[2] })
            ("child2", { id = "child2"; position = (0.0, -2.0, 0.0); children = []; person = Some people[3] })
            ("child3", { id = "child3"; position = (2.0, -2.0, 0.0); children = []; person = Some people[4] })
        ] |> Map.ofList

    let state : State =
        { State.Empty with nodes = nodes }