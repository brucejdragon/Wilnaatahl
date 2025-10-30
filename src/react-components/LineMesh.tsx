import React, { useLayoutEffect } from "react";
import { Mesh } from "three";
import { Entity } from "koota";
import { MeshRef } from "../ecs";

export function LineMesh({ entity }: { entity: Entity }) {
  const ref = React.useRef<Mesh>(null);

  useLayoutEffect(() => {
    if (!ref.current) {
      return;
    }

    entity.add(MeshRef(ref.current));
    return () => {
      entity.remove(MeshRef);
    };
  }, [entity]);

  // Geometry will be set dynamically by the ECS-based rendering system.
  return (
    <mesh ref={ref}>
      <meshStandardMaterial color="#AAAAAA" />
    </mesh>
  );
}
