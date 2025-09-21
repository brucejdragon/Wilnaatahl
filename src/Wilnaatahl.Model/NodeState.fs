namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model
open Fable.Core

[<Erase>]
type NodeId = NodeId of int

/// Represents a node in the tree.
type TreeNode =
    { id: NodeId
      position: float * float * float
      person: Person }

/// Encapsulates the state of all nodes and selection.
module NodeState =
    type NodeState =
        private
            { nodes: Map<int, TreeNode>
              selectedNodeId: int option }

    let createNodeState nodes =
        let nodeIdToInt (NodeId nodeId) = nodeId

        { nodes =
            nodes
            |> Seq.map (fun node -> nodeIdToInt node.id, node)
            |> Map.ofSeq
          selectedNodeId = None }

    let isSelected (NodeId nodeId) state =
        match state.selectedNodeId with
        | Some id when id = nodeId -> true
        | _ -> false

    let deselect (NodeId nodeId) state =
        match state.selectedNodeId with
        | Some id when id = nodeId -> { state with selectedNodeId = None }
        | _ -> state

    let deselectAll state = { state with selectedNodeId = None }

    let findNode (NodeId nodeId) state = Map.find nodeId state.nodes

    let select (NodeId nodeId) state =
        { state with selectedNodeId = Some nodeId }

    let selected state =
        state.selectedNodeId
        |> Option.toList
        |> List.map (fun id -> findNode (NodeId id) state)
        |> Seq.ofList

    let setNode (NodeId nodeId) node state =
        assert (state.nodes |> Map.containsKey nodeId)
        { state with nodes = state.nodes |> Map.add nodeId node }

    let unselected state =
        state.nodes
        |> Map.values
        |> Seq.filter (fun node -> not (state |> isSelected node.id))
