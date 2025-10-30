module Wilnaatahl.Systems.WorldActions

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity
open Wilnaatahl.ECS.Relation
open Wilnaatahl.ECS.TraitExtensions
open Wilnaatahl.Model
open Wilnaatahl.Systems.Traits

type TreeNodeData =
    { Person: Person
      Position: float * float * float
      RenderedInWilp: EntityId }

let destroyAllTreeNodes (world: IWorld) =
    for entity in world.Query(With PersonRef) do
        entity |> destroy

let spawnWilpBox (world: IWorld) (wilp: WilpName) =
    // Since a Wilp is also a BoundingBox, it will be cleaned up along with all other Connectors.
    let boundingBoxId, _, _ = world |> BoundingBox.spawn Line3.zeroPosition // TODO: Tweak Size.x to make huwilp forest look good
    boundingBoxId |> addWith Wilp {| wilpName = wilp.AsString |} // TODO: Also add MeshRef/GroupRef/SceneRef to link this to a Three.js <group/>
    // TODO: Add Follows => Layout entity to track increasing z co-ordinate during layout.
    boundingBoxId

let spawnTreeNode (world: IWorld) treeNodeData =
    let nx, ny, nz = treeNodeData.Position
    let person = treeNodeData.Person

    let nodeSize =
        match person.Shape with
        | Sphere -> {| x = 0.4; y = 0.4; z = 0.4 |}
        | Cube -> {| x = 0.6; y = 0.6; z = 0.6 |}

    world.Spawn(
        PersonRef.Val person,
        Position.Val {| x = 0.0; y = 0.0; z = 0.0 |},
        TargetPosition.Val {| x = nx; y = ny; z = nz |},
        (RenderedIn => treeNodeData.RenderedInWilp).Tag(),
        Size.Val nodeSize
    )
