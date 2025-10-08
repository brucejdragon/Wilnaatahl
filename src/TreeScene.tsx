// TreeScene.tsx
import { Canvas, ThreeEvent } from "@react-three/fiber";
import { OrbitControls, DragControls, Html } from "@react-three/drei";
import { JSX } from "react";
import React from "react";
import * as THREE from "three";
import { Family } from "./generated/Model";
import { TreeNode } from "./generated/NodeState";
import {
  Msg_SelectNode,
  Msg_DeselectAll,
  Msg_ToggleSelection,
  Msg_TouchNode,
  Msg_StartDrag,
  Msg_DragTo,
  Msg_EndDrag,
  Msg_Undo,
  Msg_Redo,
  ViewModel,
} from "./generated/ViewModel";
import { defaultArg } from "./generated/fable_modules/fable-library-ts.4.25.0/Option.js";

type TreeSceneProps = {
  initialNodes: Iterable<TreeNode>;
  initialFamilies: Iterable<Family>;
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
  const person = node.Person;
  const position = node.Position;
  const label = defaultArg(person.Label, undefined);
  return (
    <>
      <mesh
        position={position}
        onClick={onClick}
        onPointerDown={onPointerDown}
        castShadow
        receiveShadow
      >
        {person.Shape === "sphere" ? (
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

function ConnectorMesh({ from, to }: { from: THREE.Vector3; to: THREE.Vector3 }) {
  const direction = to.clone().sub(from);
  const length = direction.length();
  const mid = from.clone().add(direction.clone().multiplyScalar(0.5));
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

function ElbowSphereMesh({ position }: { position: THREE.Vector3 }) {
  return (
    <mesh position={position}>
      <sphereGeometry args={[0.03, 16, 16]} />
      <meshStandardMaterial color="#AAAAAA" />
    </mesh>
  );
}

function makeFamilyId(parent1: TreeNode, parent2: TreeNode): string {
  return `${parent1.Person.Id}-${parent2.Person.Id}-family`;
}

function ChildrenGroup({
  familyId,
  position, // Location of the top of vertical connector that goes down to the children.
  children,
}: {
  familyId: string;
  position: THREE.Vector3;
  children: Iterable<TreeNode>;
}) {
  // Calculate the branch position dynamically based on the highest child node and
  // the given midpoint of the bottom connector between the parents.
  var highestChildY = -Infinity;
  for (const child of children) {
    const childY = child.Position[1];
    if (childY > highestChildY) {
      highestChildY = childY;
    }
  }

  const branchPosition = new THREE.Vector3(position.x, highestChildY + 0.65, position.z);
  const connectors: JSX.Element[] = [];
  connectors.push(
    <ConnectorMesh key={`${familyId}-vertical-to-branch`} from={position} to={branchPosition} />
  );

  // Add connectors from the branch position to each child node. Unless a child node is directly below
  // the branch position, a right-angle connector with sphere "elbow" is needed.
  var childrenDirectlyBelow = 0;
  for (const child of children) {
    const childPosition = new THREE.Vector3(...child.Position);
    const childId = child.Person.Id;
    const branchY = branchPosition.y;

    var childConnectorKey: React.Key;
    const junction = new THREE.Vector3(childPosition.x, branchY, childPosition.z);
    if (childPosition.x !== branchPosition.x || childPosition.z !== branchPosition.z) {
      // Child is not directly below branch, so add right-angle connector with sphere "elbow"
      connectors.push(
        <ConnectorMesh key={`branch-to-${childId}-junction`} from={branchPosition} to={junction} />
      );
      connectors.push(<ElbowSphereMesh key={`${childId}-junction-sphere`} position={junction} />);
      childConnectorKey = `junction-to-${childId}`;
    } else {
      // Child is directly below branch, so a straight connector suffices, and we won't
      // need an "elbow" sphere at the branch point later.
      childrenDirectlyBelow++;
      childConnectorKey = `branch-to-${childId}`;
    }

    connectors.push(<ConnectorMesh key={childConnectorKey} from={junction} to={childPosition} />);
  }

  // Unless there is a child directly below the branch, add another sphere for the elbow
  // at the branch end of the connector.
  if (childrenDirectlyBelow === 0) {
    connectors.push(
      <ElbowSphereMesh key={`${familyId}-junction-sphere`} position={branchPosition} />
    );
  }

  return <group>{connectors}</group>;
}

function FamilyGroup({
  parent1,
  parent2,
  children,
}: {
  parent1: TreeNode;
  parent2: TreeNode;
  children: Iterable<TreeNode>;
}) {
  // Calculate the vector between the two parent nodes
  const p1 = new THREE.Vector3(...parent1.Position);
  const p2 = new THREE.Vector3(...parent2.Position);
  const dir = p2.clone().sub(p1).normalize();

  // Find a vector perpendicular to dir in the XY plane
  const perp = new THREE.Vector3(-dir.y, dir.x, 0).normalize();

  // Top and bottom offsets
  const gap = 0.1; // Fixed gap between parent connectors
  const offset = perp.clone().multiplyScalar(gap);

  // Calculate connector endpoints
  const parent1Top = p1.clone().add(offset);
  const parent1Bottom = p1.clone().sub(offset);
  const parent2Top = p2.clone().add(offset);
  const parent2Bottom = p2.clone().sub(offset);

  // Calculate the midpoint of the bottom connector between the parents.
  // We'll need this for the position of the child connector group.
  const verticalConnectorStart = new THREE.Vector3().lerpVectors(parent1Bottom, parent2Bottom, 0.5);
  const familyId = makeFamilyId(parent1, parent2);
  const parent1Id = parent1.Person.Id;
  const parent2Id = parent2.Person.Id;

  return (
    <group>
      <ConnectorMesh
        key={`parent-${parent1Id}-${parent2Id}-connector-top`}
        from={parent1Top}
        to={parent2Top}
      />
      <ConnectorMesh
        key={`parent-${parent1Id}-${parent2Id}-connector-bottom`}
        from={parent1Bottom}
        to={parent2Bottom}
      />
      <ChildrenGroup
        key={`${familyId}-children`}
        familyId={familyId}
        position={verticalConnectorStart}
        children={children}
      />
    </group>
  );
}

export default function TreeScene({ initialNodes, initialFamilies }: TreeSceneProps) {
  const viewModel = new ViewModel();
  const [state, dispatch] = React.useReducer(
    viewModel.Update,
    [initialNodes, initialFamilies],
    viewModel.CreateInitialViewState
  );

  const handlePointerDown = (id: number) => (e: ThreeEvent<PointerEvent>) => {
    dispatch(Msg_TouchNode(id));
  };
  const handleNodeClick = (id: number) => (e: ThreeEvent<MouseEvent>) => {
    dispatch(Msg_SelectNode(id));
    e.stopPropagation();
  };

  const staticNodes: JSX.Element[] = [];
  for (const node of viewModel.EnumerateUnselectedTreeNodes(state)) {
    const id = node.Person.Id;
    staticNodes.push(
      <TreeNodeMesh
        key={id}
        node={node}
        isSelected={false}
        onClick={handleNodeClick(id)}
        onPointerDown={handlePointerDown(id)}
      />
    );
  }

  const draggableNodes: JSX.Element[] = [];
  for (const node of viewModel.EnumerateSelectedTreeNodes(state)) {
    const id = node.Person.Id;
    draggableNodes.push(
      <TreeNodeMesh
        key={id}
        node={node}
        isSelected={true}
        onClick={handleNodeClick(id)}
        onPointerDown={handlePointerDown(id)}
      />
    );
  }

  const familyGroups: JSX.Element[] = [];
  for (const family of viewModel.EnumerateFamilies(state)) {
    const parents = viewModel.EnumerateParents(state, family);
    const parent1 = parents[0];
    const parent2 = parents[1];
    familyGroups.push(
      <FamilyGroup
        key={makeFamilyId(parent1, parent2)}
        parent1={parents[0]}
        parent2={parents[1]}
        children={viewModel.EnumerateChildren(state, family)}
      />
    );
  }

  const handleDrag = (l: THREE.Matrix4) => {
    const local = new THREE.Vector3();
    l.decompose(local, new THREE.Quaternion(), new THREE.Vector3());
    dispatch(Msg_DragTo(local.x, local.y, local.z));
  };

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
        <button onClick={() => dispatch(Msg_Undo())} disabled={!viewModel.CanUndo(state)}>
          Undo
        </button>
        <button
          onClick={() => dispatch(Msg_Redo())}
          disabled={!viewModel.CanRedo(state)}
          style={{ marginLeft: "8px" }}
        >
          Redo
        </button>
        <button
          style={{ marginLeft: "8px" }}
          onClick={() => {
            const nextMode = viewModel.IsSingleSelectEnabled(state)
              ? "multiSelect"
              : "singleSelect";
            dispatch(Msg_ToggleSelection(nextMode));
          }}
        >
          {viewModel.IsSingleSelectEnabled(state) ? "Multi-select" : "Single-select"}
        </button>
      </div>
      <div style={{ flex: 1, width: "100%", height: "100%" }}>
        <Canvas
          camera={{ position: [0, 0, 6], fov: 50 }}
          shadows
          onPointerMissed={() => dispatch(Msg_DeselectAll())}
        >
          {/* Ambient light for general illumination */}
          <ambientLight intensity={0.7} />
          {/* Directional light for stronger highlights and shadows */}
          <directionalLight position={[5, 5, 5]} intensity={1} castShadow />
          {/* Additional point light for more dynamic lighting */}
          <pointLight position={[1, -1, 2]} intensity={5} castShadow />
          <OrbitControls enabled={viewModel.ShouldEnableOrbitControls(state)} />
          <group position={[0, 1, 0]}>
            <DragControls
              autoTransform={false}
              axisLock="z"
              onDragStart={(origin) => dispatch(Msg_StartDrag(origin.x, origin.y, origin.z))}
              onDrag={handleDrag}
              onDragEnd={() => dispatch(Msg_EndDrag())}
            >
              {draggableNodes}
            </DragControls>
            {staticNodes}
            {familyGroups}
          </group>
        </Canvas>
      </div>
    </div>
  );
}
