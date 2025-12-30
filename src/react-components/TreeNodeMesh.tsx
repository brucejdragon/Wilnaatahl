import React from "react";
import { MathUtils, Mesh, Vector3 } from "three";
import { ThreeEvent, useFrame, useThree } from "@react-three/fiber";
import { Html } from "@react-three/drei";
import { defaultArg } from "../generated/fable_modules/fable-library-ts.4.27.0/Option.js";
import { TreeNode } from "../generated/ViewModel/NodeState";
import { Msg_Animate } from "../generated/ViewModel/ViewModel";
import { useViewModel } from "../context/viewModelContext";

export function TreeNodeMesh({
  node,
  isSelected,
  onClick,
  onPointerDown,
}: {
  node: TreeNode;
  isSelected: boolean;
  onClick: (e: ThreeEvent<MouseEvent>) => void;
  onPointerDown: (e: ThreeEvent<PointerEvent>) => void;
}) {
  const { dispatch } = useViewModel();
  const selectedNodeColour = "#8B4000"; // Deep, red copper
  const person = node.Person;
  const [x, y, z] = node.Position;
  const label = defaultArg(person.Label, undefined);
  const ref = React.useRef<Mesh>(null);
  const { camera } = useThree();

  // Compute distance from camera to mesh
  const [fontSize, setFontSize] = React.useState(16);

  useFrame((_, delta) => {
    if (!ref.current) {
      return;
    }

    ref.current.position.set(...node.Position);

    if (node.IsAnimating) {
      const lambda = 6;
      const [tx, ty, tz] = node.TargetPosition;
      const newX = MathUtils.damp(x, tx, lambda, delta);
      const newY = MathUtils.damp(y, ty, lambda, delta);
      const newZ = MathUtils.damp(z, tz, lambda, delta);
      dispatch(Msg_Animate(node.Id, newX, newY, newZ));
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

  return (
    <>
      <mesh onClick={onClick} onPointerDown={onPointerDown} castShadow receiveShadow ref={ref}>
        {person.Shape === "sphere" ? (
          <sphereGeometry args={[0.4, 16, 16]} />
        ) : (
          <boxGeometry args={[0.6, 0.6, 0.6]} />
        )}
        <meshStandardMaterial
          color={isSelected ? selectedNodeColour : "#FF0000"} // Deep copper if selected, red otherwise
          metalness={0.3} // Slight metallic effect
          roughness={0.3} // Moderate roughness for better light scattering
          emissive={isSelected ? selectedNodeColour : undefined}
          emissiveIntensity={isSelected ? 0.8 : 0}
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
