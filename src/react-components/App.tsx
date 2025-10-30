import React, { useEffect, useMemo } from "react";
import { Canvas } from "@react-three/fiber";
import { useActions } from "koota/react";
import { GraphViewFactory } from "../generated/ViewModel";
import Toolbar from "./Toolbar";
import TreeScene from "./TreeScene";
import { eventActions, worldActions } from "../ecs";

export default function App() {
  const nodes = useMemo(() => {
    const factory = new GraphViewFactory();
    const graph = factory.LoadGraph();
    return factory.LayoutGraph(graph, factory.FirstWilp(graph));
  }, []);

  const { despawnAllTreeNodes, spawnTreeNode } = useActions(worldActions);
  const { handlePointerMissed } = useActions(eventActions);

  useEffect(() => {
    for (const node of nodes) {
      spawnTreeNode(node.Person, node.TargetPosition);
    }
    return despawnAllTreeNodes;
  }, [nodes, spawnTreeNode, despawnAllTreeNodes]);

  return (
    <div
      className="w-full h-screen"
      style={{
        width: "100vw",
        height: "100vh",
        display: "flex",
        flexDirection: "column",
        justifyContent: "center",
        alignItems: "center",
      }}
    >
      <Toolbar />
      <div style={{ flex: 1, width: "100%", height: "100%" }}>
        <Canvas
          camera={{ position: [0, 0, 6], fov: 50 }}
          shadows
          onPointerMissed={handlePointerMissed}
        >
          <TreeScene />
        </Canvas>
      </div>
    </div>
  );
}
