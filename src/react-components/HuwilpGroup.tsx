import React, { JSX } from "react";
import { Matrix4, Quaternion, Vector3 } from "three";
import { ThreeEvent } from "@react-three/fiber";
import { DragControls } from "@react-three/drei";
import { TreeNode } from "../generated/ViewModel/NodeState";
import {
  Msg_SelectNode,
  Msg_TouchNode,
  Msg_StartDrag,
  Msg_DragBy,
  Msg_EndDrag,
} from "../generated/ViewModel/ViewModel";
import { useViewModel } from "../context/viewModelContext";
import { TreeNodeMesh } from "./TreeNodeMesh";
import { LineMesh } from "./LineMesh";
import { ElbowSphereMesh } from "./ElbowSphereMesh";

function makeFamilyId(parent1: TreeNode, parent2: TreeNode): string {
  return `${parent1.Id}-${parent2.Id}-family`;
}

function ChildrenGroup({
  familyId,
  position, // Location of the top of vertical connector that goes down to the children.
  children,
}: {
  familyId: string;
  position: Vector3;
  children: Iterable<TreeNode>;
}) {
  const childList = Array.from(children);

  // Compute highest child Y (or fall back to the connector position Y).
  const highestChildY =
    childList.length > 0 ? Math.max(...childList.map((c) => c.Position[1])) : position.y;

  const branchPos = new Vector3(position.x, highestChildY + 0.65, position.z);

  return (
    <group>
      <LineMesh key={`${familyId}-vertical-to-branch`} from={position} to={branchPos} />
      <ElbowSphereMesh key={`${familyId}-junction-sphere`} position={branchPos} />
      {childList.flatMap((child) => {
        const childPos = new Vector3(...child.Position);
        const junction = new Vector3(childPos.x, branchPos.y, childPos.z);

        return [
          <LineMesh key={`branch-to-${child.Id}-junction`} from={branchPos} to={junction} />,
          <ElbowSphereMesh key={`${child.Id}-junction-sphere`} position={junction} />,
          <LineMesh key={`junction-to-${child.Id}`} from={junction} to={childPos} />,
        ];
      })}
    </group>
  );
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
  // Figure out which parent is leftmost because this determines which
  // parent connector is "top" and which is "bottom".
  const [leftParent, rightParent] =
    parent1.Position[0] < parent2.Position[0] ? [parent1, parent2] : [parent2, parent1];
  const p1 = new Vector3(...leftParent.Position);
  const p2 = new Vector3(...rightParent.Position);
  const dir = p2.clone().sub(p1).normalize();

  // Find a vector perpendicular to dir in the XY plane
  const perp = new Vector3(-dir.y, dir.x, 0).normalize();

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
  const verticalConnectorStart = new Vector3().lerpVectors(parent1Bottom, parent2Bottom, 0.5);
  const familyId = makeFamilyId(parent1, parent2);
  const parent1Id = parent1.Id;
  const parent2Id = parent2.Id;

  return (
    <group>
      <LineMesh
        key={`parent-${parent1Id}-${parent2Id}-connector-top`}
        from={parent1Top}
        to={parent2Top}
      />
      <LineMesh
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

export function HuwilpGroup() {
  const { viewModel, state, dispatch } = useViewModel();

  const handlePointerDown = (id: number) => (e: ThreeEvent<PointerEvent>) => {
    dispatch(Msg_TouchNode(id));
  };
  const handleNodeClick = (id: number) => (e: ThreeEvent<MouseEvent>) => {
    dispatch(Msg_SelectNode(id));
    e.stopPropagation();
  };

  const staticNodes: JSX.Element[] = [];
  for (const node of viewModel.EnumerateUnselectedTreeNodes(state)) {
    const id = node.Id;
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
    const id = node.Id;
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

  const handleDrag = (l: Matrix4) => {
    const local = new Vector3();
    l.decompose(local, new Quaternion(), new Vector3());
    dispatch(Msg_DragBy(local.x, local.y, local.z));
  };

  // We have two groups below to control the positioning of the wilp in the scene.
  // The outer group defines a central vertical axis that should be shared by all huwilp,
  // and sets the rotation of this wilp around that axis. The inner group sets the position
  // of the wilp relative to that axis.
  return (
    <group rotation-y={0}>
      <group position={[0, 1, 0]}>
        <DragControls
          autoTransform={false}
          axisLock="z"
          onDragStart={() => dispatch(Msg_StartDrag())} // Ignore origin from DragControls since it always seems to be (0, 0, 0).
          onDrag={handleDrag}
          onDragEnd={() => dispatch(Msg_EndDrag())}
        >
          {draggableNodes}
        </DragControls>
        {staticNodes}
        {familyGroups}
      </group>
    </group>
  );
}
