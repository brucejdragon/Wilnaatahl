// App shell: src/App.tsx
import React from "react";
import TreeScene from "./TreeScene";
import { Initial_nodes, Initial_branches } from "./generated/ViewModel";

function App() {
  return (
    <div className="w-full h-screen">
      <TreeScene initialNodes={Initial_nodes} initialBranches={Initial_branches} />
    </div>
  );
}

export default App;
