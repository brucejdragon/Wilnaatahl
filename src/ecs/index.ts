import { createActions, createWorld, World } from "koota";
import { Button as WrappedButton, spawnControls } from "../generated/Systems/Controls";
import { runSystems as runFableSystems } from "../generated/Systems/Runner";
import { layoutNodes } from "../generated/Systems/Layout";
import { destroyScene, spawnScene } from "../generated/Systems/WorldActions";
import { FamilyGraph_FamilyGraph as FamilyGraph } from "../generated/Model";
import { fromKootaWorld, toKootaValueTrait } from "./kootaWrapper";

export const world = createWorld();

spawnControls(fromKootaWorld(world));

export function runSystems(input: { world: World; delta: number }) {
  runFableSystems(fromKootaWorld(input.world), input.delta);
}

export const worldActions = createActions((world: World) => {
  const wrappedWorld = fromKootaWorld(world);
  return {
    destroyScene: () => destroyScene(wrappedWorld),
    layoutNodes: () => layoutNodes(wrappedWorld),
    spawnScene: (familyGraph: FamilyGraph) => spawnScene(wrappedWorld, familyGraph),
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
