module Wilnaatahl.Systems.Layout

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity
open Wilnaatahl.Systems.Traits
open Wilnaatahl.Model

let private coparentWidth = 1.9
let private leafWidth = 1.95
let private familyHeight = 2.0

let layoutNodes (world: IWorld) =
    let layoutPerson (person: Person, treeNodeId) =
        let place x =
            treeNodeId
            |> addWith TargetPosition {| x = x; y = float person.Generation * -familyHeight; z = 0.0 |}

        match person.Id.AsInt with
        | 0 -> place -0.9
        | 1 -> place 1.0
        | 2 -> place -1.9
        | 3 -> place 0.05
        | 4 -> place 2.0
        | _ -> place 0.0

    world.QueryTrait(PersonRef).ForEach layoutPerson
