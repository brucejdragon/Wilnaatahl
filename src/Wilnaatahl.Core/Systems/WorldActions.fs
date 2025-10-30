module Wilnaatahl.Systems.WorldActions

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity
open Wilnaatahl.ECS.Relation
open Wilnaatahl.ECS.TraitExtensions
open Wilnaatahl.Model
open Wilnaatahl.ViewModel
open Wilnaatahl.Systems.Connectors
open Wilnaatahl.Systems.Traits

let private destroyAllTreeNodes (world: IWorld) =
    for entity in world.Query(With PersonRef) do
        entity |> destroy

let private spawnWilpBox (wilp: WilpName) (world: IWorld) =
    // Since a Wilp is also a BoundingBox, it will be cleaned up along with all other Connectors.
    let boundingBoxId, _, _ = world |> BoundingBox.spawn Line3.zeroPosition // TODO: Tweak Size.x to make huwilp forest look good
    boundingBoxId |> addWith Wilp {| wilpName = wilp.AsString |} // TODO: Also add MeshRef/GroupRef/SceneRef to link this to a Three.js <group/>
    // TODO: Add Follows => Layout entity to track increasing z co-ordinate during layout.
    boundingBoxId

let private spawnTreeNode person wilp (world: IWorld) =
    let nodeSize =
        match person.Shape with
        | Sphere -> {| x = 0.4; y = 0.4; z = 0.4 |}
        | Cube -> {| x = 0.6; y = 0.6; z = 0.6 |}

    world.Spawn(
        PersonRef.Val person,
        Position.Val {| x = 0.0; y = 0.0; z = 0.0 |},
        (RenderedIn => wilp).Tag(),
        Size.Val nodeSize
    )
    |> ignore

let destroyScene (world: IWorld) =
    // Destroy the connectors before the tree nodes (it shouldn't matter, but just for symmetry).
    world |> destroyAllConnectors |> destroyAllTreeNodes

let spawnScene (world: IWorld) familyGraph =
    // TODO: Spawn multiple huwilp once we support that.
    let huwilpMap = Scene.enumerateHuwilpToRender familyGraph
    let firstWilp, people = huwilpMap |> Seq.head |> (fun kvp -> (kvp.Key, kvp.Value))

    let wilpId = world |> spawnWilpBox firstWilp

    // Spawn the tree nodes before connectors so the connectors can connect to them.
    for person in people do
        world |> spawnTreeNode person wilpId

    world |> spawnAllConnectors familyGraph
