module Wilnaatahl.Systems.Layout

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity
open Wilnaatahl.Systems.Traits
open Wilnaatahl.Model

let layoutNodes (world: IWorld) =
    let layoutPerson (person: Person, treeNodeId) =
        let place (x, y) =
            treeNodeId |> addWith TargetPosition {| x = x; y = y; z = 0.0 |}

        match person.Id.AsInt with
        | 0 -> place (-0.9, 0.0)
        | 1 -> place (1.0, 0.0)
        | 2 -> place (-1.9, -2.0)
        | 3 -> place (0.05, -2.0)
        | 4 -> place (2.0, -2.0)
        | _ -> failwith "Layout not fully supported yet."

    world.QueryTrait(PersonRef).ForEach layoutPerson
