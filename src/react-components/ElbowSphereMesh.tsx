import React, { useLayoutEffect } from "react";
import { Mesh } from "three";
import { Entity } from "koota";
import { MeshRef } from "../ecs";

export function ElbowSphereMesh({ entity }: { entity: Entity }) {
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

  return (
    <mesh ref={ref}>
      <sphereGeometry args={[0.03, 16, 16]} />
      <meshStandardMaterial color="#AAAAAA" />
    </mesh>
  );
}
