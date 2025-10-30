import { Entity, Not, relation, World } from "koota";
import { Button, UndoButton, RedoButton } from "./controls";
import { ClickEvent, DragEndEvent, DragStartEvent } from "./events";
import { Position, Selected, TargetPosition } from "./traits";
import { EntityStack, getStack } from "./utils";

// Used to define an "undo stack" as a linked list of entities.
const HasUndo = relation({ exclusive: true });

// Used to define a "redo stack" as a linked list of entities.
const HasRedo = relation({ exclusive: true });

// Used to capture the original position of a node at the beginning of a drag operation.
const Snapshots = relation({ store: { x: 0, y: 0, z: 0 } });

function getSnapshot(snapshotEntity: Entity | undefined) {
  let hasItems = false;
  return {
    add: (entity: Entity, position: { x: number; y: number; z: number }) => {
      snapshotEntity?.add(Snapshots(entity));
      snapshotEntity?.set(Snapshots(entity), position);
      if (snapshotEntity) {
        hasItems = true;
      }
    },
    destroy: () => snapshotEntity?.destroy(),
    getEntities: () => snapshotEntity?.targetsFor(Snapshots) ?? [],
    getPositionFor: (entity: Entity) => snapshotEntity?.get(Snapshots(entity)),
    pushTo: (stack: EntityStack) => {
      if (hasItems) {
        stack.push(snapshotEntity!); // It can't have items if it doesn't exist.
      }
    },
  };
}

function handleDragStart(world: World, undoStack: EntityStack) {
  // Before allowing nodes to move as part of a drag operation, we need to capture their
  // starting positions for posterity. We use Selected and the presence of the DragStartEvent
  // to identify the nodes to process.
  if (!world.has(DragStartEvent)) {
    return;
  }

  const snapshot = getSnapshot(world.spawn());

  // There are two distinct cases: Either the node about to be dragged was animating,
  // or it was static. We only want to save static positions for Undo.
  world.query(Selected, Position, Not(TargetPosition)).updateEach(([pos], entity) => {
    snapshot.add(entity, pos);
  });

  snapshot.pushTo(undoStack);
}

function handleDragEnd(world: World, redoStack: EntityStack) {
  if (!world.has(DragEndEvent)) {
    return;
  }

  // Drag is ending; Flush the redo history of all nodes to avoid massive time-travel
  // confusion for the user.
  redoStack.clear();
}

function handleButtonState(buttonEntity: Entity, stack: EntityStack) {
  // Button must have the right traits or we have an app setup issue.
  const buttonTrait = buttonEntity.get(Button)!;

  // Update button status based on whether there is anything to undo/redo.
  buttonTrait.disabled = stack.isEmpty();
  buttonEntity.set(Button, buttonTrait);
  return buttonEntity.has(ClickEvent); // If the button was clicked, there is more work to do.
}

function handleButtonClicked(world: World, fromStack: EntityStack, toStack: EntityStack) {
  const snapshot = getSnapshot(fromStack.pop());
  if (!snapshot) {
    return;
  }

  const newSnapshot = getSnapshot(world.spawn());

  // How Undo/Redo behaves depends on whether the node being manipulated is static or animating.
  // The invariants we want to maintain are:
  // 1. Positions saved on either stack represent static positions, not intermediate positions on
  //    an animated path.
  // 2. When restoring an old position, the node should animate to that old position, so we're
  //    using a static position from one of the stacks to set a new TargetPosition.
  // This should provide the most intuitive UX.
  for (const entity of snapshot.getEntities()) {
    const posToSave = entity.get(TargetPosition) ?? entity.get(Position);
    const newPos = snapshot.getPositionFor(entity);
    if (!posToSave || !newPos) {
      continue;
    }

    newSnapshot.add(entity, posToSave);

    entity.add(TargetPosition);
    entity.set(TargetPosition, newPos);
  }

  newSnapshot.pushTo(toStack);
  snapshot.destroy();
}

export function handleUndoRedo({ world }: { world: World }) {
  // Start by getting the entities representing the Undo/Redo buttons, which also
  // point to the Undo/Redo stacks. Then update the button state and check for clicks.

  // Buttons must exist and have the right traits or we have an app setup issue.
  const undoButtonEntity = world.queryFirst(Button, UndoButton)!;
  const redoButtonEntity = world.queryFirst(Button, RedoButton)!;

  const undoStack = getStack(undoButtonEntity, HasUndo);
  const redoStack = getStack(redoButtonEntity, HasRedo);

  if (handleButtonState(undoButtonEntity, undoStack)) {
    handleButtonClicked(world, undoStack, redoStack);
    return; // Clicking Undo is mutually exclusive with clicking Redo or dragging.
  }

  if (handleButtonState(redoButtonEntity, redoStack)) {
    handleButtonClicked(world, redoStack, undoStack);
    return; // Clicking Redo is mutually exclusive with clicking Undo or dragging.
  }

  handleDragStart(world, undoStack);
  handleDragEnd(world, redoStack);
}
