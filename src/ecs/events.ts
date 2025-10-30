import { Matrix4, Quaternion, Vector3 } from "three";
import { ThreeEvent } from "@react-three/fiber";
import { createActions, Entity, trait, World } from "koota";
import { removeAll } from "./utils";

// The following traits are used to flag input events, some global, some on entities.
// They are deleted at the end of every frame to avoid being processed multiple times.
export const ClickEvent = trait();
export const DragEndEvent = trait();
export const DragEvent = trait({ x: 0, y: 0, z: 0 });
export const DragStartEvent = trait();
export const PointerDownEvent = trait();
export const PointerMissedEvent = trait();

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

export function cleanupEvents({ world }: { world: World }) {
  // Remove event traits from all entities at the end of the frame.
  removeAll(world, PointerDownEvent);
  removeAll(world, ClickEvent);

  // Global events are world traits, so we have to delete them one by one.
  world.remove(PointerMissedEvent);
  world.remove(DragStartEvent);
  world.remove(DragEvent);
  world.remove(DragEndEvent);
}
