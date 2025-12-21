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
type WilpName =
    | WilpName of string

    member this.AsString =
        let (WilpName wilp) = this
        wilp

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
      Wilp: WilpName option
      Shape: NodeShape
      DateOfBirth: DateOnly option
      DateOfDeath: DateOnly option
      Generation: int (* Temporary hack for demo layout if needed *) }

    /// Used for situations where we need a prototypical instance of Person just to infer its type.
    static member Empty =
        { Id = PersonId 0
          Label = None
          Wilp = Some(WilpName "H") (* Temporary hack for demo *)
          Shape = Sphere
          DateOfBirth = None
          DateOfDeath = None
          Generation = 0 }

/// Represents a parent-child relationship. For every Person with recorded parents,
/// there will be two ParentChildRelationships, one for each parent.
type ParentChildRelationship = { Parent: PersonId; Child: PersonId }

/// Represents a pair of co-parents. If a child is missing one of the two recorded parents,
/// the missing parent is modeled as a Person with no extra non-identifying information.
type CoParentRelationship = { Mother: PersonId; Father: PersonId }

// A family tree centered around one Wilp, including coparents from outside that Wilp.
// If a Wilp has mutiple roots, then it will have more than one such tree.
type WilpTree =
    | Leaf of PersonId // Person with no descendants
    | Family of Family
// A Wilp member with one or more coparents and their descendant sub-trees.
and Family =
    { WilpParent: PersonId
      CoParentsAndDescendants: Map<PersonId, WilpTree seq> }

module FamilyGraph =

    type FamilyGraph =
        private
            { People: Map<int, Person>
              ParentChildRelationshipsByParent: Map<int, ParentChildRelationship list>
              CoParentRelationships: Set<CoParentRelationship>
              Huwilp: Set<WilpName>
              HuwilpForests: Map<WilpName, WilpTree seq> }

    let createFamilyGraph (peopleAndParents: seq<Person * CoParentRelationship option>) =
        let peopleMap =
            peopleAndParents |> Seq.map (fun (p, _) -> p.Id.AsInt, p) |> Map.ofSeq

        let coParents =
            peopleAndParents |> Seq.choose (fun (_, parents) -> parents) |> Set.ofSeq

        let parentChildMap =
            seq {
                for person, maybeParents in peopleAndParents do
                    match maybeParents with
                    | Some parents ->
                        yield { Parent = parents.Mother; Child = person.Id }
                        yield { Parent = parents.Father; Child = person.Id }
                    | None -> () // Person has no recorded parents so they are a "root" in the family multi-graph.
            }
            |> Seq.groupBy (fun rel -> rel.Parent.AsInt)
            |> Seq.map (fun (parent, children) -> parent, children |> List.ofSeq)
            |> Map.ofSeq

        let huwilp = peopleAndParents |> Seq.choose (fun (p, _) -> p.Wilp) |> Set.ofSeq

        // Helper to build WilpTree recursively using coparent relationships.
        let rec buildWilpTree person =
            // Find all coparent relationships where this person is a parent
            let coparentRels =
                coParents
                |> Set.filter (fun rel -> rel.Mother = person.Id || rel.Father = person.Id)
                |> Set.toList

            if List.isEmpty coparentRels then
                Leaf person.Id
            else
                // For each coparent, find all children for this pair, and build a forest (seq) of their WilpTrees
                let coParentsAndDescendants =
                    coparentRels
                    |> List.map (fun rel ->
                        let coparentId = if rel.Mother = person.Id then rel.Father else rel.Mother
                        // Find all children for this coparent pair
                        let children =
                            peopleAndParents
                            |> Seq.choose (fun (p, maybeParents) ->
                                match maybeParents with
                                | Some rel' when rel' = rel -> Some p
                                | _ -> None)
                            |> Seq.toList
                        // For each child, build their WilpTree
                        let descendantTrees = children |> List.map buildWilpTree |> Seq.ofList
                        // If no children, yield an empty sequence
                        coparentId, descendantTrees)
                    |> Map.ofList

                Family
                    { WilpParent = person.Id
                      CoParentsAndDescendants = coParentsAndDescendants }

        // For each Wilp, find root persons (with that Wilp and no parents).
        let huwilpForests =
            huwilp
            |> Seq.map (fun w ->
                let roots =
                    peopleAndParents
                    |> Seq.choose (fun (p, maybeParents) ->
                        match p.Wilp, maybeParents with
                        | Some w', None when w' = w -> Some p
                        | _ -> None)

                let trees = roots |> Seq.map buildWilpTree
                w, trees)
            |> Map.ofSeq

        { People = peopleMap
          ParentChildRelationshipsByParent = parentChildMap
          CoParentRelationships = coParents
          Huwilp = huwilp
          HuwilpForests = huwilpForests }

    let allPeople graph =
        graph.People |> Map.values :> Person seq

    let coparents graph = graph.CoParentRelationships

    let huwilp graph = graph.Huwilp

    let findPerson (PersonId personId) graph = graph.People |> Map.find personId

    let findChildren (PersonId personId) graph =
        match graph.ParentChildRelationshipsByParent |> Map.tryFind personId with
        | Some rels -> rels |> List.map (fun r -> r.Child) |> Set.ofList
        | None -> Set.empty

    /// Catamorphism for WilpTree forests by WilpName.
    /// visitWilpForest graph wilpName leaf family
    ///   - graph: FamilyGraph
    ///   - wilpName: WilpName to select the forest
    ///   - leaf: function to handle Leaf (PersonId -> 'R)
    ///   - family: function to handle Family (PersonId -> Map<PersonId, 'R seq> -> 'R)
    /// Returns: sequence of 'R, one for each root in the forest
    let visitWilpForest
        wilpName
        (leaf: PersonId -> FamilyGraph -> 'R)
        (family: PersonId -> Map<PersonId, 'R seq> -> FamilyGraph -> 'R)
        graph
        : seq<'R> =
        let rec visit tree =
            match tree with
            | Leaf pid -> leaf pid graph
            | Family fam ->
                let mapped = fam.CoParentsAndDescendants |> Map.map (fun _ ts -> Seq.map visit ts)
                family fam.WilpParent mapped graph

        match graph.HuwilpForests |> Map.tryFind wilpName with
        | Some forest -> Seq.map visit forest
        | None -> Seq.empty

module Initial =

    let peopleAndParents =
        [ { Person.Empty with Id = PersonId 0; Shape = Sphere }, None
          { Person.Empty with
              Id = PersonId 1
              Label = Some "GGGG Grandfather"
              Wilp = None
              Shape = Cube },
          None
          { Person.Empty with
              Id = PersonId 2
              Label = Some "GGG Grandmother" // Putting an underlined X̲ here for no particular reason...
              Shape = Sphere
              Generation = 1 },
          Some { Mother = PersonId 0; Father = PersonId 1 }
          { Person.Empty with
              Id = PersonId 3
              Label = Some "GGG Granduncle H"
              Shape = Cube
              Generation = 1 },
          Some { Mother = PersonId 0; Father = PersonId 1 }
          { Person.Empty with
              Id = PersonId 4
              Label = Some "GGG Granduncle N"
              Shape = Cube
              Generation = 1 },
          Some { Mother = PersonId 0; Father = PersonId 1 } ]
