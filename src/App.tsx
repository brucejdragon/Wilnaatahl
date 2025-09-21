// App shell: src/App.tsx
import React from "react";
import TreeScene from "./TreeScene";
import { Initial_nodes, Initial_families } from "./generated/ViewModel";

function App() {
  return (
    <div className="w-full h-screen">
      <TreeScene initialNodes={Initial_nodes} initialFamilies={Initial_families} />
    </div>
  );
}

export default App;
