// App shell: src/App.tsx
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import TreeScene from "./TreeScene";
import React from "react";

function App() {
  return (
    <div className="w-full h-screen">
      <Canvas camera={{ position: [0, 0, 6], fov: 50 }}>
        <ambientLight intensity={0.5} />
        <pointLight position={[10, 10, 10]} />
        <OrbitControls />
        <TreeScene />
      </Canvas>
    </div>
  );
}

export default App;
