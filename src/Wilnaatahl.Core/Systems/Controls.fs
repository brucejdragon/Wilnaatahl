module Wilnaatahl.Systems.Controls

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Trait
open Wilnaatahl.ECS.TraitExtensions

// Used for entities that represent buttons on the toolbar.
let Button = valueTrait {| sortOrder = 0; label = ""; disabled = false |}

let UndoButton = tagTrait ()
let RedoButton = tagTrait ()
let SelectModeButton = valueTrait {| multiSelect = true |}

let spawnControls (world: IWorld) =
    // The controls are spawned on app startup and never destroyed, which should be fine.
    world.Spawn(Button.Val {| sortOrder = 0; label = "Undo"; disabled = true |}, UndoButton.Tag())
    |> ignore

    world.Spawn(Button.Val {| sortOrder = 1; label = "Redo"; disabled = true |}, RedoButton.Tag())
    |> ignore

    world.Spawn(
        Button.Val {| sortOrder = 2; label = "Multi-select"; disabled = false |},
        SelectModeButton.Val {| multiSelect = false |}
    )
    |> ignore
