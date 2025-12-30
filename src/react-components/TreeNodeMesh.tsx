import { ThreeEvent, useFrame } from "@react-three/fiber";
import { Html } from "@react-three/drei";
import React from "react";
import { MathUtils, Mesh } from "three";
import { TreeNode } from "../generated/ViewModel/NodeState";
import { Msg_Animate } from "../generated/ViewModel/ViewModel";
import { useViewModel } from "../context/viewModelContext";
import { defaultArg } from "../generated/fable_modules/fable-library-ts.4.27.0/Option.js";

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
      </mesh>
      {label && (
        <Html position={[x, y - 0.5, z]} center>
          <div
            style={{ color: "white", fontSize: "16px", textAlign: "center", pointerEvents: "none" }}
          >
            {label}
          </div>
        </Html>
      )}
    </>
  );
}
