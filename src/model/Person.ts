export enum NodeShape {
  Sphere = "sphere",
  Cube = "cube",
}

export type Person = {
  label?: string;
  type: NodeShape;
};