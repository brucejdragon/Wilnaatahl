import { trait } from "koota";
import { Mesh } from "three";
import { toKootaRelation, toKootaTagTrait, toKootaValueFactoryTrait } from "./kootaWrapper";
import * as Traits from "../generated/Systems/Traits";

// Used to connect entities that represent visible components to Three.js meshes.
export const MeshRef = trait(() => new Mesh());

// Re-export traits from the F# side.
export const PersonRef = toKootaValueFactoryTrait(Traits.PersonRef);
export const Selected = toKootaTagTrait(Traits.Selected);
export const Dragging = toKootaRelation(Traits.Dragging);
