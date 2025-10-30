import { Entity, Not, relation, trait, World } from "koota";
import { Button, UndoButton, RedoButton } from "./controls";
import { ClickEvent, DragEndEvent, DragStartEvent } from "./events";
import { Position, Selected, TargetPosition } from "./traits";
import { Stack } from "./utils";

// Used to define an undo/redo stack of entities.
const UndoRedoStack = trait(() => new Stack<Entity>());

// Used to capture the original position of a node at the beginning of a drag operation.
// For efficiency, the target of the relation will be the snapshot itself, since targets
// require extra bookkeeping in Koota to track them.
const SnapshottedBy = relation({ store: { x: 0, y: 0, z: 0 } });

function getSnapshot(world: World, snapshotEntity: Entity | undefined) {
  let hasItems = false;
  if (snapshotEntity) {
    return {
      capture: (entity: Entity, position: { x: number; y: number; z: number }): void => {
        entity.add(SnapshottedBy(snapshotEntity));
        entity.set(SnapshottedBy(snapshotEntity), position);
        hasItems = true;
      },
      destroy: (): void => snapshotEntity.destroy(),
      getEntities: () => world.query(SnapshottedBy(snapshotEntity)),
      getSavedPositionFor: (entity: Entity) => entity.get(SnapshottedBy(snapshotEntity)),
      pushTo: (stack: Stack<Entity>): void => {
        if (hasItems) {
          stack.push(snapshotEntity);
        }
      },
    };
  } else {
    return {
      capture: (): void => {},
      destroy: (): void => {},
      getEntities: () => [],
      getSavedPositionFor: () => undefined,
      pushTo: (): void => {},
    };
  }
}

function handleDragStart(world: World, undoStack: Stack<Entity>) {
  // Before allowing nodes to move as part of a drag operation, we need to capture their
  // starting positions for posterity. We use Selected and the presence of the DragStartEvent
  // to identify the nodes to process.
  if (!world.has(DragStartEvent)) {
    return;
  }

  const snapshot = getSnapshot(world, world.spawn());

  // There are two distinct cases: Either the node about to be dragged was animating,
  // or it was static. We only want to save static positions for Undo.
  world.query(Selected, Position, Not(TargetPosition)).updateEach(([pos], entity) => {
    snapshot.capture(entity, pos);
  });

  snapshot.pushTo(undoStack);
}

function handleDragEnd(world: World, redoStack: Stack<Entity>) {
  if (!world.has(DragEndEvent)) {
    return;
  }

  // Drag is ending; Flush the redo history of all nodes to avoid massive time-travel
  // confusion for the user, but only if at least one of the nodes being dragged does
  // *not* have a TargetPosition. Otherwise, that means the user is dragging nodes that
  // are already animating, which is not an "undoable/redoable" operation. We use Selected
  // here as a proxy for being dragged.
  if (world.query(Selected, Not(TargetPosition)).length > 0) {
    redoStack.clear();
  }
}

function updateButtonState(buttonEntity: Entity, stack: Stack<Entity>) {
  // Button must have the right traits or we have an app setup issue.
  const buttonTrait = buttonEntity.get(Button)!;

  // Update button status based on whether there is anything to undo/redo.
  buttonTrait.disabled = stack.isEmpty();
  buttonEntity.set(Button, buttonTrait);
}

function handleButtonClicked(world: World, fromStack: Stack<Entity>, toStack: Stack<Entity>) {
  const snapshot = getSnapshot(world, fromStack.pop());
  const newSnapshot = getSnapshot(world, world.spawn());

  // How Undo/Redo behaves depends on whether the node being manipulated is static or animating.
  // The invariants we want to maintain are:
  // 1. Positions saved on either stack represent static positions, not intermediate positions on
  //    an animated path.
  // 2. When restoring an old position, the node should animate to that old position, so we're
  //    using a static position from one of the stacks to set a new TargetPosition.
  // This should provide the most intuitive UX.
  for (const entity of snapshot.getEntities()) {
    const posToSave = entity.get(TargetPosition) ?? entity.get(Position);
    const newPos = snapshot.getSavedPositionFor(entity);
    if (!posToSave || !newPos) {
      continue;
    }

    newSnapshot.capture(entity, posToSave);

    entity.add(TargetPosition);
    entity.set(TargetPosition, newPos);
  }

  newSnapshot.pushTo(toStack);
  snapshot.destroy();
}

function ensureStack(entity: Entity): Stack<Entity> {
  if (!entity.has(UndoRedoStack)) {
    entity.add(UndoRedoStack(new Stack<Entity>()));
  }

  return entity.get(UndoRedoStack)!; // Guaranteed to exist per above.
}

export function handleUndoRedo({ world }: { world: World }) {
  // Start by getting the entities representing the Undo/Redo buttons, which also
  // point to the Undo/Redo stacks. Then update the button state and check for clicks.

  // Buttons must exist and have the right traits or we have an app setup issue.
  const undoButtonEntity = world.queryFirst(Button, UndoButton)!;
  const redoButtonEntity = world.queryFirst(Button, RedoButton)!;

  const undoStack = ensureStack(undoButtonEntity);
  const redoStack = ensureStack(redoButtonEntity);

  if (undoButtonEntity.has(ClickEvent)) {
    handleButtonClicked(world, undoStack, redoStack);
    updateButtonState(undoButtonEntity, undoStack);
    return; // Clicking Undo is mutually exclusive with clicking Redo or dragging.
  }

  if (redoButtonEntity.has(ClickEvent)) {
    handleButtonClicked(world, redoStack, undoStack);
    updateButtonState(redoButtonEntity, redoStack);
    return; // Clicking Redo is mutually exclusive with clicking Undo or dragging.
  }

  handleDragStart(world, undoStack);
  updateButtonState(undoButtonEntity, undoStack);

  handleDragEnd(world, redoStack);
  updateButtonState(redoButtonEntity, redoStack);
}
