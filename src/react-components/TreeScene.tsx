import React from "react";
import { useFrame } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { useQuery, useWorld } from "koota/react";
import { Dragging, runSystems } from "../ecs";
import { WilpGroup } from "./WilpGroup";

export default function TreeScene() {
  const world = useWorld();
  const isDragInProgress = useQuery(Dragging("*")).length > 0;

  useFrame((_state, delta) => {
    runSystems({ world, delta });
  });

  return (
    <group>
      {/* Ambient light for general illumination */}
      <ambientLight intensity={0.7} />
      {/* Directional light for stronger highlights and shadows */}
      <directionalLight position={[5, 5, 5]} intensity={1} castShadow />
      {/* Additional point light for more dynamic lighting */}
      <pointLight position={[1, -1, 2]} intensity={5} castShadow />
      <OrbitControls enabled={!isDragInProgress} />
      <WilpGroup />
    </group>
  );
}
