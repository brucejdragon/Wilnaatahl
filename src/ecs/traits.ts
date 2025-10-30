import { relation, trait } from "koota";
import { Mesh } from "three";
import { Person_get_Empty } from "../generated/Model";

// Used to connect entities that represent visible components to Three.js meshes.
export const MeshRef = trait(() => new Mesh());

// Used for entities that represent "tree nodes", i.e. people in the family tree.
export const PersonRef = trait(Person_get_Empty);

// Used for any visible entity that has an on-screen position.
export const Position = trait({ x: 0, y: 0, z: 0 });

// Used for any visible entity that has a position and is animating towards a target position.
// An entity that has this trait must also have the Position trait, although it's unclear how
// to enforce this with koota.
export const TargetPosition = trait({ x: 0, y: 0, z: 0 });

// Used to mark tree nodes that are selected.
export const Selected = trait();

// Represents an ongoing drag operation as a relation to the node being dragged.
export const Dragging = relation({ exclusive: true, store: { x: 0, y: 0, z: 0 } });
