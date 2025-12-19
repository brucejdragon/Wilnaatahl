module Wilnaatahl.Systems.Runner

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Tracking
open Wilnaatahl.Systems.Animation
open Wilnaatahl.Systems.Dragging
open Wilnaatahl.Systems.Events
open Wilnaatahl.Systems.Movement
open Wilnaatahl.Systems.Selection
open Wilnaatahl.Systems.UndoRedo

/// Exposes systems that are implemented in TypeScript so we can include them in runSystems.
[<AutoOpen>]
module private TypeScriptSystems =
#if FABLE_COMPILER
    open Fable.Core

    /// Calls the rendering system; Must be called on each frame.
    [<Import("render", "../../ecs/rendering.ts")>]
    let render: IWorld -> IWorld = nativeOnly

#else

    /// Unit test stub for rendering that does nothing.
    let render (world: IWorld) = world

#endif

let private allSystemsPositionChangeTracker = createChanged ()

let runSystems (world: IWorld) delta =
    world
    |> animate delta
    |> dragNodes
    |> handleUndoRedo
    |> selectNodes
    |> move allSystemsPositionChangeTracker
    |> render
    |> cleanupEvents
    |> ignore

    ()
