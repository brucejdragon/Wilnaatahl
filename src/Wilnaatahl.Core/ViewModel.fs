namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model
open Wilnaatahl.Model.FamilyGraph
open Wilnaatahl.Model.Initial

/// Represents a node in the tree.
type TreeNode = { TargetPosition: float * float * float; Person: Person }

// Functionality to initialize the core ViewModel data structures in an interface for easier consumption from TypeScript.
type IGraphViewFactory =
    abstract FirstWilp: FamilyGraph -> WilpName
    abstract LayoutGraph: FamilyGraph -> seq<TreeNode>
    abstract LoadGraph: unit -> FamilyGraph

type GraphViewFactory() =
    interface IGraphViewFactory with
        member _.FirstWilp familyGraph = familyGraph |> huwilp |> Seq.head // ASSUMPTION: At least one Wilp is represented in the input data.

        member _.LayoutGraph familyGraph =
            let place personId (x, y) =
                { TargetPosition = x, y, 0.0
                  Person = familyGraph |> findPerson (PersonId personId) }

            [ place 0 (-0.9, 0.0)
              place 1 (1.0, 0.0)
              place 2 (-1.9, -2.0)
              place 3 (0.05, -2.0)
              place 4 (2.0, -2.0) ]
            |> Seq.ofList

        member _.LoadGraph() = createFamilyGraph peopleAndParents
