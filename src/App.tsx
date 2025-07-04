// App shell: src/App.tsx
import React from "react";
import TreeScene from "./TreeScene";
import { Person, NodeShape_Sphere, NodeShape_Cube } from "./generated/Model";
import { Node$ } from "./generated/ViewModel";
import { empty, ofArray } from "./generated/fable_modules/fable-library-ts.4.25.0/List.js";

const people: Person[] = [
  new Person(undefined, NodeShape_Sphere()), // root1
  new Person("GGGG Grandfather", NodeShape_Cube()), // root2
  new Person("GGG Grandmother", NodeShape_Sphere()), // child1
  new Person("GGG Granduncle H", NodeShape_Cube()), // child2
  new Person("GGG Granduncle N", NodeShape_Cube()), // child3
];

const nodes: Record<string, Node$> = {
  root1: new Node$("root1", [-1, 0, 0], empty<string>(), people[0]),
  root2: new Node$("root2", [1, 0, 0], empty<string>(), people[1]),
  branch: new Node$("branch", [0, -1, 0], ofArray(["child1", "child2", "child3"]), undefined),
  child1: new Node$("child1", [-2, -2, 0], empty<string>(), people[2]),
  child2: new Node$("child2", [0, -2, 0], empty<string>(), people[3]),
  child3: new Node$("child3", [2, -2, 0], empty<string>(), people[4])
};

function App() {
  return (
    <div className="w-full h-screen">
      <TreeScene nodes={nodes} />
    </div>
  );
}

export default App;
