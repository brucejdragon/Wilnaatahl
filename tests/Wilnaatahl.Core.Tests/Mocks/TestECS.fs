namespace Wilnaatahl.Core.Tests.Mocks

open System.Collections
open System.Collections.Generic
open Wilnaatahl.ECS

type private TestTagTrait() =
    interface ITagTrait with
        member _.IsTag = true

type private TestValueTrait<'T, 'TMutable>() =
    interface IMutableValueTrait<'T, 'TMutable> with
        member _.IsTag = false

type private TestRelation<'TTrait when 'TTrait :> ITrait>(createTrait: unit -> 'TTrait) =
    let wildcard = createTrait ()

    member _.TargetToTraitMap = Dictionary<int, 'TTrait>()

    interface IRelation<'TTrait> with
        member _.IsTag = true

        member this.WithTarget(entity: EntityId) =
            let (EntityId n) = entity

            lock this.TargetToTraitMap (fun () ->
                match this.TargetToTraitMap.TryGetValue n with
                | true, t -> t
                | false, _ ->
                    let t = createTrait ()
                    this.TargetToTraitMap.Add(n, t)
                    t)

        member _.Wildcard() = wildcard

type private TestTagRelation() =
    inherit TestRelation<ITagTrait>(fun () -> TestTagTrait())

type private TestValueRelation<'T, 'TMutable>() =
    inherit TestRelation<IMutableValueTrait<'T, 'TMutable>>(fun () -> TestValueTrait<'T, 'TMutable>())

type private QueryResult<'T, 'TMutable> private (entities, getRead, getMutable) =
    static member Create(entities, getRead, getMutable) =
        QueryResult<'T, 'TMutable>(entities, getRead, getMutable)

    interface IQueryResult<'T, 'TMutable> with
        member _.ForEach callback =
            for entity in entities do
                match getRead entity with
                | Some value -> callback (value, entity)
                | None -> ()

        // TODO: Implement change detection
        member _.UpdateEachWith changeDetectionOption callback =
            for e in entities do
                match getMutable e with
                | Some v -> callback (v, e)
                | None -> ()

    interface IEnumerable<EntityId> with
        member _.GetEnumerator() = entities.GetEnumerator()

    interface IEnumerable with
        member _.GetEnumerator() = entities.GetEnumerator()

type private TestTraitFactory() =
    interface ITraitFactory with
        member _.CreateAdded() =
            { new IAddedTracker with
                member _.Tracker = AddedTracker
            }

        member _.CreateChanged() =
            { new IChangedTracker with
                member _.Tracker = ChangedTracker
            }

        member _.CreateRemoved() =
            { new IRemovedTracker with
                member _.Tracker = RemovedTracker
            }

        member _.Relation config = TestTagRelation()

        member _.RelationWith(config, store, mutableStore) = TestValueRelation<'T, 'TMutable>()

        member _.TagTrait() = TestTagTrait()

        member _.TraitWith value mutableValue = TestValueTrait<'T, 'TMutable>()

        member _.TraitWithRef valueFactory = TestValueTrait<'T, 'T>()

[<AutoOpen>]
module private Ids =
    let getWorldId entity =
        let (EntityId id) = entity
        id >>> 28 &&& 0xF

    let getLocalId entity =
        let (EntityId id) = entity
        id &&& 0x0FFFFFFF

    let packEntityId worldId localEntityId =
        worldId <<< 28 ||| (localEntityId &&& 0x0FFFFFFF) |> EntityId

[<AutoOpen>]
module private World =
    type World = {
        Id: int
        TraitStores: Dictionary<ITrait, Dictionary<int, obj option>>
        mutable NextEntityId: int
    }

    let private getStore someTrait world =
        match world.TraitStores.TryGetValue someTrait with
        | true, traitStore -> traitStore
        | false, _ ->
            let newStore = Dictionary<int, obj option>()
            world.TraitStores.Add(someTrait, newStore)
            newStore

    let private allocEntity world =
        let entityId = world.NextEntityId
        world.NextEntityId <- world.NextEntityId + 1
        packEntityId world.Id entityId

    let createWorld id = {
        Id = id
        TraitStores = Dictionary<ITrait, Dictionary<int, obj option>>()
        NextEntityId = 1
    }

    let addTrait someTrait (EntityId entityId) world =
        let store = world |> getStore someTrait
        store.TryAdd(entityId, None) |> ignore

    let hasTrait someTrait (EntityId entityId) world =
        let store = world |> getStore someTrait
        store.ContainsKey entityId

    let removeTrait someTrait (EntityId entityId) world =
        let store = world |> getStore someTrait
        store.Remove entityId |> ignore

    let destroy entity world =
        for someTrait in world.TraitStores.Keys do
            world |> removeTrait someTrait entity

    let getTraitValue (valueTrait: IValueTrait<'T>) (EntityId entityId) world =
        let store = world |> getStore valueTrait

        match store.TryGetValue entityId with
        | true, Some value -> Some(value :?> 'T)
        | _ -> None

    let setTraitValue (valueTrait: IValueTrait<'T>) (value: 'T) (EntityId entityId) world =
        let store = world |> getStore valueTrait

        if not (store.ContainsKey entityId) then
            invalidArg (nameof valueTrait) $"Trait not present on entity {entityId}"

        store[entityId] <- Some(value :> obj)

    let setTraitValueWith (valueTrait: IValueTrait<'T>) (update: 'T -> 'T) (EntityId entityId) world =
        let store = world |> getStore valueTrait

        match store.TryGetValue entityId with
        | true, Some value ->
            let newValue = update (value :?> 'T)
            store[entityId] <- Some(newValue :> obj)
        | _ -> invalidArg (nameof valueTrait) $"Trait value not set on entity {entityId}"

    let targetsFor (relation: IRelation<'TTrait>) entity world =
        let relationImpl = relation :?> TestRelation<'TTrait>

        let targetOfSubject (kvp: KeyValuePair<int, 'TTrait>) =
            if world |> hasTrait kvp.Value entity then
                Some(EntityId kvp.Key)
            else
                None

        relationImpl.TargetToTraitMap |> Seq.choose targetOfSubject |> Array.ofSeq

    let targetFor relation entity world =
        let targets = world |> targetsFor relation entity

        if targets |> Seq.isEmpty then
            None
        else
            Some(targets |> Seq.head)

    // TODO: Extend this for change tracking.
    type private MatchedEntities = {
        With: Set<int>
        Or: Set<int>
        Not: Set<int>
    } with

        static member Empty = { With = Set.empty; Or = Set.empty; Not = Set.empty }

    let query where world =
        let getEntitySet someTrait =
            let store = world |> getStore someTrait
            store.Keys |> Set.ofSeq

        let getEntitySetUnion traits =
            traits |> Seq.map getEntitySet |> Set.unionMany

        let collect acc queryOp =
            match queryOp with
            | With someTrait -> {
                acc with
                    With = someTrait |> getEntitySet |> Set.intersect acc.With
              }
            | Or traits -> { acc with Or = traits |> getEntitySetUnion |> Set.union acc.Or }
            | Not traits -> { acc with Not = traits |> getEntitySetUnion |> Set.union acc.Not }
            // TODO: Implement change tracking.
            | Changed(_, _) -> acc
            | Added(_, _) -> acc
            | Removed(_, _) -> acc

        let matches = where |> Array.fold collect MatchedEntities.Empty

        Set.difference (Set.intersect matches.Or matches.With) matches.Not
        |> Seq.map EntityId

    let queryFirst where world =
        let results = world |> query where
        if Seq.isEmpty results then None else Some(Seq.head results)

    let spawn traits world =
        let entity = world |> allocEntity

        let setValue (someTrait, value) =
            let (EntityId entityId) = entity
            world |> addTrait someTrait entity

            // Since we don't know the type of the value, we need to access the store directly.
            let store = world |> getStore someTrait
            store[entityId] <- Some value

        for someTrait in traits do
            someTrait |> TraitSpec.Map (fun tag -> world |> addTrait tag entity) setValue

        entity

type private Universe private () =
    // World ID assignment (top 4 bits). Keep a global counter for worlds.
    let mutable nextWorldId = 1
    let worlds = Dictionary<int, World>()

    let allocWorldId () =
        let id = nextWorldId
        nextWorldId <- nextWorldId + 1

        if id > 0xF then
            failwith "TestWorld: too many worlds (max 16)"

        id

    let findWorld entity =
        let worldId = entity |> getWorldId

        match worlds.TryGetValue worldId with
        | true, world -> world
        | _ -> invalidArg (nameof worldId) $"No world registered for id {worldId}"

    let registerWorld world =
        worlds[world.Id] <- world
        world

    static member Instance = Universe()

    member this.CreateWorld() =
        allocWorldId () |> createWorld |> registerWorld

    interface IEntityOperations with
        member _.Add someTrait entity =
            findWorld entity |> addTrait someTrait entity

        member _.Destroy entity = findWorld entity |> destroy entity

        member _.FriendlyId entity = getLocalId entity

        member _.Get valueTrait entity =
            findWorld entity |> getTraitValue valueTrait entity

        member _.Has someTrait entity =
            findWorld entity |> hasTrait someTrait entity

        member _.Remove someTrait entity =
            findWorld entity |> removeTrait someTrait entity

        member _.Set valueTrait value entity =
            findWorld entity |> setTraitValue valueTrait value entity

        member _.SetWith valueTrait update entity =
            findWorld entity |> setTraitValueWith valueTrait update entity

        member _.TargetFor relation entity =
            findWorld entity |> targetFor relation entity

        member _.TargetsFor relation entity =
            findWorld entity |> targetsFor relation entity

type TestWorld() =
    let world = Universe.Instance.CreateWorld()

    // World-level special entity has ID 0.
    let worldEntity = packEntityId world.Id 0

    interface IWorld with
        member _.Add someTrait = world |> addTrait someTrait worldEntity

        member _.Get valueTrait =
            world |> getTraitValue valueTrait worldEntity

        member _.Has someTrait = world |> hasTrait someTrait worldEntity

        member _.Query where =
            let entities = world |> query where
            QueryResult.Create(entities, (fun _ -> Some()), (fun _ -> Some()))

        member _.QueryTrait(someTrait, where) =
            let entities = world |> query [| With someTrait; yield! where |]

            let getRead entity = world |> getTraitValue someTrait entity

            let getMutable entity =
                getRead entity |> Option.map (fun value -> value :> obj :?> 'TMutable)

            QueryResult.Create(entities, getRead, getMutable)

        member _.QueryTraits(firstTrait, secondTrait, where) =
            let entities = world |> query [| With firstTrait; With secondTrait; yield! where |]

            let getRead entity =
                match world |> getTraitValue firstTrait entity, world |> getTraitValue secondTrait entity with
                | Some a, Some b -> Some(a, b)
                | _ -> None

            let getMutable entity =
                getRead entity
                |> Option.map (fun (firstValue, secondValue) ->
                    firstValue :> obj :?> 'TMutable, secondValue :> obj :?> 'UMutable)

            QueryResult.Create(entities, getRead, getMutable)

        member _.QueryTraits3(firstTrait, secondTrait, thirdTrait, where) =
            let entities =
                world
                |> query [| With firstTrait; With secondTrait; With thirdTrait; yield! where |]

            let getRead entity =
                match
                    world |> getTraitValue firstTrait entity,
                    world |> getTraitValue secondTrait entity,
                    world |> getTraitValue thirdTrait entity
                with
                | Some a, Some b, Some c -> Some(a, b, c)
                | _ -> None

            let getMutable entity =
                getRead entity
                |> Option.map (fun (firstValue, secondValue, thirdValue) ->
                    firstValue :> obj :?> 'TMutable, secondValue :> obj :?> 'UMutable, thirdValue :> obj :?> 'VMutable)

            QueryResult.Create(entities, getRead, getMutable)

        member _.QueryTraits4(firstTrait, secondTrait, thirdTrait, fourthTrait, where) =

            let entities =
                world
                |> query [|
                    With firstTrait
                    With secondTrait
                    With thirdTrait
                    With fourthTrait
                    yield! where
                |]

            let getRead entity =
                match
                    world |> getTraitValue firstTrait entity,
                    world |> getTraitValue secondTrait entity,
                    world |> getTraitValue thirdTrait entity,
                    world |> getTraitValue fourthTrait entity
                with
                | Some a, Some b, Some c, Some d -> Some(a, b, c, d)
                | _ -> None

            let getMutable entity =
                getRead entity
                |> Option.map (fun (firstValue, secondValue, thirdValue, fourthValue) ->
                    firstValue :> obj :?> 'TMutable,
                    secondValue :> obj :?> 'UMutable,
                    thirdValue :> obj :?> 'VMutable,
                    fourthValue :> obj :?> 'WMutable)

            QueryResult.Create(entities, getRead, getMutable)

        member _.QueryFirst where = world |> queryFirst where

        member _.Remove someTrait =
            world |> removeTrait someTrait worldEntity

        member _.Set valueTrait value =
            world |> setTraitValue valueTrait value worldEntity

        member _.Spawn traits = world |> spawn traits

module TestECS =

    let Install () =
        let router = Universe.Instance
        let traits = TestTraitFactory()
        Globals.Instance.Entities <- router
        Globals.Instance.Traits <- traits
