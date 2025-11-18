import { createActions, createWorld, World } from "koota";
import { Button as WrappedButton, spawnControls } from "../generated/Systems/Controls";
import { destroyAllConnectors, spawnAllConnectors } from "../generated/Systems/Connectors";
import { runSystems as runFableSystems } from "../generated/Systems/Runner";
import {
  destroyAllTreeNodes,
  spawnTreeNode,
  TreeNodeData,
} from "../generated/Systems/WorldActions";
import { FamilyGraph_FamilyGraph as FamilyGraph, Person } from "../generated/Model";
import { fromKootaWorld, toKootaValueTrait } from "./kootaWrapper";

export const world = createWorld();

spawnControls(fromKootaWorld(world));

export function runSystems(input: { world: World; delta: number }) {
  runFableSystems(fromKootaWorld(input.world), input.delta);
}

export const worldActions = createActions((world: World) => {
  const wrappedWorld = fromKootaWorld(world);
  return {
    destroyAllConnectors: () => destroyAllConnectors(wrappedWorld),
    destroyAllTreeNodes: () => destroyAllTreeNodes(wrappedWorld),
    spawnAllConnectors: (familyGraph: FamilyGraph) => spawnAllConnectors(wrappedWorld, familyGraph),
    spawnTreeNode: (person: Person, position: [number, number, number], renderedInWilp: string) =>
      spawnTreeNode(wrappedWorld, new TreeNodeData(person, position, renderedInWilp)),
  };
});

// Redefine unwrapped traits for consumption on the TypeScript side.
const Button = toKootaValueTrait(WrappedButton);

export { Button };
export { eventActions } from "./events";
export { Dragging, MeshRef, PersonRef, Selected } from "./traits";
