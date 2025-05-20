// TreeScene.tsx
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { JSX, useMemo } from "react";
import * as THREE from "three";
import React from "react";
import { Html } from "@react-three/drei";

enum NodeShape {
  Sphere = "sphere",
  Cube = "cube",
}

type Person = {
  label?: string; // Optional label
  type: NodeShape;
};

// Create a collection of Person objects
const people: Person[] = [
  { label: undefined, type: NodeShape.Sphere }, // root1
  { label: "GGGG Grandfather", type: NodeShape.Cube }, // root2
  { label: "GGG Grandmother", type: NodeShape.Sphere }, // child1
  { label: "GGG Granduncle H", type: NodeShape.Cube }, // child2
  { label: "GGG Granduncle N", type: NodeShape.Cube }, // child3
];

type Node = {
  id: string;
  position: [number, number, number];
  children: string[];
  person?: Person; // Optional reference to a Person
};

// Update the nodes to reference the Person collection
const nodes: Record<string, Node> = {
  root1: { id: "root1", position: [-1, 0, 0], children: [], person: people[0] },
  root2: { id: "root2", position: [1, 0, 0], children: [], person: people[1] },
  branch: { id: "branch", position: [0, -1, 0], children: ["child1", "child2", "child3"] },
  child1: { id: "child1", position: [-2, -2, 0], children: [], person: people[2] },
  child2: { id: "child2", position: [0, -2, 0], children: [], person: people[3] },
  child3: { id: "child3", position: [2, -2, 0], children: [], person: people[4] },
};

function RenderNode({
  position,
  label,
  type,
}: {
  position: [number, number, number];
  label?: string;
  type: NodeShape;
}) {
  return (
    <>
      <mesh position={position}>
        {type === NodeShape.Sphere ? (
          <sphereGeometry args={[0.4, 16, 16]} />
        ) : (
          <boxGeometry args={[0.6, 0.6, 0.6]} />
        )}
        <meshStandardMaterial
          color="#FF0000" // Red for all nodes
          metalness={0.3} // Slight metallic effect
          roughness={0.3} // Moderate roughness for better light scattering
        />
      </mesh>
      {label && (
        <Html position={[position[0], position[1] - 0.6, position[2]]} center>
          <div style={{ color: "white", fontSize: "12px", textAlign: "center" }}>
            {label}
          </div>
        </Html>
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
      <cylinderGeometry args={[0.03, 0.03, length, 8]} />
      <meshStandardMaterial color="#AAAAAA" />
    </mesh>
  );
}

function renderParentConnector(
  parent1Position: [number, number, number],
  parent2Position: [number, number, number]
): JSX.Element[] {
  const connectors: JSX.Element[] = [];
  const parentConnectorY = parent1Position[1]; // Align with the parent nodes' vertical position
  const gapBetweenLines = 0.2; // Vertical gap between the two lines
  const centerX = (parent1Position[0] + parent2Position[0]) / 2; // Center between parent1 and parent2

  // Top horizontal line
  connectors.push(
    <Connector
      key="parent-connector-top"
      from={[centerX - 0.5, parentConnectorY + gapBetweenLines / 2, 0]} // Start slightly left of center
      to={[centerX + 0.5, parentConnectorY + gapBetweenLines / 2, 0]} // End slightly right of center
    />
  );

  // Bottom horizontal line
  connectors.push(
    <Connector
      key="parent-connector-bottom"
      from={[centerX - 0.5, parentConnectorY - gapBetweenLines / 2, 0]} // Start slightly left of center
      to={[centerX + 0.5, parentConnectorY - gapBetweenLines / 2, 0]} // End slightly right of center
    />
  );

  return connectors;
}

export default function TreeScene() {
  const tree = useMemo(() => {
    const spheres = Object.entries(nodes)
      .filter(([id]) => id !== "branch") // Exclude the branch node
      .map(([id, node]) => {
        const person = node.person;

        return (
          <RenderNode
            key={id}
            position={node.position}
            label={person?.label} // Use the label from the Person object
            type={person?.type ?? NodeShape.Sphere} // Default to Sphere if no Person
          />
        );
      });
    const connectors: JSX.Element[] = [];

    connectors.push(
      ...renderParentConnector(nodes.root1.position, nodes.root2.position)
    );

    // Add a vertical connector from the midpoint of the bottom line to the branch node
    const parentConnectorY = nodes.root1.position[1];
    const gapBetweenLines = 0.2;
    const centerX = (nodes.root1.position[0] + nodes.root2.position[0]) / 2;
    const verticalConnectorStart: [number, number, number] = [
      centerX,
      parentConnectorY - gapBetweenLines / 2 - 0.2,
      0,
    ]; // Slight gap below the bottom line
    connectors.push(
      <Connector
        key="vertical-to-branch"
        from={verticalConnectorStart}
        to={nodes.branch.position}
      />
    );

    // Add connectors from the branch node to each child node
    nodes.branch.children.forEach((childId) => {
      const childPosition = nodes[childId].position;

      // For siblings off the center line, create right-angle connectors
      if (childId === "child1" || childId === "child3") {
        // Lower the junction point closer to the child node
        const loweredBranchY = nodes.branch.position[1] - 0.35; // Adjust branch point downward
        const junction = [childPosition[0], loweredBranchY, childPosition[2]] as [
          number,
          number,
          number
        ];

        // Add the first segment from the branch to the junction
        connectors.push(
          <Connector
            key={`branch-to-${childId}-junction`}
            from={[nodes.branch.position[0], loweredBranchY, nodes.branch.position[2]]}
            to={junction}
          />
        );

        // Add the second segment from the junction to the child node
        connectors.push(
          <Connector
            key={`junction-to-${childId}`}
            from={junction}
            to={childPosition}
          />
        );

        // Add a small sphere at the junction point
        connectors.push(
          <mesh key={`junction-sphere-${childId}`} position={junction}>
            <sphereGeometry args={[0.03, 16, 16]} /> {/* Same radius as the cylinders */}
            <meshStandardMaterial color="#AAAAAA" /> {/* Same color as the cylinders */}
          </mesh>
        );
      } else {
        // For the center child node, use a straight connector
        connectors.push(
          <Connector
            key={`branch-to-${childId}`}
            from={nodes.branch.position}
            to={childPosition}
          />
        );
      }
    });

    return [...spheres, ...connectors];
  }, []);

  return (
    <div style={{ width: "100vw", height: "100vh", display: "flex", justifyContent: "center", alignItems: "center" }}>
      <Canvas camera={{ position: [0, 0, 6], fov: 50 }} shadows>
        {/* Ambient light for general illumination */}
        <ambientLight intensity={0.7} />

        {/* Directional light for stronger highlights and shadows */}
        <directionalLight position={[5, 5, 5]} intensity={1} castShadow />

        {/* Additional point light for more dynamic lighting */}
        <pointLight position={[1, -1, 2]} intensity={5} castShadow />

        <OrbitControls />
        <group position={[0, 1, 0]}>
          {tree}
        </group>
      </Canvas>
    </div>
  );
}
