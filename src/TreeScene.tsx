// TreeScene.tsx
import { Canvas, ThreeEvent } from "@react-three/fiber";
import { OrbitControls, Html } from "@react-three/drei";
import { JSX } from "react";
import React from "react";
import * as THREE from "three";
import { NodeShape } from "./generated/Model";
import { ViewModel, Msg_SelectNode, Msg_StartDrag, Msg_DragTo, Msg_EndDrag, TreeNode } from "./generated/ViewModel";
import { defaultArg } from "./generated/fable_modules/fable-library-ts.4.25.0/Option.js";
import { ofList } from "./generated/fable_modules/fable-library-ts.4.25.0/Map.js";
import { ofArray, head, last } from "./generated/fable_modules/fable-library-ts.4.25.0/List.js";
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
  parent1Id: string,
  parent1Top: THREE.Vector3,
  parent1Bottom: THREE.Vector3,
  parent2Id: string,
  parent2Top: THREE.Vector3,
  parent2Bottom: THREE.Vector3
): JSX.Element[] {
  const connectors: JSX.Element[] = [];

  // Top connector
  connectors.push(
    <Connector
      key={`parent-${parent1Id}-${parent2Id}-connector-top`}
      from={[parent1Top.x, parent1Top.y, parent1Top.z]}
      to={[parent2Top.x, parent2Top.y, parent2Top.z]}
    />
  );
  // Bottom connector
  connectors.push(
    <Connector
      key={`parent-${parent1Id}-${parent2Id}-connector-bottom`}
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

  // Extract the nodeId being dragged from the drag state (Fable union)
  const draggingNodeId = viewModel.GetDraggingNodeId(state);

  // Only allow drag for selected node
  const handlePointerDown = (id: string) => (e: ThreeEvent<PointerEvent>) => {
    if (state.selectedNodeId === id) {
      dispatch(Msg_StartDrag(id, e.point.x, e.point.y, e.point.z));
      e.stopPropagation();
    }
  }
  const handlePointerMove = (id: string) => (e: ThreeEvent<PointerEvent>) => {
    if (draggingNodeId === id && e.point) {
      dispatch(Msg_DragTo(e.point.x, e.point.y, e.point.z));
      e.stopPropagation();
    }
  }
  const handlePointerUp = (id: string) => (e: ThreeEvent<PointerEvent>) => {
    if (draggingNodeId === id) {
      dispatch(Msg_EndDrag());
      e.stopPropagation();
    }
  }
  const handlePointerOut = (id: string) => (e: ThreeEvent<PointerEvent>) => {
    if (draggingNodeId === id) {
      dispatch(Msg_EndDrag());
    }
  }

  const renderedNodes: JSX.Element[] = [];
  for (const item of viewModel.EnumerateRenderableNodes(state)) {
    const [node, person] = item;
    const id = node.id;
    renderedNodes.push(
      <RenderNode
        key={id}
        position={node.position}
        label={defaultArg(person.label, undefined)}
        type={person.shape}
        isSelected={state.selectedNodeId === id}
        onClick={() => dispatch(Msg_SelectNode(id))}
        onPointerDown={handlePointerDown(id)}
        onPointerMove={handlePointerMove(id)}
        onPointerUp={handlePointerUp(id)}
        onPointerOut={handlePointerOut(id)}
      />
    );
  }

  const connectors: JSX.Element[] = [];
  for (const item of viewModel.EnumerateBranchNodes(state)) {
    const [branch, branchData] = item;
    const root1 = state.nodes.get(branchData.parents[0]);
    const root2 = state.nodes.get(branchData.parents[1]);

    const { parent1Top, parent1Bottom, parent2Top, parent2Bottom } =
      calculateParentConnectorVectors(root1.position, root2.position);

    connectors.push(
      ...renderParentConnector(root1.id, parent1Top, parent1Bottom, root2.id, parent2Top, parent2Bottom)
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
        key={`vertical-to-branch-${branch.id}`}
        from={verticalConnectorStart}
        to={branch.position}
      />
    );

    // Add a horizontal connector between the leftmost and rightmost nodes at the branch's Y level.
    // This includes the branch node itself, to handle the case where all children have been dragged
    // to one side of the branch node. Each end of the horiztonal connector should have a small sphere
    // to round out the corner.
    // TODO: Actually find "leftmost" and "rightmost" based on X/Z position, not just first and last in list
    const leftMostNodeId = head(branchData.children);
    const rightMostNodeId = last(branchData.children);
    const branchY = branch.position[1];

    if (leftMostNodeId != rightMostNodeId) {
      const leftMostNode = state.nodes.get(leftMostNodeId);
      const rightMostNode = state.nodes.get(rightMostNodeId);

      const leftJunction: [number, number, number] = [leftMostNode.position[0], branchY, leftMostNode.position[2]];
      const rightJunction: [number, number, number] = [rightMostNode.position[0], branchY, rightMostNode.position[2]];
      connectors.push(
        <Connector
          key={`branch-${branch.id}-to-junctions`}
          from={leftJunction}
          to={rightJunction}
        />
      );

      const endpoints: [string, [number, number, number]][] =
        [[leftMostNodeId, leftJunction], [rightMostNodeId, rightJunction]];

      for (const [nodeId, junction] of endpoints) {
        connectors.push(
          <mesh key={`junction-sphere-${nodeId}`} position={junction}>
            <sphereGeometry args={[0.03, 16, 16]} />
            <meshStandardMaterial color="#AAAAAA" />
          </mesh>
        );
      }
    }

    // Add connectors from the branch node to each child node
    for (const childId of branchData.children) {
      const childPosition = state.nodes.get(childId).position;
      connectors.push(
        <Connector
          key={`branch-to-${childId}`}
          from={[childPosition[0], branchY, childPosition[2]]}
          to={childPosition}
        />
      );
    }
  }

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
