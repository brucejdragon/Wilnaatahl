import React from "react";
import { OrbitControls } from "@react-three/drei";
import { useViewModel } from "../context/viewModelContext";
import { HuwilpGroup } from "./HuwilpGroup";

export default function TreeScene() {
  const { viewModel, state } = useViewModel();

  return (
    <group>
      {/* Ambient light for general illumination */}
      <ambientLight intensity={0.7} />
      {/* Directional light for stronger highlights and shadows */}
      <directionalLight position={[5, 5, 5]} intensity={1} castShadow />
      {/* Additional point light for more dynamic lighting */}
      <pointLight position={[1, -1, 2]} intensity={5} castShadow />
      <OrbitControls enabled={viewModel.ShouldEnableOrbitControls(state)} />
      <HuwilpGroup />
    </group>
  );
}
