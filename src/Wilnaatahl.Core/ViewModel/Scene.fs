namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model
open Wilnaatahl.Model.FamilyGraph

/// Represents information needed to render a family member in the context of their Wilp.
type IFamilyMemberInfo =
    abstract Person: Person
    abstract RenderedInWilp: WilpName

/// This is a handy data structure for creating the connectors between members of an
/// immediate family.
type Family<'T when 'T :> IFamilyMemberInfo> =
    // NOTE: Because this indirectly tracks EntityIds, it must be ephemeral and not persist between frames.
    // This type is only meant to be used while initializing connectors.
    {
        Parents: 'T * 'T
        Children: 'T list
    }

module Scene =

    /// Produces a map from WilpName to the people that will render along with that Wilp. Since people can
    /// play roles in different huwilp, the same Person may appear under different WilpName keys in the result.
    let enumerateHuwilpToRender familyGraph =
        // TODO: Extend to support multilple huwilp.
        let wilp = familyGraph |> huwilp |> Seq.head // ASSUMPTION: At least one Wilp is represented in the input data.
        let people = familyGraph |> allPeople
        [ (wilp, people) ] |> Map.ofList

    /// Organizes the given sequence of family member info into a data structure useful in spawning
    /// connectors between immediate family members in the tree scene.
    let extractFamilies<'T when 'T :> IFamilyMemberInfo> familyGraph (nodes: 'T seq) =
        // TODO: Make this a recursive depth-first traversal so that leaf-most families are returned before root-most.
        // It needs to produce one tree traversal per Wilp and return the Wilp info with each Family.
        let nodesByPersonInWilp =
            nodes
            |> Seq.map (fun f -> (f.Person.Id.AsInt, f.RenderedInWilp), f)
            |> Map.ofSeq

        // Each Person appears at most once in a rendered Wilp, so this mapping is guaranteed to be unique.
        let personIdToNodeId wilp (personId: PersonId) =
            nodesByPersonInWilp |> Map.tryFind (personId.AsInt, wilp)

        let huwilpToRender = nodes |> Seq.map _.RenderedInWilp |> Seq.distinct

        seq {
            for rel in coparents familyGraph do
                let childrenOfMother = findChildren rel.Mother familyGraph
                let childrenOfFather = findChildren rel.Father familyGraph
                let childrenOfBoth = Set.intersect childrenOfMother childrenOfFather |> Set.toList

                for wilp in huwilpToRender do
                    let mapId = personIdToNodeId wilp

                    match mapId rel.Mother, mapId rel.Father, childrenOfBoth |> List.choose mapId with
                    | Some motherId, Some fatherId, (_ :: _ as childrenIds) ->
                        yield { Parents = motherId, fatherId; Children = childrenIds }
                    | _ -> () // Nothing to render since we need both parents and at least one child.
        }
