namespace Wilnaatahl.Model

open System
open Fable.Core

[<StringEnum>]
type NodeShape =
    | Sphere
    | Cube

type Person =
    { label: string option
      shape: NodeShape
      mother: Person option
      father: Person option
      dateOfBirth: DateOnly option
      dateOfDeath: DateOnly option }

module Initial =
    let private maternalAncestor =
        { label = None
          shape = Sphere
          mother = None
          father = None
          dateOfBirth = None
          dateOfDeath = None }

    let private paternalAncestor =
        { label = Some "GGGG Grandfather"
          shape = Cube
          mother = None
          father = None
          dateOfBirth = None
          dateOfDeath = None }

    let people =
        [ maternalAncestor
          paternalAncestor
          { label = Some "GGG Grandmother"
            shape = Sphere
            mother = Some maternalAncestor
            father = Some paternalAncestor
            dateOfBirth = None
            dateOfDeath = None }
          { label = Some "GGG Granduncle H"
            shape = Cube
            mother = Some maternalAncestor
            father = Some paternalAncestor
            dateOfBirth = None
            dateOfDeath = None }
          { label = Some "GGG Granduncle N"
            shape = Cube
            mother = Some maternalAncestor
            father = Some paternalAncestor
            dateOfBirth = None
            dateOfDeath = None } ]
