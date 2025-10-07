namespace Wilnaatahl.Model

open System
#if FABLE_COMPILER
open Fable.Core
#endif

#if FABLE_COMPILER
[<StringEnum>]
#endif
type NodeShape =
    | Sphere
    | Cube

type Person =
    { Label: string option
      Shape: NodeShape
      Mother: Person option
      Father: Person option
      DateOfBirth: DateOnly option
      DateOfDeath: DateOnly option }

module Initial =
    let private maternalAncestor =
        { Label = None
          Shape = Sphere
          Mother = None
          Father = None
          DateOfBirth = None
          DateOfDeath = None }

    let private paternalAncestor =
        { Label = Some "GGGG Grandfather"
          Shape = Cube
          Mother = None
          Father = None
          DateOfBirth = None
          DateOfDeath = None }

    let people =
        [ maternalAncestor
          paternalAncestor
          { Label = Some "GGG Grandmother" // Putting an underlined X̲ here for no particular reason...
            Shape = Sphere
            Mother = Some maternalAncestor
            Father = Some paternalAncestor
            DateOfBirth = None
            DateOfDeath = None }
          { Label = Some "GGG Granduncle H"
            Shape = Cube
            Mother = Some maternalAncestor
            Father = Some paternalAncestor
            DateOfBirth = None
            DateOfDeath = None }
          { Label = Some "GGG Granduncle N"
            Shape = Cube
            Mother = Some maternalAncestor
            Father = Some paternalAncestor
            DateOfBirth = None
            DateOfDeath = None } ]
