// TreeScene.tsx
import { Canvas, ThreeEvent } from "@react-three/fiber";
import { OrbitControls, Html } from "@react-three/drei";
import { JSX } from "react";
import React from "react";
import * as THREE from "three";
import { NodeShape } from "./generated/Model";
import {
  ViewModel,
  Msg_SelectNode,
  Msg_StartDrag,
  Msg_DragTo,
  Msg_EndDrag,
  TreeNode
} from "./generated/ViewModel";
import { defaultArg } from "./generated/fable_modules/fable-library-ts.4.25.0/Option.js";
import { ofList } from "./generated/fable_modules/fable-library-ts.4.25.0/Map.js";
import { ofArray } from "./generated/fable_modules/fable-library-ts.4.25.0/List.js";
import { comparePrimitives } from "./generated/fable_modules/fable-library-ts.4.25.0/Util.js";

type TreeSceneProps = {
  initialNodes: Record<string, TreeNode>;
};

function RenderNode({
  position,
  label,
  type,
  isSelected = false,
  onClick,
  onPointerDown,
  onPointerMove,
  onPointerUp,
  onPointerOut,
}: {
  position: [number, number, number];
  label?: string;
  type: NodeShape;
  isSelected?: boolean;
  onClick?: (e: ThreeEvent<MouseEvent>) => void;
  onPointerDown?: (e: ThreeEvent<PointerEvent>) => void;
  onPointerMove?: (e: ThreeEvent<PointerEvent>) => void;
  onPointerUp?: (e: ThreeEvent<PointerEvent>) => void;
  onPointerOut?: (e: ThreeEvent<PointerEvent>) => void;
}) {
  const SelectedNodeColour = "#8B4000"; // Deep, red copper
  return (
    <>
      <mesh
        position={position}
        onClick={onClick}
        onPointerDown={onPointerDown}
        onPointerMove={onPointerMove}
        onPointerUp={onPointerUp}
        onPointerOut={onPointerOut}
        castShadow
        receiveShadow
      >
        {type === "sphere" ? (
          <sphereGeometry args={[0.4, 16, 16]} />
        ) : (
          <boxGeometry args={[0.6, 0.6, 0.6]} />
        )}
        <meshStandardMaterial
          color={isSelected ? SelectedNodeColour : "#FF0000"} // Deep copper if selected, red otherwise
          metalness={0.3} // Slight metallic effect
          roughness={0.3} // Moderate roughness for better light scattering
          emissive={isSelected ? SelectedNodeColour : undefined}
          emissiveIntensity={isSelected ? 0.8 : 0}
        />
      </mesh>
      {label && (
        <Html position={[position[0], position[1] - 0.5, position[2]]} center>
          <div style={{ color: "white", fontSize: "16px", textAlign: "center", pointerEvents: "none" }}>
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

function calculateParentConnectorVectors(
  parent1Position: [number, number, number],
  parent2Position: [number, number, number]
): {
  parent1Top: THREE.Vector3;
  parent1Bottom: THREE.Vector3;
  parent2Top: THREE.Vector3;
  parent2Bottom: THREE.Vector3;
} {
  const gap = 0.2; // Fixed gap between parent connectors

  // Calculate the vector between the two parent nodes
  const p1 = new THREE.Vector3(...parent1Position);
  const p2 = new THREE.Vector3(...parent2Position);
  const dir = p2.clone().sub(p1).normalize();

  // Find a vector perpendicular to dir in the XY plane
  const perp = new THREE.Vector3(-dir.y, dir.x, 0).normalize();

  // Top and bottom offsets
  const offset = perp.clone().multiplyScalar(gap / 2);

  // Calculate connector endpoints
  const p1Top = p1.clone().add(offset);
  const p1Bottom = p1.clone().sub(offset);
  const p2Top = p2.clone().add(offset);
  const p2Bottom = p2.clone().sub(offset);
  return { parent1Top: p1Top, parent1Bottom: p1Bottom, parent2Top: p2Top, parent2Bottom: p2Bottom };
}

function renderParentConnector(
  parent1Top: THREE.Vector3,
  parent1Bottom: THREE.Vector3,
  parent2Top: THREE.Vector3,
  parent2Bottom: THREE.Vector3
): JSX.Element[] {
  const connectors: JSX.Element[] = [];

  // Top connector
  connectors.push(
    <Connector
      key="parent-connector-top"
      from={[parent1Top.x, parent1Top.y, parent1Top.z]}
      to={[parent2Top.x, parent2Top.y, parent2Top.z]}
    />
  );
  // Bottom connector
  connectors.push(
    <Connector
      key="parent-connector-bottom"
      from={[parent1Bottom.x, parent1Bottom.y, parent1Bottom.z]}
      to={[parent2Bottom.x, parent2Bottom.y, parent2Bottom.z]}
    />
  );

  return connectors;
}

export default function TreeScene({ initialNodes }: TreeSceneProps) {
  const viewModel = new ViewModel();

  // Convert nodes prop to F# State for reducer init
  const fsharpMap = ofList(ofArray(Object.entries(initialNodes)), { Compare: comparePrimitives });
  const initialState = viewModel.CreateInitialViewState(fsharpMap);

  const [state, dispatch] = React.useReducer(viewModel.Update, initialState);

  // Always use positions from reducer state
  const nodes = state.nodes;

  // Extract the nodeId being dragged from the drag state (Fable union)
  const draggingNodeId = viewModel.GetDraggingNodeId(state);

  // Only allow drag for selected node
  const handlePointerDown = (id: string) => (e: ThreeEvent<PointerEvent>) => {
    if (state.selectedNodeId === id) {
      dispatch(Msg_StartDrag(id, e.point.x, e.point.y, e.point.z));
      e.stopPropagation();
    }
  };
  const handlePointerMove = (id: string) => (e: ThreeEvent<PointerEvent>) => {
    if (draggingNodeId === id && e.point) {
      dispatch(Msg_DragTo(e.point.x, e.point.y, e.point.z));
      e.stopPropagation();
    }
  };
  const handlePointerUp = (id: string) => (e: ThreeEvent<PointerEvent>) => {
    if (draggingNodeId === id) {
      dispatch(Msg_EndDrag());
      e.stopPropagation();
    }
  };
  const handlePointerOut = (id: string) => (e: ThreeEvent<PointerEvent>) => {
    if (draggingNodeId === id) {
      dispatch(Msg_EndDrag());
    }
  };

  const renderedNodes = Array.from(nodes.entries())
    .filter(([id]) => id !== "branch") // Exclude the branch node
    .map(([id, node]) => {
      const person = defaultArg(node.person, undefined);
      return (
        <RenderNode
          key={id}
          position={node.position}
          label={defaultArg(person?.label, undefined)} // Use the label from the Person object
          type={person?.shape ?? "sphere"} // Default to Sphere if no Person
          isSelected={state.selectedNodeId === id}
          onClick={() => dispatch(Msg_SelectNode(id))}
          onPointerDown={handlePointerDown(id)}
          onPointerMove={handlePointerMove(id)}
          onPointerUp={handlePointerUp(id)}
          onPointerOut={handlePointerOut(id)}
        />
      );
    });
  const connectors: JSX.Element[] = [];

  const root1 = nodes.get("root1");
  const root2 = nodes.get("root2");
  const branch = nodes.get("branch");

  const { parent1Top, parent1Bottom, parent2Top, parent2Bottom } =
    calculateParentConnectorVectors(root1.position, root2.position);

  connectors.push(
    ...renderParentConnector(parent1Top, parent1Bottom, parent2Top, parent2Bottom)
  );

  // Add a vertical connector from the midpoint of the bottom connector to the branch node
  // Use Three.js to calculate the midpoint between parent1Bottom and parent2Bottom
  const verticalConnectorStartVec = new THREE.Vector3().lerpVectors(parent1Bottom, parent2Bottom, 0.5);
  const verticalConnectorStart: [number, number, number] = [
    verticalConnectorStartVec.x,
    verticalConnectorStartVec.y,
    verticalConnectorStartVec.z,
  ];
  connectors.push(
    <Connector
      key="vertical-to-branch"
      from={verticalConnectorStart}
      to={branch.position}
    />
  );

  // Add connectors from the branch node to each child node
  for (var childId of branch.children) {
    const childPosition = nodes.get(childId).position;

    // For siblings off the center line, create right-angle connectors
    if (childId === "child1" || childId === "child3") {
      const branchY = branch.position[1];
      const junction = [childPosition[0], branchY, childPosition[2]] as [
        number,
        number,
        number
      ];

      // Add the first segment from the branch to the junction
      connectors.push(
        <Connector
          key={`branch-to-${childId}-junction`}
          from={[branch.position[0], branchY, branch.position[2]]}
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
          from={branch.position}
          to={childPosition}
        />
      );
    }
  };

  const tree = [...renderedNodes, ...connectors];

  return (
    <div style={{ width: "100vw", height: "100vh", display: "flex", justifyContent: "center", alignItems: "center" }}>
      <Canvas camera={{ position: [0, 0, 6], fov: 50 }} shadows>
        {/* Ambient light for general illumination */}
        <ambientLight intensity={0.7} />
        {/* Directional light for stronger highlights and shadows */}
        <directionalLight position={[5, 5, 5]} intensity={1} castShadow />
        {/* Additional point light for more dynamic lighting */}
        <pointLight position={[1, -1, 2]} intensity={5} castShadow />
        <OrbitControls enabled={viewModel.ShouldEnableOrbitControls(state)} />
        <group position={[0, 1, 0]}>
          {tree}
        </group>
      </Canvas>
    </div>
  );
}
