import React from "react";
import { DragControls } from "@react-three/drei";
import { Entity, Not } from "koota";
import { useActions, useQuery } from "koota/react";
import { eventActions, PersonRef, Selected } from "../ecs";
import { TreeNodeMesh } from "./TreeNodeMesh";

export function WilpGroup() {
  const staticEntities = useQuery(PersonRef, Not(Selected));
  const draggableEntities = useQuery(PersonRef, Selected);
  const { handleDrag, handleDragEnd, handleDragStart } = useActions(eventActions);

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
          onDrag={handleDrag}
          onDragEnd={handleDragEnd}
          onDragStart={handleDragStart}
        >
          {draggableEntities.map((entity: Entity) => (
            <TreeNodeMesh entity={entity} key={entity.id()} />
          ))}
        </DragControls>
        {staticEntities.map((entity: Entity) => (
          <TreeNodeMesh entity={entity} key={entity.id()} />
        ))}
      </group>
    </group>
  );
}
