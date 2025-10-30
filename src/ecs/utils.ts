import { Entity, Trait, World } from "koota";

export function removeAll(world: World, trait: Trait) {
  // There could be a lot of entities to update, so we pull out the removal for
  // each entity into a standalone function for better perf.
  function remove([], entity: Entity) {
    entity.remove(trait);
  }

  world.query(trait).updateEach(remove);
}

export class Stack<T> {
  private items: T[] = [];

  isEmpty(): boolean {
    return this.items.length === 0;
  }

  push(item: T): void {
    this.items.push(item);
  }

  pop(): T | undefined {
    return this.items.pop();
  }

  clear(): void {
    this.items = [];
  }
}
