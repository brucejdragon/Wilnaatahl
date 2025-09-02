namespace Wilnaatahl.Model

open Fable.Core

[<StringEnum>]
type NodeShape =
    | Sphere
    | Cube

type Person =
    { label: string option
      shape: NodeShape }

module Initial =
    let people =
        [ { label = None; shape = Sphere }
          { label = Some "GGGG Grandfather"
            shape = Cube }
          { label = Some "GGG Grandmother"
            shape = Sphere }
          { label = Some "GGG Granduncle H"
            shape = Cube }
          { label = Some "GGG Granduncle N"
            shape = Cube } ]
