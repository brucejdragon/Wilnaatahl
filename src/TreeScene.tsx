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

function RenderNode({
  id,
  position,
  label,
  type,
}: {
  id: string;
  position: [number, number, number];
  label?: string;
  type: NodeShape;
}) {
  if (id === "branch") {
    return null; // Do not render the branch node
  }

  return (
    <>
      <mesh position={position}>
        {type === NodeShape.Sphere ? (
          <sphereGeometry args={[0.4, 16, 16]} /> // Sphere for mothers
        ) : (
          <boxGeometry args={[0.6, 0.6, 0.6]} /> // Cube for fathers
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

export default function TreeScene() {
  const tree = useMemo(() => {
    const spheres = Object.entries(nodes).map(([id, node]) => {
      let label: string | undefined;
      let type: NodeShape = NodeShape.Sphere; // Default to mother

      // Define labels and types for specific nodes
      if (id === "root2") {
        label = "GGGG Grandfather";
        type = NodeShape.Cube;
      }
      if (id === "child1") {
        label = "GGG Grandmother";
      }
      if (id === "child2") {
        label = "GGG Granduncle H";
        type = NodeShape.Cube;
      }
      if (id === "child3") {
        label = "GGG Granduncle N";
        type = NodeShape.Cube;
      }

      return <RenderNode key={id} id={id} position={node.position} label={label} type={type} />;
    });

    const connectors: JSX.Element[] = [];

    // Adjust the vertical position of the "equals sign" connector
    const parentConnectorY = nodes.root1.position[1]; // Align with the parent nodes' vertical position
    const gapBetweenLines = 0.2; // Vertical gap between the two lines
    const centerX = (nodes.root1.position[0] + nodes.root2.position[0]) / 2; // Center between root1 and root2

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

    // Add a vertical connector from the midpoint of the bottom line to the branch node
    const verticalConnectorStart: [number, number, number] = [
      centerX,
      (parentConnectorY - gapBetweenLines / 2) - 0.2,
      0
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
