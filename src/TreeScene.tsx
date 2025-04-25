// TreeScene.tsx
import { useMemo } from "react";
import * as THREE from "three";
import React from "react";

type Node = {
  id: string;
  position: [number, number, number];
  children: string[];
};

const nodes: Record<string, Node> = {
  root: { id: "root", position: [0, 0, 0], children: ["child1", "child2"] },
  child1: { id: "child1", position: [-1.5, -2, 0], children: [] },
  child2: { id: "child2", position: [1.5, -2, 0], children: [] },
};

function SphereNode({ position }: { position: [number, number, number] }) {
  return (
    <mesh position={position}>
      <sphereGeometry args={[0.4, 16, 16]} />
      <meshStandardMaterial color="#FFD700" />
    </mesh>
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
      <SphereNode key={node.id} position={node.position} />
    ));

    const connectors = Object.values(nodes).flatMap((node) =>
      node.children.map((childId) => (
        <Connector
          key={`${node.id}-${childId}`}
          from={node.position}
          to={nodes[childId].position}
        />
      ))
    );

    return [...spheres, ...connectors];
  }, []);

  return (
    <group position={[0, 1, 0]}>
      {tree}
    </group>
  );
  }
