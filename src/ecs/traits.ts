import { trait } from "koota";
import { Mesh } from "three";
import {
  toKootaRelation,
  toKootaTagTrait,
  toKootaValueFactoryTrait,
  toKootaValueTrait,
} from "./kootaWrapper";
import * as Traits from "../generated/Systems/Traits";

// Used to connect entities that represent visible components to Three.js meshes.
export const MeshRef = trait(() => new Mesh());

// Re-export traits from the F# side.
export const Dragging = toKootaRelation(Traits.Dragging);
export const Elbow = toKootaTagTrait(Traits.Elbow);
export const EndpointOf = toKootaRelation(Traits.EndpointOf);
export const Hidden = toKootaTagTrait(Traits.Hidden);
export const Line = toKootaTagTrait(Traits.Line);
export const Size = toKootaValueTrait(Traits.Size);
export const PersonRef = toKootaValueFactoryTrait(Traits.PersonRef);
export const Position = toKootaValueFactoryTrait(Traits.Position);
export const Selected = toKootaTagTrait(Traits.Selected);
