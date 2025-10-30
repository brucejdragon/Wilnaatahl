import { Not, World } from "koota";
import { Mesh, MeshStandardMaterial } from "three";
import { MeshRef, Position, Selected } from "./traits";

// This will be called for every rendered tree node, so it will be faster as a standalone function.
function setPositionOnMesh([pos, mesh]: [{ x: number; y: number; z: number }, Mesh]) {
  mesh.position.copy(pos);
}

export function copyPositionsToMeshes({ world }: { world: World }) {
  world.query(Position, MeshRef).updateEach(setPositionOnMesh);
}

function setColourProperties(
  mesh: Mesh,
  colorHex: string,
  emissiveHex: string,
  emissiveIntensity: number
) {
  const material = mesh.material as MeshStandardMaterial;
  material.color.set(colorHex);
  material.emissive.set(emissiveHex);
  material.emissiveIntensity = emissiveIntensity;
}

export function paintTreeNodes({ world }: { world: World }) {
  const selectedNodeColour = "#8B4000"; // Deep, red copper
  const defaultNodeColour = "#FF0000"; // Red

  function setDefaultColour([mesh]: [Mesh]) {
    setColourProperties(mesh, selectedNodeColour, selectedNodeColour, 0.8);
  }

  function setSelectedColour([mesh]: [Mesh]) {
    // No emissive color for unselected nodes.
    setColourProperties(mesh, defaultNodeColour, "#000000", 0);
  }

  world.query(MeshRef, Selected).updateEach(setDefaultColour);
  world.query(MeshRef, Not(Selected)).updateEach(setSelectedColour);
}
