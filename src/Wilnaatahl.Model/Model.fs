namespace Wilnaatahl.Model

type NodeShape =
    | Sphere
    | Cube

type Person = {
    label: string option
    shape: NodeShape
}