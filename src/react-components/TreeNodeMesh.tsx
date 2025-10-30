import React, { useLayoutEffect } from "react";
import { Mesh, Vector3 } from "three";
import { useFrame, useThree } from "@react-three/fiber";
import { Html } from "@react-three/drei";
import { Entity } from "koota";
import { useActions, useTrait } from "koota/react";
import { defaultArg } from "../generated/fable_modules/fable-library-ts.4.27.0/Option.js";
import { eventActions, Size, MeshRef, PersonRef } from "../ecs";

export function TreeNodeMesh({ entity }: { entity: Entity }) {
  // WilpGroup guarantees that the traits are present.
  const person = useTrait(entity, PersonRef)!;
  const size = useTrait(entity, Size)!;
  const label = defaultArg(person.Label, undefined);
  const ref = React.useRef<Mesh>(null);
  const { camera } = useThree();

  // Compute distance from camera to mesh
  const [fontSize, setFontSize] = React.useState(16);

  // Use useFrame for smoother updates
  useFrame(() => {
    if (!ref.current) {
      return;
    }

    // Get mesh world position.
    const meshPos = ref.current.getWorldPosition(new Vector3());
    const camPos = camera.position;
    const distance = meshPos.distanceTo(camPos);

    // Adjust this formula as needed for the scene scale.
    // Clamp to reasonable min/max.
    const size = Math.max(10, Math.min(32, 120 / distance));
    setFontSize(size);
  });

  useLayoutEffect(() => {
    if (!ref.current) {
      return;
    }

    entity.add(MeshRef(ref.current));
    return () => {
      entity.remove(MeshRef);
    };
  }, [entity]);

  const { handlePointerDown, handleMeshClick } = useActions(eventActions);

  return (
    <>
      <mesh
        onClick={handleMeshClick(entity)}
        onPointerDown={handlePointerDown(entity)}
        castShadow
        receiveShadow
        ref={ref}
      >
        {person.Shape === "cube" ? (
          <boxGeometry args={[size.x, size.y, size.z]} />
        ) : (
          <sphereGeometry args={[Math.min(size.x, size.y, size.z), 16, 16]} />
        )}
        <meshStandardMaterial
          color={"#FF0000"} // Default to red; Paint system will update it as needed.
          metalness={0.3} // Slight metallic effect
          roughness={0.3} // Moderate roughness for better light scattering
        />
        {label && (
          <Html position={[0, -0.5, 0]} center>
            <div
              style={{
                color: "white",
                fontSize: `${fontSize}px`,
                textAlign: "center",
                pointerEvents: "none",
                width: "160%",
                marginLeft: "-30%",
              }}
            >
              {label}
            </div>
          </Html>
        )}
      </mesh>
    </>
  );
}
