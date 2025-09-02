// App shell: src/App.tsx
import React from "react";
import TreeScene from "./TreeScene";
import { Initial_state } from "./generated/ViewModel";

function mapToObject(map: any) {
  const obj: Record<string, any> = {};
  for (const [k, v] of map) {
    obj[k] = v;
  }
  return obj;
}

function App() {
  // Convert F# Map to JS object for TreeScene
  const nodes = mapToObject(Initial_state.nodes);
  return (
    <div className="w-full h-screen">
      <TreeScene initialNodes={nodes} />
    </div>
  );
}

export default App;
