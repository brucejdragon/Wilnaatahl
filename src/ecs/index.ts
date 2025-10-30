import { createActions, createWorld, Entity, World } from "koota";
import { Button as WrappedButton, spawnControls } from "../generated/Systems/Controls";
import { destroyAllConnectors, spawnAllConnectors } from "../generated/Systems/Connectors";
import { runSystems as runFableSystems } from "../generated/Systems/Runner";
import {
  destroyAllTreeNodes,
  spawnTreeNode,
  spawnWilpBox,
  TreeNodeData,
} from "../generated/Systems/WorldActions";
import { FamilyGraph_FamilyGraph as FamilyGraph, Person, WilpName } from "../generated/Model";
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
    spawnTreeNode: (person: Person, position: [number, number, number], renderedInWilp: Entity) =>
      spawnTreeNode(wrappedWorld, new TreeNodeData(person, position, renderedInWilp)),
    spawnWilpBox: (wilpName: WilpName): Entity => spawnWilpBox(wrappedWorld, wilpName) as Entity,
  };
});

// Redefine unwrapped traits for consumption on the TypeScript side.
const Button = toKootaValueTrait(WrappedButton);

export { Button };
export { getLinePositions } from "./connectors";
export { eventActions } from "./events";
export {
  Dragging,
  Elbow,
  EndpointOf,
  Hidden,
  Line,
  Size,
  MeshRef,
  PersonRef,
  Position,
  Selected,
} from "./traits";
