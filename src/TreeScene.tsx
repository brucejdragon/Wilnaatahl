// TreeScene.tsx
import { Canvas, ThreeEvent } from "@react-three/fiber";
import { OrbitControls, DragControls, Html } from "@react-three/drei";
import { JSX } from "react";
import React from "react";
import * as THREE from "three";
import { Person } from "./generated/Model";
import { TreeNode } from "./generated/NodeState";
import {
  ViewModel,
  Msg_SelectNode,
  Msg_DeselectAll,
  Msg_TouchNode,
  Msg_StartDrag,
  Msg_DragTo,
  Msg_EndDrag,
  Msg_Undo,
  Msg_Redo,
  Branch,
} from "./generated/ViewModel";
import { defaultArg } from "./generated/fable_modules/fable-library-ts.4.25.0/Option.js";
import { FSharpList } from "./generated/fable_modules/fable-library-ts.4.25.0/List.js";
import { FSharpMap } from "./generated/fable_modules/fable-library-ts.4.25.0/Map.js";

type TreeSceneProps = {
  initialNodes: FSharpMap<string, TreeNode>;
  initialBranches: FSharpList<Branch>;
};

function TreeNodeMesh({
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
  const selectedNodeColour = "#8B4000"; // Deep, red copper
  const person = node.person;
  const position = node.position;
  const label = defaultArg(person.label, undefined);
  return (
    <>
      <mesh
        position={position}
        onClick={onClick}
        onPointerDown={onPointerDown}
        castShadow
        receiveShadow
      >
        {person.shape === "sphere" ? (
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
        <Html position={[position[0], position[1] - 0.5, position[2]]} center>
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

function ConnectorMesh({
  from,
  to,
}: {
  from: [number, number, number];
  to: [number, number, number];
}) {
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

function ElbowSphereMesh({ position }: { position: [number, number, number] }) {
  return (
    <mesh position={position}>
      <sphereGeometry args={[0.03, 16, 16]} />
      <meshStandardMaterial color="#AAAAAA" />
    </mesh>
  );
}

function calculateParentConnectorSegments(
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

export default function TreeScene({ initialNodes, initialBranches }: TreeSceneProps) {
  const viewModel = new ViewModel();
  const [state, dispatch] = React.useReducer(
    viewModel.Update,
    [initialNodes, initialBranches],
    viewModel.CreateInitialViewState
  );

  const handlePointerDown = (id: string) => (e: ThreeEvent<PointerEvent>) => {
    dispatch(Msg_TouchNode(id));
  };
  const handleNodeClick = (id: string) => (e: ThreeEvent<MouseEvent>) => {
    dispatch(Msg_SelectNode(id));
    e.stopPropagation();
  };
  const handleBackgroundClick = () => {
    dispatch(Msg_DeselectAll());
  };
  const handleDragStart = (origin: THREE.Vector3) => {
    dispatch(Msg_StartDrag(origin.x, origin.y, origin.z));
  };
  const handleDragEnd = () => {
    dispatch(Msg_EndDrag());
  };
  const handleDrag = (l: THREE.Matrix4, dl: THREE.Matrix4, w: THREE.Matrix4, dw: THREE.Matrix4) => {
    const local = new THREE.Vector3();
    l.decompose(local, new THREE.Quaternion(), new THREE.Vector3());
    dispatch(Msg_DragTo(local.x, local.y, local.z));
  };

  const renderedStaticNodes: JSX.Element[] = [];
  for (const node of viewModel.EnumerateUnselectedTreeNodes(state)) {
    const id = node.id;
    renderedStaticNodes.push(
      <TreeNodeMesh
        key={id}
        node={node}
        isSelected={false}
        onClick={handleNodeClick(id)}
        onPointerDown={handlePointerDown(id)}
      />
    );
  }

  const renderedDraggableNodes: JSX.Element[] = [];
  for (const node of viewModel.EnumerateSelectedTreeNodes(state)) {
    const id = node.id;
    renderedDraggableNodes.push(
      <TreeNodeMesh
        key={id}
        node={node}
        isSelected={true}
        onClick={handleNodeClick(id)}
        onPointerDown={handlePointerDown(id)}
      />
    );
  }

  const connectors: JSX.Element[] = [];
  for (const branch of viewModel.EnumerateBranches(state)) {
    const parents = viewModel.EnumerateParents(state, branch);
    const parent1 = parents[0];
    const parent2 = parents[1];

    const { parent1Top, parent1Bottom, parent2Top, parent2Bottom } =
      calculateParentConnectorSegments(parent1.position, parent2.position);

    const parentSegments: [string, THREE.Vector3, THREE.Vector3][] = [
      ["top", parent1Top, parent2Top],
      ["bottom", parent1Bottom, parent2Bottom],
    ];

    for (const [label, v1, v2] of parentSegments) {
      connectors.push(
        <ConnectorMesh
          key={`parent-${parent1.id}-${parent2.id}-connector-${label}`}
          from={[v1.x, v1.y, v1.z]}
          to={[v2.x, v2.y, v2.z]}
        />
      );
    }

    // Calculate the branch position dynamically based on the highest child node and
    // midpoint of the bottom connector between the parents.
    // Use Three.js to calculate the midpoint between parent1Bottom and parent2Bottom
    const verticalConnectorStartVec = new THREE.Vector3().lerpVectors(
      parent1Bottom,
      parent2Bottom,
      0.5
    );
    const verticalConnectorStart: [number, number, number] = [
      verticalConnectorStartVec.x,
      verticalConnectorStartVec.y,
      verticalConnectorStartVec.z,
    ];

    // Get all children of the branch
    const children = viewModel.EnumerateChildren(state, branch);

    // Find the highest child node
    var highestChildY = -Infinity;
    for (const child of children) {
      const childY = child.position[1];
      if (childY > highestChildY) {
        highestChildY = childY;
      }
    }

    const branchPosition: [number, number, number] = [
      verticalConnectorStartVec.x,
      highestChildY + 0.65,
      verticalConnectorStartVec.z,
    ];

    // Add a vertical connector from the midpoint of the bottom connector to the branch position
    connectors.push(
      <ConnectorMesh
        key={`vertical-to-branch-${branch.id}`}
        from={verticalConnectorStart}
        to={branchPosition}
      />
    );

    // Add connectors from the branch node to each child node. Unless a child node is directly below
    // the branch node, a right-angle connector with sphere "elbow" is needed.
    var childrenDirectlyBelow = 0;
    for (const child of children) {
      const childPosition = child.position;
      const childId = child.id;
      const branchY = branchPosition[1];

      var childConnectorKey: React.Key;
      if (childPosition[0] !== branchPosition[0] || childPosition[2] !== branchPosition[2]) {
        // Child is not directly below branch, so add right-angle connector with sphere "elbow"
        const junction: [number, number, number] = [childPosition[0], branchY, childPosition[2]];
        connectors.push(
          <ConnectorMesh
            key={`branch-to-${childId}-junction`}
            from={branchPosition}
            to={junction}
          />
        );
        connectors.push(<ElbowSphereMesh key={`junction-sphere-${childId}`} position={junction} />);
        childConnectorKey = `junction-to-${childId}`;
      } else {
        // Child is directly below branch, so a straight connector suffices, and we won't
        // need an "elbow" sphere at the branch point later.
        childrenDirectlyBelow++;
        childConnectorKey = `branch-to-${childId}`;
      }

      connectors.push(
        <ConnectorMesh
          key={childConnectorKey}
          from={[childPosition[0], branchY, childPosition[2]]}
          to={childPosition}
        />
      );
    }

    // Unless there is a child directly below the branch, add another sphere for the elbow
    // at the branch end of the connector.
    if (childrenDirectlyBelow === 0) {
      connectors.push(
        <ElbowSphereMesh key={`junction-sphere-${branch.id}`} position={branchPosition} />
      );
    }
  }

  const tree = [...renderedStaticNodes, ...connectors];
  const shouldEnableOrbitControls = viewModel.ShouldEnableOrbitControls(state);

  // Undo/Redo buttons
  // Disable if no undo/redo available
  const canRedo = viewModel.CanRedo(state);
  const canUndo = viewModel.CanUndo(state);

  return (
    <div
      style={{
        width: "100vw",
        height: "100vh",
        display: "flex",
        flexDirection: "column",
        justifyContent: "center",
        alignItems: "center",
      }}
    >
      <div style={{ margin: "8px" }}>
        <button onClick={() => dispatch(Msg_Undo())} disabled={!canUndo}>
          Undo
        </button>
        <button
          onClick={() => dispatch(Msg_Redo())}
          disabled={!canRedo}
          style={{ marginLeft: "8px" }}
        >
          Redo
        </button>
      </div>
      <div style={{ flex: 1, width: "100%", height: "100%" }}>
        <Canvas
          camera={{ position: [0, 0, 6], fov: 50 }}
          shadows
          onPointerMissed={handleBackgroundClick}
        >
          {/* Ambient light for general illumination */}
          <ambientLight intensity={0.7} />
          {/* Directional light for stronger highlights and shadows */}
          <directionalLight position={[5, 5, 5]} intensity={1} castShadow />
          {/* Additional point light for more dynamic lighting */}
          <pointLight position={[1, -1, 2]} intensity={5} castShadow />
          <OrbitControls enabled={shouldEnableOrbitControls} />
          <group position={[0, 1, 0]}>
            <DragControls
              autoTransform={false}
              axisLock="z"
              onDragStart={handleDragStart}
              onDrag={handleDrag}
              onDragEnd={handleDragEnd}
            >
              {renderedDraggableNodes}
            </DragControls>
            {tree}
          </group>
        </Canvas>
      </div>
    </div>
  );
}
