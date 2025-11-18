module Wilnaatahl.Systems.WorldActions

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity
open Wilnaatahl.ECS.TraitExtensions
open Wilnaatahl.Model
open Wilnaatahl.Systems.Traits

type TreeNodeData = { Person: Person; Position: float * float * float }

let destroyAllTreeNodes (world: IWorld) =
    for entity in world.Query(With PersonRef) do
        entity |> destroy

let spawnTreeNode (world: IWorld) treeNodeData =
    let nx, ny, nz = treeNodeData.Position

    world.Spawn(
        PersonRef.Val treeNodeData.Person,
        Position.Val {| x = 0.0; y = 0.0; z = 0.0 |},
        TargetPosition.Val {| x = nx; y = ny; z = nz |}
    )
