// App shell: src/App.tsx
import React from "react";
import TreeScene from "./TreeScene";
import { GraphViewFactory } from "./generated/ViewModel";

function App() {
  const factory = new GraphViewFactory();
  const graph = factory.LoadGraph();
  const families = factory.ExtractFamilies(graph);
  const nodes = factory.LayoutTreeNodes(graph);
  return (
    <div className="w-full h-screen">
      <TreeScene initialNodes={nodes} initialFamilies={families} />
    </div>
  );
}

export default App;
