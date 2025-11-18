module Wilnaatahl.Systems.Selection

open Wilnaatahl.ECS
open Wilnaatahl.Systems.Controls

(*import { Entity, World } from "koota";
import * as Controls from "../generated/Systems/Controls";
import { ClickEvent, PointerMissedEvent } from "./eventActions";
import { PersonRef, Selected } from "./traits";
import { removeAll } from "./utils";
import { toKootaTrait } from "./kootaWrapper";

const Button = toKootaTrait(Controls.Button);
const SelectModeButton = toKootaTrait(Controls.SelectModeButton);

function getSelectModeButton(world: World) {
  // Get the multi-select mode from the state of the multi-select button.
  const buttonEntity = world.queryFirst(Button, SelectModeButton)!; // Button must exist or we have an app setup issue.
  const multiSelect = buttonEntity.get(SelectModeButton)!.multiSelect; // Trait must be present per query above.

  return { buttonEntity: buttonEntity, multiSelect: multiSelect };
}

function handleSelectModeButtonClick(world: World, buttonEntity: Entity, multiSelect: boolean) {
  if (!buttonEntity.has(ClickEvent)) {
    return false;
  }

  // The label is set based on the *new* state after toggling, so the label will reflect
  // what state you move into after clicking.
  const label = multiSelect ? "Multi-select" : "Single-select";

  // Toggle the multi-select state and deselect all nodes.
  // We clear the selection when toggling selection mode so you don't end up
  // confusing the user by having multiple nodes selected when in single-selection mode.
  buttonEntity.set(SelectModeButton, { multiSelect: !multiSelect });
  buttonEntity.set(Button, { label: label });
  removeAll(world, Selected);
  return true;
}

// Handle background clicks (deselect all).
function handleBackgroundClick(world: World) {
  const clicked = world.has(PointerMissedEvent);
  if (clicked) {
    removeAll(world, Selected);
  }

  return clicked;
}

function handleNodeClick(world: World, multiSelect: boolean) {
  // Find the node that received a click event (should be zero or one).
  // We're using PersonRef here as a proxy for tree nodes, since only nodes
  // mapping to people are selectable.
  const nodeEntity = world.queryFirst(ClickEvent, PersonRef);

  // If no node was clicked, then there's nothing to do.
  if (!nodeEntity) {
    return;
  }

  if (nodeEntity.has(Selected)) {
    nodeEntity.remove(Selected);
  } else {
    // Before selecting the clicked node, we first need to deselect all other nodes
    // if we're in single-select mode.
    if (!multiSelect) {
      removeAll(world, Selected);
    }
    nodeEntity.add(Selected);
  }
}

export function selectNodes({ world }: { world: World }) {
  const { buttonEntity, multiSelect } = getSelectModeButton(world);

  if (handleSelectModeButtonClick(world, buttonEntity, multiSelect)) {
    // We're done; It shouldn't be possible to click the button, the background, and a node in the same frame.
    return;
  }

  if (handleBackgroundClick(world)) {
    // We're done; It shouldn't be possible to both click the background and a node in the same frame.
    return;
  }

  handleNodeClick(world, multiSelect);
}
*)
