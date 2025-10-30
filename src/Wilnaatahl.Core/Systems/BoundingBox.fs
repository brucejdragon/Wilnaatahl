module Wilnaatahl.Systems.BoundingBox

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity
open Wilnaatahl.ECS.Relation
open Wilnaatahl.ECS.TraitExtensions
open Wilnaatahl.Systems.Traits

let spawn size (world: IWorld) =
    let boxPosId =
        world.Spawn(Position.Val Line3.zeroPosition, Hidden.Tag(), Connector.Tag())

    let boundPosId =
        world.Spawn(Position.Val Line3.zeroPosition, Hidden.Tag(), Connector.Tag())

    let boxId = world.Spawn(Size.Val size, Hidden.Tag(), Connector.Tag())
    boxPosId |> addWith (CornerOf => boxId) {| IsBounds = false |}
    boundPosId |> addWith (CornerOf => boxId) {| IsBounds = true |}
    boxId, boxPosId, boundPosId

let getCorners (world: IWorld) boxId =
    let corners = world.Query(With(CornerOf => boxId)) |> Array.ofSeq

    if corners.Length = 2 then
        corners[0], corners[1]
    else
        failwith $"Found BoundingBox {boxId} with {corners.Length} corners."

let updateCorners (world: IWorld) changeOption f boxId =
    let mutable i = 0

    world.QueryTraits(Position, CornerOf => boxId).UpdateEachWith changeOption
    <| fun ((pos, which), _) ->
        i <- i + 1

        match i with
        | 1 -> f pos which.IsBounds
        | 2 -> f pos which.IsBounds
        | _ -> failwith $"Found Line {boxId} with {i} endpoints during update."
