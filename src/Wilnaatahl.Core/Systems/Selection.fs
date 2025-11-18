module Wilnaatahl.Systems.Selection

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity
open Wilnaatahl.ECS.Extensions
open Wilnaatahl.Systems.Controls
open Wilnaatahl.Systems.Events
open Wilnaatahl.Systems.Traits

let private getSelectModeButton (world: IWorld) =
    // Get the multi-select mode from the state of the multi-select button.
    match world.QueryFirstTrait(SelectModeButton, With Button) with
    | Some(buttonEntity, buttonData) -> buttonEntity, buttonData.multiSelect
    // Button must exist or we have an app setup issue.
    | None -> failwith "App setup error: Select Mode Button not found."

let private handleSelectModeButtonClick buttonEntity multiSelect (world: IWorld) =
    if not (buttonEntity |> has ClickEvent) then
        false
    else
        // The label is set based on the *new* state after toggling, so the label will reflect
        // what state you move into after clicking.
        let label = if multiSelect then "Multi-select" else "Single-select"

        // Toggle the multi-select state and deselect all nodes.
        // We clear the selection when toggling selection mode so you don't end up
        // confusing the user by having multiple nodes selected when in single-selection mode.
        buttonEntity |> set SelectModeButton {| multiSelect = not multiSelect |}
        buttonEntity |> setWith Button (fun data -> {| data with label = label |})
        world.RemoveAll Selected
        true

// Handle background clicks (deselect all).
let private handleBackgroundClick (world: IWorld) =
    if world.Has PointerMissedEvent then
        world.RemoveAll Selected
        true
    else
        false

let private handleNodeClick multiSelect (world: IWorld) =
    // Find the node that received a click event (should be zero or one).
    // We're using PersonRef here as a proxy for tree nodes, since only nodes
    // mapping to people are selectable.
    match world.QueryFirst(With ClickEvent, With PersonRef) with
    | Some nodeEntity when nodeEntity |> has Selected ->
        nodeEntity |> remove Selected
        world
    | Some nodeEntity ->
        // Before selecting the clicked node, we first need to deselect all other nodes
        // if we're in single-select mode.
        if not multiSelect then
            world.RemoveAll Selected

        nodeEntity |> add Selected
        world
    | None -> world // If no node was clicked, then there's nothing to do.

let selectNodes world =
    let buttonEntity, multiSelect = getSelectModeButton world

    if world |> handleSelectModeButtonClick buttonEntity multiSelect then
        // We're done; It shouldn't be possible to click the button, the background, and a node in the same frame.
        world
    else if world |> handleBackgroundClick then
        // We're done; It shouldn't be possible to both click the background and a node in the same frame.
        world
    else
        world |> handleNodeClick multiSelect
