namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model

/// Represents a node in the tree.
type TreeNode =
    { id: string
      position: float * float * float
      person: Person }

/// Encapsulates the state of all nodes and selection.
module NodeState =
    type NodeState =
        private
            { nodes: Map<string, TreeNode>
              selectedNodeId: string option }

    let createNodeState nodes =
        { nodes = nodes; selectedNodeId = None }

    let isSelected nodeId state =
        match state.selectedNodeId with
        | Some id when id = nodeId -> true
        | _ -> false

    let deselect nodeId state =
        match state.selectedNodeId with
        | Some id when id = nodeId -> { state with selectedNodeId = None }
        | _ -> state

    let deselectAll state = { state with selectedNodeId = None }

    let findNode nodeId state = Map.find nodeId state.nodes

    let select nodeId state =
        { state with selectedNodeId = Some nodeId }

    let selected state =
        state.selectedNodeId
        |> Option.toList
        |> List.map (fun id -> findNode id state)
        |> Seq.ofList

    let setNode nodeId node state =
        assert (state.nodes |> Map.containsKey nodeId)
        { state with nodes = state.nodes |> Map.add nodeId node }

    let unselected state =
        state.nodes
        |> Map.values
        |> Seq.filter (fun node -> not (state |> isSelected node.id))
