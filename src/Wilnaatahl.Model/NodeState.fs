namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model
#if FABLE_COMPILER
open Fable.Core
#endif

/// Represents a unique identifier for a renderable node.
/// There can be more than one renderable node per Person, so this is distinct from PersonId.
#if FABLE_COMPILER
[<Erase>]
#endif
type NodeId =
    | NodeId of int
    member this.AsInt =
        let (NodeId nodeId) = this
        nodeId

/// Represents a node in the tree.
type TreeNode =
    { Id: NodeId
      RenderedInWilp: Wilp
      Position: float * float * float
      Person: Person }

/// Encapsulates the state of all nodes and selection.
module NodeState =
    type NodeState =
        private
            { Nodes: Map<int, TreeNode>
              SelectedNodes: Map<int, TreeNode> }

    let createNodeState nodes =
        { Nodes =
            nodes
            |> Seq.map (fun node -> node.Id.AsInt, node)
            |> Map.ofSeq
          SelectedNodes = Map.empty }

    let deselect (NodeId nodeId) state =
        match state.SelectedNodes |> Map.tryFind nodeId with
        | Some node ->
            { state with
                SelectedNodes = state.SelectedNodes |> Map.remove nodeId
                Nodes = state.Nodes |> Map.add nodeId node }
        | None -> state

    let deselectAll state =
        let add ns node = ns |> Map.add node.Id.AsInt node

        let newNodes =
            state.SelectedNodes.Values
            |> Seq.fold add state.Nodes

        { state with
            SelectedNodes = Map.empty
            Nodes = newNodes }

    let findNode (NodeId nodeId) state =
        // Look in selectedNodes first, then nodes. This is on the theory that
        // lookups are more frequent when updating the positions of selected nodes
        // during a drag operation.
        match state.SelectedNodes |> Map.tryFind nodeId with
        | Some node -> node
        | None -> Map.find nodeId state.Nodes

    let isSelected (NodeId nodeId) state =
        state.SelectedNodes |> Map.containsKey nodeId

    let mapSelected f state =
        let mappedNodes = state.SelectedNodes |> Map.map (fun _ n -> f n)
        { state with SelectedNodes = mappedNodes }

    let select (NodeId nodeId) state =
        match state.Nodes |> Map.tryFind nodeId with
        | Some node ->
            { state with
                SelectedNodes = state.SelectedNodes |> Map.add nodeId node
                Nodes = state.Nodes |> Map.remove nodeId }
        | None -> state

    let selected state =
        state.SelectedNodes |> Map.values :> seq<TreeNode>

    let unselected state =
        state.Nodes |> Map.values :> seq<TreeNode>
