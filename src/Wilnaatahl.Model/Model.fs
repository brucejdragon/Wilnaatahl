namespace Wilnaatahl.Model

open System
#if FABLE_COMPILER
open Fable.Core
#endif

/// Represents a unique identifier for a person. This is the universal key into all
/// data structures in the application.
#if FABLE_COMPILER
[<Erase>]
#endif
type PersonId =
    | PersonId of int
    static member ToInt(PersonId personId) = personId

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
      Label: string option
      Shape: NodeShape
      DateOfBirth: DateOnly option
      DateOfDeath: DateOnly option }

/// Represents a parent-child relationship. For every Person with recorded parents,
/// there will be two ParentChildRelationships, one for each parent.
type ParentChildRelationship = { Parent: PersonId; Child: PersonId }

/// Represents a pair of co-parents. If a child is missing one of the two recorded parents,
/// the missing parent is modeled as a Person with no extra non-identifying information.
type CoParentRelationship = { Mother: PersonId; Father: PersonId }

/// This is a handy data structure for rendering the connectors between members of an
/// immediate family. It's here for convenience despite being useful primarily in the ViewModel
/// and View layers, since it's produced by FamilyGraph.
type Family =
    { Parents: PersonId * PersonId
      Children: PersonId list }

module FamilyGraph =
    type FamilyGraph =
        private
            { People: Map<int, Person>
              ParentChildRelationshipsByParent: Map<int, ParentChildRelationship list>
              CoParentRelationships: Set<CoParentRelationship> }

    let createFamilyGraph (peopleAndParents: seq<Person * CoParentRelationship option>) =
        let peopleMap =
            peopleAndParents
            |> Seq.map (fun (p, _) -> PersonId.ToInt p.Id, p)
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
            |> Seq.groupBy (fun rel -> PersonId.ToInt rel.Parent)
            |> Seq.map (fun (parent, children) -> parent, children |> List.ofSeq)
            |> Map.ofSeq

        { People = peopleMap
          ParentChildRelationshipsByParent = parentChildMap
          CoParentRelationships = coParents }

    let findPerson (PersonId personId) graph = graph.People |> Map.find personId

    let findChildren (PersonId personId) graph =
        match graph.ParentChildRelationshipsByParent
              |> Map.tryFind personId
            with
        | Some rels -> rels |> List.map (fun r -> r.Child) |> Set.ofList
        | None -> Set.empty

    let createFamilies graph =
        seq {
            for rel in graph.CoParentRelationships do
                let childrenOfMother = findChildren rel.Mother graph
                let childrenOfFather = findChildren rel.Father graph

                yield
                    { Parents = rel.Mother, rel.Father
                      Children =
                        Set.intersect childrenOfMother childrenOfFather
                        |> Set.toList }
        }

module Initial =
    let private peopleAndParents =
        [ { Id = PersonId 0
            Label = None
            Shape = Sphere
            DateOfBirth = None
            DateOfDeath = None },
          None
          { Id = PersonId 1
            Label = Some "GGGG Grandfather"
            Shape = Cube
            DateOfBirth = None
            DateOfDeath = None },
          None
          { Id = PersonId 2
            Label = Some "GGG Grandmother" // Putting an underlined X̲ here for no particular reason...
            Shape = Sphere
            DateOfBirth = None
            DateOfDeath = None },
          Some
              { Mother = PersonId 0
                Father = PersonId 1 }
          { Id = PersonId 3
            Label = Some "GGG Granduncle H"
            Shape = Cube
            DateOfBirth = None
            DateOfDeath = None },
          Some
              { Mother = PersonId 0
                Father = PersonId 1 }
          { Id = PersonId 4
            Label = Some "GGG Granduncle N"
            Shape = Cube
            DateOfBirth = None
            DateOfDeath = None },
          Some
              { Mother = PersonId 0
                Father = PersonId 1 } ]

    let familyGraph = FamilyGraph.createFamilyGraph peopleAndParents
