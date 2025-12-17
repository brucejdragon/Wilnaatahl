import React, { useEffect, useMemo } from "react";
import { Canvas } from "@react-three/fiber";
import { useActions } from "koota/react";
import { GraphViewFactory } from "../generated/ViewModel";
import Toolbar from "./Toolbar";
import TreeScene from "./TreeScene";
import { eventActions, worldActions } from "../ecs";

export default function App() {
  const [familyGraph, nodes, firstWilp] = useMemo(() => {
    const factory = new GraphViewFactory();
    const graph = factory.LoadGraph();
    return [graph, factory.LayoutGraph(graph), factory.FirstWilp(graph)];
  }, []);

  const {
    destroyAllConnectors,
    destroyAllTreeNodes,
    spawnAllConnectors,
    spawnTreeNode,
    spawnWilpBox,
  } = useActions(worldActions);

  useEffect(() => {
    // TODO: Spawn multiple huwilp once we support that.
    const wilpId = spawnWilpBox(firstWilp);

    // Spawn the tree nodes before connectors so the connectors can connect to them.
    for (const node of nodes) {
      spawnTreeNode(node.Person, node.TargetPosition, wilpId);
    }
    spawnAllConnectors(familyGraph);
    return () => {
      // Destroy the connectors before the tree nodes (it shouldn't matter, but just for symmetry).
      destroyAllConnectors();
      destroyAllTreeNodes();
    };
  }, [
    familyGraph,
    firstWilp,
    nodes,
    destroyAllConnectors,
    destroyAllTreeNodes,
    spawnAllConnectors,
    spawnTreeNode,
    spawnWilpBox,
  ]);

  const { handlePointerMissed } = useActions(eventActions);
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
          camera={{ position: [0, 0, 8], fov: 50 }}
          shadows
          onPointerMissed={handlePointerMissed}
        >
          <TreeScene />
        </Canvas>
      </div>
    </div>
  );
}
