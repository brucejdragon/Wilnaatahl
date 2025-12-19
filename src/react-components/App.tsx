import React from "react";
import { Canvas } from "@react-three/fiber";
import { GraphViewFactory, Msg_DeselectAll, ViewModel } from "../generated/ViewModel/ViewModel";
import { ViewModelContext } from "../context/viewModelContext";
import TreeScene from "./TreeScene";
import Toolbar from "./Toolbar";

export default function App() {
  const [nodes, families] = React.useMemo(() => {
    const factory = new GraphViewFactory();
    const graph = factory.LoadGraph();
    const nodes = factory.LayoutGraph(graph, factory.FirstWilp(graph));
    const families = factory.ExtractFamilies(graph, nodes);
    return [nodes, families];
  }, []);

  const viewModel = new ViewModel();
  const [state, dispatch] = React.useReducer(
    viewModel.Update,
    [nodes, families],
    viewModel.CreateInitialViewState
  );

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
      <ViewModelContext value={{ viewModel, state, dispatch }}>
        <Toolbar />
        <div style={{ flex: 1, width: "100%", height: "100%" }}>
          <Canvas
            camera={{ position: [0, 0, 6], fov: 50 }}
            shadows
            onPointerMissed={() => dispatch(Msg_DeselectAll())}
          >
            <TreeScene />
          </Canvas>
        </div>
      </ViewModelContext>
    </div>
  );
}
