namespace Wilnaatahl.ECS

open System
#if FABLE_COMPILER
open Fable.Core

/// Exposes Koota functionality via wrappers defined in F# and implemented in TypeScript.
module private KootaWrapper =
    [<Import("createTraitFactory", "../../ecs/kootaWrapper.ts")>]
    let createTraitFactory: unit -> ITraitFactory = nativeOnly

    [<Import("createEntityOperations", "../../ecs/kootaWrapper.ts")>]
    let createEntityOperations: unit -> IEntityOperations = nativeOnly

#else

/// Exists solely for the sake of unit testing ECS-related functionality from .NET.
module private TestSupport =
    /// A default implementation of ITraitProvider that always throws.
    /// This ensures that unit tests exercising the ECS don't forget to install a mock.
    let defaultTraitFactory =
        { new ITraitFactory with
            member _.CreateAdded() = raise (NotImplementedException())
            member _.CreateChanged() = raise (NotImplementedException())
            member _.CreateRemoved() = raise (NotImplementedException())
            member _.Relation _ = raise (NotImplementedException())
            member _.RelationWith(_, _, _) = raise (NotImplementedException())
            member _.TagTrait() = raise (NotImplementedException())

            member _.TraitWith<'T, 'TMutable> _ _ : IMutableValueTrait<'T, 'TMutable> =
                raise (NotImplementedException())

            member _.TraitWithRef<'T> _ : IMutableValueTrait<'T, 'T> = raise (NotImplementedException())
        }

    /// A default implementation of IEntityOperations that always throws.
    /// This ensures that unit tests exercising the ECS don't forget to install a mock.
    let defaultEntityOperations =
        { new IEntityOperations with
            member _.Add _ _ = raise (NotImplementedException())
            member _.Destroy _ = raise (NotImplementedException())
            member _.Get _ _ = raise (NotImplementedException())
            member _.Has _ _ = raise (NotImplementedException())
            member _.FriendlyId _ = raise (NotImplementedException())
            member _.Remove _ _ = raise (NotImplementedException())
            member _.Set _ _ _ = raise (NotImplementedException())
            member _.SetWith _ _ _ = raise (NotImplementedException())
            member _.TargetFor _ _ = raise (NotImplementedException())
            member _.TargetsFor _ _ = raise (NotImplementedException())
        }

#endif

/// Provides global access to ECS functionality. When compiling for .NET, it provides hooks for unit
/// tests to substitute Fable implementations with .NET ones.
type Globals private () =
    static member Instance = Globals()

    // We want global dependencies to be immutable and instantiated on the TypeScript
    // side for the app itself, but mutable and settable by unit tests on the .NET side.
    member val Traits: ITraitFactory =
#if FABLE_COMPILER
        KootaWrapper.createTraitFactory () with get
#else
        TestSupport.defaultTraitFactory with get, set
#endif

    member val Entities: IEntityOperations =
#if FABLE_COMPILER
        KootaWrapper.createEntityOperations () with get
#else
        TestSupport.defaultEntityOperations with get, set
#endif

module Entity =
    let add someTrait entity =
        entity |> Globals.Instance.Entities.Add someTrait

    let destroy entity =
        entity |> Globals.Instance.Entities.Destroy

    let friendlyId entity =
        entity |> Globals.Instance.Entities.FriendlyId

    let get valueTrait entity =
        entity |> Globals.Instance.Entities.Get valueTrait

    let getFirst valueTrait1 valueTrait2 entity =
        match entity |> get valueTrait1 with
        | Some value1 -> Some value1
        | None -> entity |> get valueTrait2

    let has someTrait entity =
        entity |> Globals.Instance.Entities.Has someTrait

    let remove someTrait entity =
        entity |> Globals.Instance.Entities.Remove someTrait

    let set valueTrait value entity =
        entity |> Globals.Instance.Entities.Set valueTrait value

    let setWith valueTrait f entity =
        entity |> Globals.Instance.Entities.SetWith valueTrait f

    let targetFor relation entity =
        entity |> Globals.Instance.Entities.TargetFor relation

    let targetWithValueFor relation entity =
        match entity |> targetFor relation with
        | Some target ->
            match entity |> get (relation.WithTarget target) with
            | Some value -> Some(target, value)
            | None ->
                // The relation data may be missing only if the target was removed from the relation
                // in between targetFor and get, which would be a concurrency bug in the app.
                failwith $"Relation data not found on entity {entity} for target {target}."
        | None -> None // Entity may not have the relation, which is a legit case.

    let targetsFor relation entity =
        entity |> Globals.Instance.Entities.TargetsFor relation

    let addWith valueTrait value entity =
        entity |> add valueTrait
        entity |> set valueTrait value

    let addOnce valueTrait f entity =
        if not (entity |> has valueTrait) then
            entity |> addWith valueTrait (f ())

        match entity |> get valueTrait with
        | Some value -> value
        | None -> failwith $"Entity {entity} does not have trait even though it was just added."

module Relation =
    let tagRelationWith config = Globals.Instance.Traits.Relation config

    let tagRelation () = tagRelationWith RelationConfig.Default

    let mutableRelationWith store mutableStore config =
        Globals.Instance.Traits.RelationWith(config, store, mutableStore)

    let mutableRelation store mutableStore =
        mutableRelationWith store mutableStore RelationConfig.Default

    let valueRelationWith store config = mutableRelationWith store store config

    let valueRelation store =
        valueRelationWith store RelationConfig.Default

    /// Returns the trait that represents the relation with the given entity as target. The returned
    /// trait can be used to set a value for the relation if it's a value trait, to add the relation
    /// with the given target to a subject entity, or anything else a trait can be used for.
    let inline (=>) (rel: IRelation<'TTrait>) targetEntity = rel.WithTarget targetEntity

module Tracking =
    let createAdded () = Globals.Instance.Traits.CreateAdded()
    let createChanged () = Globals.Instance.Traits.CreateChanged()
    let createRemoved () = Globals.Instance.Traits.CreateRemoved()

module Trait =
    let tagTrait () = Globals.Instance.Traits.TagTrait()

    let mutableTrait data mutableData =
        Globals.Instance.Traits.TraitWith data mutableData

    let valueTrait data = mutableTrait data data

    let refTrait dataFactory =
        Globals.Instance.Traits.TraitWithRef dataFactory

module Extensions =
    open Entity

    type IQueryResult<'T, 'TMutable> with
        /// Returns a sequence over the results of the query, including trait values. Note that this fully
        /// materializes all query results.
        member this.ToSequence() =
            let results = ResizeArray<'T * EntityId>()
            this.ForEach <| fun x -> results.Add x
            results :> seq<'T * EntityId>

        /// Invokes the given callback on every value/entity pair of the query results. The values are of the
        /// "mutable" type, not the "read" type, so the intent is that the callback can mutate the values
        /// passed to it. Change detection happens only if the query has a Changed modifier.
        member this.UpdateEach callback = this.UpdateEachWith Auto callback

    type IWorld with
        /// Adds the given trait to the world and sets its value in a single operation.
        member this.AddWith valueTrait value =
            this.Add valueTrait
            this.Set valueTrait value

        /// Queries for the first entity matching the given criteria that has the given value trait
        /// and returns the entity along with the trait value.
        member this.QueryFirstTrait
            (someTrait: IValueTrait<'T>, [<ParamArray>] where: QueryOperator[])
            : (EntityId * 'T) option =
            match this.QueryFirst [| With someTrait; yield! where |] with
            | Some entity ->
                match entity |> get someTrait with
                | Some value -> Some(entity, value)
                | None ->
                    // The trait could only be missing if it was removed in between QueryFirst and get,
                    // which would be a concurrency bug in the app.
                    failwith $"Required trait not found on entity {entity}."
            | None -> None // Legit case where no entities meet the criteria.

        /// Queries for the first entity matching the given criteria that has the given relation (i.e. acts as the
        /// subject of the relation) and returns the subject entity, target entity, and the trait value.
        member this.QueryFirstTarget<'T, 'TTrait when 'TTrait :> IValueTrait<'T>>
            (relation: IRelation<'TTrait>, [<ParamArray>] where: QueryOperator[])
            =
            match this.QueryFirst [| With(relation.Wildcard()); yield! where |] with
            | Some entity ->
                match entity |> targetWithValueFor relation with
                | Some(target, value) -> Some(entity, target, value)
                | None ->
                    // The target may be missing only if the relation was removed from the entity
                    // in between QueryFirst and targetWithValueFor, which would be a concurrency bug in the app.
                    failwith $"Target not found for relation on entity {entity}."
            | None -> None // Legit case where no entities have the relation.

        /// Removes all instances of the given trait from all entities in the world.
        member this.RemoveAll someTrait =
            for entity in this.Query(With someTrait) do
                entity |> remove someTrait
