module Wilnaatahl.ECS.Controls

open ECS

// Used for entities that represent buttons on the toolbar.
let Button =
    traitWith
        {| sortOrder = 0
           label = ""
           disabled = false |}

let UndoButton = tagTrait ()
let RedoButton = tagTrait ()
let SelectModeButton = traitWith {| multiSelect = true |}

let defineControls (world: IWorld) =
    world.Spawn(
        Val(
            Button,
            {| sortOrder = 0
               label = "Undo"
               disabled = true |}
        ),
        Tag UndoButton
    )
    |> ignore

    world.Spawn(
        Val(
            Button,
            {| sortOrder = 1
               label = "Redo"
               disabled = true |}
        ),
        Tag RedoButton
    )
    |> ignore

    world.Spawn(
        Val(
            Button,
            {| sortOrder = 2
               label = "Multi-select"
               disabled = false |}
        ),
        Val(SelectModeButton, {| multiSelect = false |})
    )
    |> ignore
