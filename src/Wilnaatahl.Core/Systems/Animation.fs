module Wilnaatahl.Systems.Animation

open System
open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity
open Wilnaatahl.Systems.Traits
open Wilnaatahl.Systems.Utils

let animate delta (world: IWorld) =
    world.QueryTraits(Position, TargetPosition).UpdateEach
    <| fun ((pos, targetPos), entity) ->
        let lambda = 6
        let tx, ty, tz = targetPos.x, targetPos.y, targetPos.z
        let x, y, z = pos.x, pos.y, pos.z

        let nx, ny, nz =
            damp x tx lambda delta, damp y ty lambda delta, damp z tz lambda delta

        // We need some tolerance due to funny business with IEEE 754 equality.
        let closeEnough = 0.01

        if
            Math.Abs(nx - tx) < closeEnough
            && Math.Abs(ny - ty) < closeEnough
            && Math.Abs(nz - tz) < closeEnough
        then
            // Animation is finished; Set exactly to target and remove TargetPosition.
            pos.x <- tx
            pos.y <- ty
            pos.z <- tz
            entity |> remove TargetPosition
        else
            pos.x <- nx
            pos.y <- ny
            pos.z <- nz

    world
