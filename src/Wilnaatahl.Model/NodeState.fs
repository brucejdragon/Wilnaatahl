namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model
#if FABLE_COMPILER
open Fable.Core
#endif

#if FABLE_COMPILER
[<Erase>]
#endif
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
              selectedNodes: Map<int, TreeNode> }

    let private nodeIdToInt (NodeId nodeId) = nodeId

    let createNodeState nodes =
        { nodes =
            nodes
            |> Seq.map (fun node -> nodeIdToInt node.id, node)
            |> Map.ofSeq
          selectedNodes = Map.empty }

    let deselect (NodeId nodeId) state =
        match state.selectedNodes |> Map.tryFind nodeId with
        | Some node ->
            { state with
                selectedNodes = state.selectedNodes |> Map.remove nodeId
                nodes = state.nodes |> Map.add nodeId node }
        | None -> state

    let deselectAll state =
        let add ns node =
            ns |> Map.add (nodeIdToInt node.id) node

        let newNodes =
            state.selectedNodes.Values
            |> Seq.fold add state.nodes

        { state with
            selectedNodes = Map.empty
            nodes = newNodes }

    let findNode (NodeId nodeId) state =
        // Look in selectedNodes first, then nodes. This is on the theory that
        // lookups are more frequent when updating the positions of selected nodes
        // during a drag operation.
        match state.selectedNodes |> Map.tryFind nodeId with
        | Some node -> node
        | None -> Map.find nodeId state.nodes

    let isSelected (NodeId nodeId) state =
        state.selectedNodes |> Map.containsKey nodeId

    let mapSelected f state =
        let mappedNodes = state.selectedNodes |> Map.map (fun _ n -> f n)
        { state with selectedNodes = mappedNodes }

    let select (NodeId nodeId) state =
        match state.nodes |> Map.tryFind nodeId with
        | Some node ->
            { state with
                selectedNodes = state.selectedNodes |> Map.add nodeId node
                nodes = state.nodes |> Map.remove nodeId }
        | None -> state

    let selected state =
        state.selectedNodes |> Map.values :> seq<TreeNode>

    let unselected state =
        state.nodes |> Map.values :> seq<TreeNode>
