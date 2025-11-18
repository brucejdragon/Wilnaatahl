module Wilnaatahl.Systems.Connectors

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity
open Wilnaatahl.ECS.Extensions
open Wilnaatahl.ECS.Trait
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
      RenderedInWilp: Wilp }

/// This is a handy data structure for creating the connectors between members of an
/// immediate family.
type private Family =
    // NOTE: Because this indirectly tracks EntityIds, it must be ephemeral and not persist between frames.
    // This type is only meant to be used while initializing connectors.
    { Parents: FamilyNode * FamilyNode
      Children: FamilyNode list }

/// Used to mark connector entities for ease of cleanup later.
let private Connector = tagTrait ()

let private extractFamilies familyGraph (world: IWorld) =
    let nodes =
        world.QueryTraits3(PersonRef, Position, RenderedInWilp).Map
        <| fun ((person, position, wilpTrait), entity) ->
            { Entity = entity
              Person = person
              Position = position
              RenderedInWilp = Wilp wilpTrait.wilp }

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

// NOTE: Many connectors have initial position at the origin for convenience. Those positions
// will be dynamically updated the first time the Movement system runs.
let private zeroPosition = {| x = 0.0; y = 0.0; z = 0.0 |}

let private spawnLine firstPos secondPos (world: IWorld) =
    let firstEndpoint = world.Spawn(Position.Val firstPos, Connector.Tag())
    let secondEndpoint = world.Spawn(Position.Val secondPos, Connector.Tag())
    firstEndpoint |> add (LineTo.On secondEndpoint)
    firstEndpoint, secondEndpoint

let private spawnDynamicLine (world: IWorld) =
    world |> spawnLine zeroPosition zeroPosition

let private spawnHiddenLine firstPos secondPos (world: IWorld) =
    let lineId, lineEndId = world |> spawnLine firstPos secondPos
    lineId |> add Hidden
    lineEndId |> add Hidden
    lineId, lineEndId

let private follows targetId (x, y, z) subjectId =
    subjectId |> addWith (FollowsX.On targetId) {| x = x |}
    subjectId |> addWith (FollowsY.On targetId) {| y = y |}
    subjectId |> addWith (FollowsZ.On targetId) {| z = z |}

let private snapTo firstPos secondPos (lineId, lineEndId) =
    let zeroDistance = 0.0, 0.0, 0.0
    lineId |> follows firstPos zeroDistance
    lineEndId |> follows secondPos zeroDistance
    lineId, lineEndId

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
        let hiddenLineId, _ =
            world
            |> spawnHiddenLine parent1.Position parent2.Position
            |> snapTo parent1.Entity parent2.Entity

        // 2. Two Lines, each of which Parallels the Hidden line, one with offset 0.1
        //    and one with offset -0.1.
        let topLineId, _ = world |> spawnLine parent1.Position parent2.Position
        let bottomLineId, _ = world |> spawnLine parent1.Position parent2.Position
        topLineId |> addWith (Parallels.On hiddenLineId) {| offset = 0.1 |}
        bottomLineId |> addWith (Parallels.On hiddenLineId) {| offset = -0.1 |}

        // 3. A Hidden entity with Position that Bisects the "bottom" line, which is not
        //    always literally below the other line depending on how the parent nodes have been
        //    dragged. Our definition of "bottom" is that it's the Line that Parallels the
        //    Hidden Line with a negative offset.
        let bisectingEntityId =
            world.Spawn(Position.Val zeroPosition, Hidden.Tag(), Connector.Tag())

        bisectingEntityId |> add (Bisects.On bottomLineId)

        // 4. A Hidden Bounding Box that includes all child nodes.
        // The Margins are chosen based on what looks good.
        let boundingBoxId =
            world.Spawn(
                Position.Val zeroPosition,
                Bounds.Val zeroPosition,
                Margin.Val {| x = 0.6; y = 0.65; z = 0 |},
                Hidden.Tag(),
                Connector.Tag()
            )

        // We'll add the children to the bounding box later, so we can do all
        // child processing in one loop.

        // 5. A visible Elbow that FollowsX the Bisects Node and FollowsY the Bounding Box.
        let branchNodeId =
            world.Spawn(Position.Val zeroPosition, Elbow.Tag(), Connector.Tag())

        branchNodeId |> addWith (FollowsX.On bisectingEntityId) {| x = 0.0 |}
        branchNodeId |> addWith (FollowsY.On boundingBoxId) {| y = 0.0 |}

        // 6. A visible Line that follows the Bisects Node and the Branch Node
        world |> spawnDynamicLine |> snapTo bisectingEntityId branchNodeId |> ignore

        for child in family.Children do
            // Finish step 4 above by adding each child to the bounding box.
            child.Entity |> add (BoundedBy.On boundingBoxId)

            // 7. A visible Elbow for each child node that FollowsX the corresponding child node
            //    and FollowsY the Bounding Box.
            let junctionId =
                world.Spawn(Position.Val zeroPosition, Elbow.Tag(), Connector.Tag())

            junctionId |> addWith (FollowsX.On child.Entity) {| x = 0.0 |}
            junctionId |> addWith (FollowsY.On boundingBoxId) {| y = 0.0 |}

            // 8. A visible Line for each child that follows the Branch Node and that child's Junction Node
            world |> spawnDynamicLine |> snapTo junctionId branchNodeId |> ignore

            // 9. A visible Line for each child that follows that child's Junction Node and the Child Node itself.
            world |> spawnDynamicLine |> snapTo junctionId child.Entity |> ignore
