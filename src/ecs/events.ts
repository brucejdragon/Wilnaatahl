import { Matrix4, Quaternion, Vector3 } from "three";
import { ThreeEvent } from "@react-three/fiber";
import { createActions, Entity, World } from "koota";
import { EntityId } from "../generated/ECS/Types";
import * as Events from "../generated/Systems/Events";
import { fromKootaWorld, toKootaTagTrait } from "./kootaWrapper";

// Redefine unwrapped traits for consumption on the TypeScript side.
const ClickEvent = toKootaTagTrait(Events.ClickEvent);

export const eventActions = createActions((world: World) => {
  const wrappedWorld = fromKootaWorld(world);
  return {
    handleClick: (entity: Entity & EntityId) => () => Events.handleClick(entity),
    handleDrag: (localMatrix: Matrix4) => {
      const local = new Vector3();
      localMatrix.decompose(local, new Quaternion(), new Vector3());
      Events.handleDrag(wrappedWorld, local.x, local.y, local.z);
    },
    handleDragEnd: () => Events.handleDragEnd(wrappedWorld),
    handleDragStart: () => {
      // We ignore the origin from DragControls since it always seems to be (0, 0, 0).
      Events.handleDragStart(wrappedWorld);
    },
    handleMeshClick: (entity: Entity) => (e: ThreeEvent<MouseEvent>) => {
      entity.add(ClickEvent);
      e.stopPropagation();
    },
    handlePointerDown: (entity: Entity & EntityId) => () => Events.handlePointerDown(entity),
    handlePointerMissed: () => Events.handlePointerMissed(wrappedWorld),
  };
});
