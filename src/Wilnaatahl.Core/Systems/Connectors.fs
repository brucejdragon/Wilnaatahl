module Wilnaatahl.Systems.Connectors

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity
open Wilnaatahl.ECS.Extensions
open Wilnaatahl.ECS.Relation
open Wilnaatahl.ECS.TraitExtensions
open Wilnaatahl.Model
open Wilnaatahl.Model.FamilyGraph
open Wilnaatahl.Systems.Traits

/// Holds trait data for a node representing a person in the family tree.
type private FamilyNode =
    // NOTE: Because this tracks an EntityId, it must be ephemeral and not persist between frames.
    // This type is only meant to be used while initializing connectors.
    { Entity: EntityId
      Person: Person
      Position: {| x: float; y: float; z: float |}
      RenderedInWilp: WilpName }

/// This is a handy data structure for creating the connectors between members of an
/// immediate family.
type private Family =
    // NOTE: Because this indirectly tracks EntityIds, it must be ephemeral and not persist between frames.
    // This type is only meant to be used while initializing connectors.
    { Parents: FamilyNode * FamilyNode
      Children: FamilyNode list }

let private extractFamilies familyGraph (world: IWorld) =
    let createFamilyNode ((person, position), entity) =
        let maybeWilpId = entity |> targetFor RenderedIn

        match maybeWilpId with
        | Some wilpId ->
            match wilpId |> get Wilp with
            | Some wilp ->
                { Entity = entity
                  Person = person
                  Position = position
                  RenderedInWilp = WilpName wilp.wilpName }
            | None -> failwith $"Found Wilp {wilpId} without a name."
        | None -> failwith $"Found tree node {entity} with no Wilp."

    let nodes =
        world.QueryTraits(PersonRef, Position, With(RenderedIn.Wildcard())).ToSequence()
        |> Seq.map createFamilyNode

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

let destroyAllConnectors (world: IWorld) =
    for entity in world.Query(With Connector) do
        entity |> destroy

let spawnAllConnectors (world: IWorld) familyGraph =
    let families = world |> extractFamilies familyGraph

    for family in families do
        // There are many components that go into a family's connectors. Let's create them
        // one-by-one:
        //
        // 1. A Hidden Line that Follows the two co-parent nodes.
        let parent1, parent2 = family.Parents

        // The first entity represents the line itself.
        let hiddenLineId =
            world
            |> Line3.spawnHidden parent1.Position parent2.Position
            |> Line3.snapTo world parent1.Entity parent2.Entity

        // 2. Two Lines, each of which Parallels the Hidden line, one with offset 0.1
        //    and one with offset -0.1.
        let topLineId = world |> Line3.spawn parent1.Position parent2.Position
        let bottomLineId = world |> Line3.spawn parent1.Position parent2.Position
        topLineId |> addWith (Parallels => hiddenLineId) {| offset = 0.1 |}
        bottomLineId |> addWith (Parallels => hiddenLineId) {| offset = -0.1 |}

        // 3. A Hidden entity with Position that Bisects the "bottom" line, which is not
        //    always literally below the other line depending on how the parent nodes have been
        //    dragged. Our definition of "bottom" is that it's the Line that Parallels the
        //    Hidden Line with a negative offset.
        let bisectingEntityId =
            world.Spawn(Position.Val Line3.zeroPosition, Hidden.Tag(), Connector.Tag())

        bisectingEntityId |> add (Bisects => bottomLineId)

        // 4. A Hidden Bounding Box that includes all child nodes.
        // The margins are chosen based on what looks good.
        let boundingBoxId, _, boxBoundId =
            world |> BoundingBox.spawn {| x = 0.6; y = 0.65; z = 0 |}

        // We'll add the children to the bounding box later, so we can do all
        // child processing in one loop.

        // 5. A visible Elbow that FollowsX the Bisects Node and FollowsY the Bounding Box.
        let branchNodeId =
            world.Spawn(Position.Val Line3.zeroPosition, Elbow.Tag(), Connector.Tag())

        branchNodeId |> addWith (SnapToX => bisectingEntityId) {| x = 0.0 |}
        branchNodeId |> addWith (SnapToY => boxBoundId) {| y = 0.0 |}

        // 6. A visible Line that follows the Bisects Node and the Branch Node
        world
        |> Line3.spawnDynamic
        |> Line3.snapTo world bisectingEntityId branchNodeId
        |> ignore

        for child in family.Children do
            // Finish step 4 above by adding each child to the bounding box.
            boundingBoxId |> add (BoundingBoxOn => child.Entity)

            // 7. A visible Elbow for each child node that FollowsX the corresponding child node
            //    and FollowsY the Bounding Box.
            let junctionId =
                world.Spawn(Position.Val Line3.zeroPosition, Elbow.Tag(), Connector.Tag())

            junctionId |> addWith (SnapToX => child.Entity) {| x = 0.0 |}
            junctionId |> addWith (SnapToY => boxBoundId) {| y = 0.0 |}

            // 8. A visible Line for each child that follows the Branch Node and that child's Junction Node
            world
            |> Line3.spawnDynamic
            |> Line3.snapTo world junctionId branchNodeId
            |> ignore

            // 9. A visible Line for each child that follows that child's Junction Node and the Child Node itself.
            world
            |> Line3.spawnDynamic
            |> Line3.snapTo world junctionId child.Entity
            |> ignore
