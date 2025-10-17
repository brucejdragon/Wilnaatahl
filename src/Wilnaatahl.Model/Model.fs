namespace Wilnaatahl.Model

open System
#if FABLE_COMPILER
open Fable.Core
#endif

/// Represents a unique identifier for a person.
#if FABLE_COMPILER
[<Erase>]
#endif
type PersonId =
    | PersonId of int
    member this.AsInt =
        let (PersonId personId) = this
        personId

/// Represents a Wilp; Strongly typed to distinguish a Wilp name from other strings.
#if FABLE_COMPILER
[<Erase>]
#endif
type Wilp =
    | Wilp of string
    member this.Name =
        let (Wilp name) = this
        name

/// Stand-in for Gender until we decide how best to handle it.
#if FABLE_COMPILER
[<StringEnum>]
#endif
type NodeShape =
    | Sphere
    | Cube

/// Everything we know about a person in the family tree.
type Person =
    { Id: PersonId
      Label: string option // TODO: Commit to schema for names (colonial vs. traditional)
      Wilp: Wilp option
      Shape: NodeShape
      DateOfBirth: DateOnly option
      DateOfDeath: DateOnly option }

/// Represents a parent-child relationship. For every Person with recorded parents,
/// there will be two ParentChildRelationships, one for each parent.
type ParentChildRelationship = { Parent: PersonId; Child: PersonId }

/// Represents a pair of co-parents. If a child is missing one of the two recorded parents,
/// the missing parent is modeled as a Person with no extra non-identifying information.
type CoParentRelationship = { Mother: PersonId; Father: PersonId }

module FamilyGraph =
    type FamilyGraph =
        private
            { People: Map<int, Person>
              ParentChildRelationshipsByParent: Map<int, ParentChildRelationship list>
              CoParentRelationships: Set<CoParentRelationship>
              Huwilp: Set<Wilp> }

    let createFamilyGraph (peopleAndParents: seq<Person * CoParentRelationship option>) =
        let peopleMap =
            peopleAndParents
            |> Seq.map (fun (p, _) -> p.Id.AsInt, p)
            |> Map.ofSeq

        let coParents =
            peopleAndParents
            |> Seq.choose (fun (_, parents) -> parents)
            |> Set.ofSeq

        let parentChildMap =
            seq {
                for person, maybeParents in peopleAndParents do
                    match maybeParents with
                    | Some parents ->
                        yield
                            { Parent = parents.Mother
                              Child = person.Id }

                        yield
                            { Parent = parents.Father
                              Child = person.Id }
                    | None -> () // Person has no recorded parents so they are a "root" in the family multi-graph.
            }
            |> Seq.groupBy (fun rel -> rel.Parent.AsInt)
            |> Seq.map (fun (parent, children) -> parent, children |> List.ofSeq)
            |> Map.ofSeq

        let huwilp =
            peopleAndParents
            |> Seq.choose (fun (p, _) -> p.Wilp)
            |> Set.ofSeq

        { People = peopleMap
          ParentChildRelationshipsByParent = parentChildMap
          CoParentRelationships = coParents
          Huwilp = huwilp }

    let coparents graph = graph.CoParentRelationships

    let huwilp graph = graph.Huwilp

    let findPerson (PersonId personId) graph = graph.People |> Map.find personId

    let findChildren (PersonId personId) graph =
        match graph.ParentChildRelationshipsByParent
              |> Map.tryFind personId
            with
        | Some rels -> rels |> List.map (fun r -> r.Child) |> Set.ofList
        | None -> Set.empty

module Initial =
    let peopleAndParents =
        [ { Id = PersonId 0
            Label = None
            Wilp = Some(Wilp "H")
            Shape = Sphere
            DateOfBirth = None
            DateOfDeath = None },
          None
          { Id = PersonId 1
            Label = Some "GGGG Grandfather"
            Wilp = None
            Shape = Cube
            DateOfBirth = None
            DateOfDeath = None },
          None
          { Id = PersonId 2
            Label = Some "GGG Grandmother" // Putting an underlined X̲ here for no particular reason...
            Wilp = Some(Wilp "H")
            Shape = Sphere
            DateOfBirth = None
            DateOfDeath = None },
          Some
              { Mother = PersonId 0
                Father = PersonId 1 }
          { Id = PersonId 3
            Label = Some "GGG Granduncle H"
            Wilp = Some(Wilp "H")
            Shape = Cube
            DateOfBirth = None
            DateOfDeath = None },
          Some
              { Mother = PersonId 0
                Father = PersonId 1 }
          { Id = PersonId 4
            Label = Some "GGG Granduncle N"
            Wilp = Some(Wilp "H")
            Shape = Cube
            DateOfBirth = None
            DateOfDeath = None },
          Some
              { Mother = PersonId 0
                Father = PersonId 1 } ]
