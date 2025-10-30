import { Entity, Relation, Schema, Trait, World } from "koota";

export function removeAll(world: World, trait: Trait) {
  // There could be a lot of entities to update, so we pull out the removal for
  // each entity into a standalone function for better perf.
  function remove([], entity: Entity) {
    entity.remove(trait);
  }

  world.query(trait).updateEach(remove);
}

// Represents a proxy to interact with a stack stored as a linked list of entities
// in the world, attached to a given entity by a relation. There can be one stack on the
// entity per stack relation. If no stack yet exists, the proxy will behave as though
// there is an empty stack.
export interface EntityStack {
  // Checks whether the given entity points to a stack.
  isEmpty: () => boolean;

  // Pushes the given entity to be the new top of the stack, linking it to the rest
  // of the stack if it exists. The stack takes ownership of the given entity.
  push: (newEntity: Entity) => void;

  // Pops the entity at the top of the stack and returns it, transferring ownership to the caller.
  // Returns undefined if the stack is empty.
  pop: () => Entity | undefined;

  // Empties the stack by repeatedly calling pop() until it's empty, destroying each entity
  // along the way.
  clear: () => void;
}

export function getStack(entity: Entity, relation: Relation<Trait<Schema>>): EntityStack {
  function isEmpty(): boolean {
    if (entity.targetFor(relation)) {
      return false;
    } else {
      return true;
    }
  }

  function push(newEntity: Entity) {
    const tailEntity = entity.targetFor(relation);
    if (tailEntity) {
      newEntity.add(relation(tailEntity));
    }

    entity.add(relation(newEntity)); // Will remove link to tailEntity automatically.
  }

  function pop() {
    const headEntity = entity.targetFor(relation);
    if (!headEntity) {
      return undefined; // Empty list.
    }

    const tailEntity = headEntity.targetFor(relation);
    if (tailEntity) {
      // We need to point the entity at the new list head before returning.
      entity.add(relation(tailEntity)); // Will remove link to headEntity automatically.
    } else {
      // We're popping the last entry in the stack; manually unlink.
      entity.remove(relation(headEntity));
    }

    return headEntity;
  }

  function clear() {
    let nextEntity = undefined;
    do {
      nextEntity = pop();
      nextEntity?.destroy();
    } while (nextEntity);
  }

  return { isEmpty, push, pop, clear };
}
