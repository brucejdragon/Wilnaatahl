import React from "react";
import { Quaternion, Vector3 } from "three";

export function LineMesh({ from, to }: { from: Vector3; to: Vector3 }) {
  const direction = to.clone().sub(from);
  const length = direction.length();
  const mid = from.clone().add(direction.clone().multiplyScalar(0.5));
  const orientation = new Quaternion().setFromUnitVectors(
    new Vector3(0, 1, 0), // cylinder's up axis
    direction.clone().normalize()
  );

  return (
    <mesh position={mid} quaternion={orientation}>
      <cylinderGeometry args={[0.03, 0.03, length, 8]} />
      <meshStandardMaterial color="#AAAAAA" />
    </mesh>
  );
}
