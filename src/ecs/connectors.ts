import { Vector3 } from "three";
import { Entity, World } from "koota";
import { EntityId } from "../generated/ECS/Types";
import { getPositions as wrappedGetLinePositions } from "../generated/Systems/Line3";
import { fromKootaWorld } from "./kootaWrapper";

export function getLinePositions(world: World, line: Entity & EntityId) {
  const wrappedWorld = fromKootaWorld(world);
  const [v1, v2] = wrappedGetLinePositions(wrappedWorld, line);
  return [new Vector3(v1.X, v1.Y, v1.Z), new Vector3(v2.X, v2.Y, v2.Z)];
}
