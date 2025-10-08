namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model
#if FABLE_COMPILER
open Fable.Core
#endif

/// Represents a node in the tree.
type TreeNode =
    { Position: float * float * float
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
            |> Seq.map (fun node -> PersonId.ToInt node.Person.Id, node)
            |> Map.ofSeq
          SelectedNodes = Map.empty }

    let deselect (PersonId personId) state =
        match state.SelectedNodes |> Map.tryFind personId with
        | Some node ->
            { state with
                SelectedNodes = state.SelectedNodes |> Map.remove personId
                Nodes = state.Nodes |> Map.add personId node }
        | None -> state

    let deselectAll state =
        let add ns node =
            ns |> Map.add (PersonId.ToInt node.Person.Id) node

        let newNodes =
            state.SelectedNodes.Values
            |> Seq.fold add state.Nodes

        { state with
            SelectedNodes = Map.empty
            Nodes = newNodes }

    let findNode (PersonId personId) state =
        // Look in selectedNodes first, then nodes. This is on the theory that
        // lookups are more frequent when updating the positions of selected nodes
        // during a drag operation.
        match state.SelectedNodes |> Map.tryFind personId with
        | Some node -> node
        | None -> Map.find personId state.Nodes

    let isSelected (PersonId personId) state =
        state.SelectedNodes |> Map.containsKey personId

    let mapSelected f state =
        let mappedNodes = state.SelectedNodes |> Map.map (fun _ n -> f n)
        { state with SelectedNodes = mappedNodes }

    let select (PersonId personId) state =
        match state.Nodes |> Map.tryFind personId with
        | Some node ->
            { state with
                SelectedNodes = state.SelectedNodes |> Map.add personId node
                Nodes = state.Nodes |> Map.remove personId }
        | None -> state

    let selected state =
        state.SelectedNodes |> Map.values :> seq<TreeNode>

    let unselected state =
        state.Nodes |> Map.values :> seq<TreeNode>
