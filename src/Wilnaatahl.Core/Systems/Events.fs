module Wilnaatahl.Systems.Events

open Wilnaatahl.ECS.Trait
open Wilnaatahl.Systems.Utils

// The following traits are used to flag input events, some global, some on entities.
// They are deleted at the end of every frame to avoid being processed multiple times.
let ClickEvent = tagTrait ()
let DragEndEvent = tagTrait ()
let DragEvent = traitWith {| x = 0; y = 0; z = 0 |}
let DragStartEvent = tagTrait ()
let PointerDownEvent = tagTrait ()
let PointerMissedEvent = tagTrait ()

let cleanupEvents world =
    // Remove event traits from all entities at the end of the frame.
    removeAll world PointerDownEvent
    removeAll world ClickEvent

    // Global events are world traits, so we have to delete them one by one.
    // See eventActions.ts to see how events get created.
    world.Remove PointerMissedEvent
    world.Remove DragStartEvent
    world.Remove DragEvent
    world.Remove DragEndEvent
