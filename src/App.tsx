// App shell: src/App.tsx
import React from "react";
import TreeScene from "./TreeScene";
import { GraphViewFactory } from "./generated/ViewModel";

function App() {
  const factory = new GraphViewFactory();
  const graph = factory.LoadGraph();
  const nodes = factory.LayoutGraph(graph, "H"); // TODO: Stop hardcoding Wilp
  const families = factory.ExtractFamilies(graph, nodes);
  return (
    <div className="w-full h-screen">
      <TreeScene initialNodes={nodes} initialFamilies={families} />
    </div>
  );
}

export default App;
