import { Matrix4, Quaternion, Vector3 } from "three";
import { ThreeEvent } from "@react-three/fiber";
import { createActions, Entity, trait, World } from "koota";
import * as Events from "../generated/Systems/Events";
import { toKootaTrait } from "./kootaWrapper";

// Redefine unwrapped events so we can use them here.
// TODO: Once all relevant systems have been ported to F#, we can drop the "export" on all these.
export const ClickEvent = toKootaTrait(Events.ClickEvent);
export const DragEndEvent = toKootaTrait(Events.DragEndEvent);
export const DragEvent = toKootaTrait(Events.DragEvent);
export const DragStartEvent = toKootaTrait(Events.DragStartEvent);
export const PointerDownEvent = toKootaTrait(Events.PointerDownEvent);
export const PointerMissedEvent = toKootaTrait(Events.PointerMissedEvent);

export const eventActions = createActions((world: World) => ({
  handleClick: (entity: Entity) => () => {
    entity.add(ClickEvent);
  },
  handleDrag: (localMatrix: Matrix4) => {
    const local = new Vector3();
    localMatrix.decompose(local, new Quaternion(), new Vector3());
    world.add(DragEvent);
    world.set(DragEvent, { x: local.x, y: local.y, z: local.z });
  },
  handleDragEnd: () => {
    world.add(DragEndEvent);
  },
  handleDragStart: () => {
    // We ignore the origin from DragControls since it always seems to be (0, 0, 0).
    world.add(DragStartEvent);
  },
  handleMeshClick: (entity: Entity) => (e: ThreeEvent<MouseEvent>) => {
    entity.add(ClickEvent);
    e.stopPropagation();
  },
  handlePointerDown: (entity: Entity) => () => {
    entity.add(PointerDownEvent);
  },
  handlePointerMissed: () => {
    world.add(PointerMissedEvent);
  },
}));
