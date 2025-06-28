// App shell: src/App.tsx
import React from "react";
import TreeScene from "./TreeScene";
import { Person, NodeShape } from "./model/Person";
import { Node } from "./viewmodel/FamilyTreeViewModel";

const people: Person[] = [
  { label: undefined, type: NodeShape.Sphere }, // root1
  { label: "GGGG Grandfather", type: NodeShape.Cube }, // root2
  { label: "GGG Grandmother", type: NodeShape.Sphere }, // child1
  { label: "GGG Granduncle H", type: NodeShape.Cube }, // child2
  { label: "GGG Granduncle N", type: NodeShape.Cube }, // child3
];

const nodes: Record<string, Node> = {
  root1: { id: "root1", position: [-1, 0, 0], children: [], person: people[0] },
  root2: { id: "root2", position: [1, 0, 0], children: [], person: people[1] },
  branch: { id: "branch", position: [0, -1, 0], children: ["child1", "child2", "child3"] },
  child1: { id: "child1", position: [-2, -2, 0], children: [], person: people[2] },
  child2: { id: "child2", position: [0, -2, 0], children: [], person: people[3] },
  child3: { id: "child3", position: [2, -2, 0], children: [], person: people[4] },
};

function App() {
  return (
    <div className="w-full h-screen">
      <TreeScene nodes={nodes} />
    </div>
  );
}

export default App;
