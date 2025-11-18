import { createActions, World } from "koota";
import { animate } from "./animation";
import * as Controls from "../generated/ECS/Controls";
import { dragNodes } from "./dragging";
import { cleanupEvents } from "./events";
import { paintTreeNodes, copyPositionsToMeshes } from "./rendering";
import { selectNodes } from "./selection";
import { PersonRef, Position, TargetPosition } from "./traits";
import { Person } from "../generated/Model";
import { handleUndoRedo } from "./undo-redo";
import { createWorld, unwrapTrait } from "./kootaWrapper";

export const world = createWorld();

Controls.defineControls(world);

// Redefine unwrapped controls for consumption in the React controls.
const Button = unwrapTrait(Controls.Button);

export function runSystems(input: { world: World; delta: number }) {
  animate(input);
  dragNodes(input);
  handleUndoRedo(input);
  selectNodes(input);
  copyPositionsToMeshes(input);
  paintTreeNodes(input);
  cleanupEvents(input);
}

export const worldActions = createActions((world: World) => ({
  despawnAllTreeNodes: () => {
    world
      .query(PersonRef)
      .select()
      .updateEach(([], entity) => {
        entity.destroy();
      });
  },
  spawnTreeNode: (person: Person, position: [number, number, number]) => {
    const [nx, ny, nz] = position;
    world.spawn(
      PersonRef(person),
      Position({
        x: 0,
        y: 0,
        z: 0,
      }),
      TargetPosition({
        x: nx,
        y: ny,
        z: nz,
      })
    );
  },
}));

export { Button };
export { eventActions } from "./events";
export { Dragging, MeshRef, PersonRef, Selected } from "./traits";
