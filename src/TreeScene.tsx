// TreeScene.tsx
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { JSX, useMemo } from "react";
import * as THREE from "three";
import React from "react";
import { Text } from "@react-three/drei";

type Node = {
  id: string;
  position: [number, number, number];
  children: string[];
};

const nodes: Record<string, Node> = {
  root1: { id: "root1", position: [-1, 0, 0], children: [] },
  root2: { id: "root2", position: [1, 0, 0], children: [] },
  branch: { id: "branch", position: [0, -1, 0], children: ["child1", "child2", "child3"] },
  child1: { id: "child1", position: [-2, -2, 0], children: [] },
  child2: { id: "child2", position: [0, -2, 0], children: [] },
  child3: { id: "child3", position: [2, -2, 0], children: [] },
};

function SphereNode({ id, position }: { id: string; position: [number, number, number] }) {
  if (id === "branch") {
    return null; // Do not render the branch node
  }

  return (
    <>
      <mesh position={position}>
        <sphereGeometry args={[0.4, 16, 16]} />
        <meshStandardMaterial color="#FF0000" /> {/* Red spheres */}
      </mesh>
      {id === "root2" && (
        <Text
          position={[position[0], position[1] - 0.6, position[2]]}
          fontSize={0.2}
          color="#FFFFFF"
        >
          GGGG Grampa
        </Text>
      )}
      {id === "child1" && (
        <Text
          position={[position[0], position[1] - 0.6, position[2]]}
          fontSize={0.2}
          color="#FFFFFF"
        >
          GGG Grandma
        </Text>
      )}
      {id === "child2" && (
        <Text
          position={[position[0], position[1] - 0.6, position[2]]}
          fontSize={0.2}
          color="#FFFFFF"
        >
          GGG Uncle
        </Text>
      )}
      {id === "child3" && (
        <Text
          position={[position[0], position[1] - 0.6, position[2]]}
          fontSize={0.2}
          color="#FFFFFF"
        >
          GGG Uncle
        </Text>
      )}
    </>
  );
}

function Connector({ from, to }: { from: [number, number, number]; to: [number, number, number] }) {
  const direction = new THREE.Vector3(...to).sub(new THREE.Vector3(...from));
  const length = direction.length();
  const mid = new THREE.Vector3(...from).add(direction.clone().multiplyScalar(0.5));
  const orientation = new THREE.Quaternion().setFromUnitVectors(
    new THREE.Vector3(0, 1, 0), // cylinder's up axis
    direction.clone().normalize()
  );

  return (
    <mesh position={mid} quaternion={orientation}>
      <cylinderGeometry args={[0.05, 0.05, length, 8]} />
      <meshStandardMaterial color="#AAAAAA" />
    </mesh>
  );
}

export default function TreeScene() {
  const tree = useMemo(() => {
    const spheres = Object.values(nodes).map((node) => (
      <SphereNode key={node.id} id={node.id} position={node.position} />
    ));

    const connectors: JSX.Element[] = [];

    // Add a horizontal connector between root1 and root2
    connectors.push(
      <Connector
        key="root1-root2"
        from={nodes.root1.position}
        to={nodes.root2.position}
      />
    );

    // Add a vertical connector from the midpoint of root1-root2 to the branch node
    const horizontalMidpoint = [
      (nodes.root1.position[0] + nodes.root2.position[0]) / 2,
      (nodes.root1.position[1] + nodes.root2.position[1]) / 2,
      (nodes.root1.position[2] + nodes.root2.position[2]) / 2,
    ] as [number, number, number];

    connectors.push(
      <Connector
        key="horizontal-to-branch"
        from={horizontalMidpoint}
        to={nodes.branch.position}
      />
    );

    // Add connectors from the branch node to each child node
    nodes.branch.children.forEach((childId) => {
      connectors.push(
        <Connector
          key={`branch-to-${childId}`}
          from={nodes.branch.position}
          to={nodes[childId].position}
        />
      );
    });

    return [...spheres, ...connectors];
  }, []);

  return (
    <div style={{ width: "100vw", height: "100vh", display: "flex", justifyContent: "center", alignItems: "center" }}>
      <Canvas camera={{ position: [0, 0, 6], fov: 50 }}>
        {/* Ambient light for general illumination */}
        <ambientLight intensity={0.7} />

        {/* Directional light for stronger highlights and shadows */}
        <directionalLight position={[5, 5, 5]} intensity={1} castShadow />

        {/* Additional point light for more dynamic lighting */}
        <pointLight position={[-5, -5, 5]} intensity={0.8} />

        <OrbitControls />
        <group position={[0, 1, 0]}>
          {tree}
        </group>
      </Canvas>
    </div>
  );
}
