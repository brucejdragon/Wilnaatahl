// App shell: src/App.tsx
import React from "react";
import TreeScene from "./TreeScene";
import { Initial_state } from "./generated/ViewModel";

function App() {
  return (
    <div className="w-full h-screen">
      <TreeScene initialNodes={Initial_state.nodes} initialBranches={Initial_state.branches} />
    </div>
  );
}

export default App;
