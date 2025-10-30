import { Entity, trait, World } from "koota";
import { ClickEvent, DragStartEvent, DragEvent, DragEndEvent, PointerDownEvent } from "./events";
import { Dragging, PersonRef, Position, Selected } from "./traits";
import { removeAll } from "./utils";

// Used to mark tree nodes that are touched.
const Touched = trait();

// The dragging system relies on knowing the last "touched" node in
// order to map co-ordinates from DragControl to a tree node. We could
// use raycasting, but this seems simpler.
function trackTouchedNodes(world: World) {
  // Due to multi-touch, there could technically be more than one PointerDownEvent
  // in a frame, but in practice it requires an improbable level of dexterity to
  // pull off. We should be fine just getting the first arbitrary PointerDownEvent.
  const event = world.queryFirst(PointerDownEvent, PersonRef);
  if (event) {
    // Clear the last touched node and replace with this one.
    removeAll(world, Touched);
    event.add(Touched);
  }
}

function handleDragStart(world: World) {
  if (!world.has(DragStartEvent)) {
    return;
  }

  // A node ought to have been touched before starting a drag; if not, we can't proceed.
  const nodeEntity = world.queryFirst(Touched, Position);
  if (nodeEntity) {
    const origin = nodeEntity.get(Position)!; // Trait must exist per the query above.
    const dragEntity = world.spawn();
    dragEntity.add(Dragging(nodeEntity));
    dragEntity.set(Dragging(nodeEntity), origin);
  }
}

function handleDrag(world: World) {
  const move = world.get(DragEvent);
  if (!move) {
    return;
  }

  const dragEntity = world.queryFirst(Dragging("*"));
  const nodeEntity = dragEntity?.targetFor(Dragging);
  const oldPosition = nodeEntity?.get(Position);
  const origin = nodeEntity ? dragEntity?.get(Dragging(nodeEntity)) : undefined;

  if (origin && oldPosition) {
    const newPosition = {
      x: origin.x + move.x,
      y: origin.y + move.y,
      z: origin.z + move.z,
    };
    const delta = {
      x: newPosition.x - oldPosition.x,
      y: newPosition.y - oldPosition.y,
      z: newPosition.z - oldPosition.z,
    };

    world.query(Position, Selected).updateEach(([pos]) => {
      pos.x += delta.x;
      pos.y += delta.y;
      pos.z += delta.z;
    });
  }

  return dragEntity;
}

function handleDragEnd(world: World, dragEntity: Entity | undefined) {
  if (world.has(DragEndEvent)) {
    if (dragEntity) {
      dragEntity.destroy();

      // There should be a spurious click event in the same frame.
      // Delete it so it doesn't trigger selection.
      // ASSUMPTION: The dragging system must run before the selection system!
      removeAll(world, ClickEvent);
    } else {
      // If there is no drag operation present, that means this is a spurious
      // DragEndEvent. We need to prevent it from propagating or it could interfere
      // with Undo/Redo.
      // ASSUMPTION: The dragging system must run before the undo/redo system!
      world.remove(DragEndEvent);
    }
  }
}

export function dragNodes({ world }: { world: World }) {
  // There are no early returns because it's possible to have some
  // combination of these events happen in the same frame.
  trackTouchedNodes(world);
  handleDragStart(world);
  const dragEntity = handleDrag(world);
  handleDragEnd(world, dragEntity);
}
