namespace Wilnaatahl.ECS

#if FABLE_COMPILER
open Fable.Core

/// Exposes Koota functionality via wrappers defined in F# and implemented in TypeScript.
module private KootaWrapper =
    [<Import("createTraitFactory", "../../ecs/kootaWrapper.ts")>]
    let createTraitFactory: unit -> ITraitFactory = nativeOnly

    [<Import("createEntityOperations", "../../ecs/kootaWrapper.ts")>]
    let createEntityOperations: unit -> IEntityOperations = nativeOnly

#else
open System

/// Exists solely for the sake of unit testing ECS-related functionality from .NET.
module private TestSupport =
    /// A default implementation of ITraitProvider that always throws.
    /// This ensures that unit tests exercising the ECS don't forget to install a mock.
    let defaultTraitFactory =
        { new ITraitFactory with
            member _.Relation _ = raise (NotImplementedException())
            member _.RelationWith(_, _) = raise (NotImplementedException())
            member _.TagTrait() = raise (NotImplementedException())
            member _.TraitWith<'T>(_: 'T) : IValueTrait<'T> = raise (NotImplementedException())
            member _.TraitWithRef<'T>(_: (unit -> 'T)) : IValueTrait<'T> = raise (NotImplementedException()) }

    /// A default implementation of IEntityOperations that always throws.
    /// This ensures that unit tests exercising the ECS don't forget to install a mock.
    let defaultEntityOperations =
        { new IEntityOperations with
            member _.Add _ _ = raise (NotImplementedException())
            member _.Destroy _ = raise (NotImplementedException())
            member _.Get _ _ = raise (NotImplementedException())
            member _.Has _ _ = raise (NotImplementedException())
            member _.Remove _ _ = raise (NotImplementedException())
            member _.Set _ _ _ = raise (NotImplementedException())
            member _.TargetFor _ _ = raise (NotImplementedException())
            member _.TargetsFor _ _ = raise (NotImplementedException()) }

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

module Trait =
    let tagTrait () = Globals.Instance.Traits.TagTrait()
    let traitWith data = Globals.Instance.Traits.TraitWith data

module Entity =
    let add someTrait entity =
        entity |> Globals.Instance.Entities.Add someTrait

    let destroy entity =
        entity |> Globals.Instance.Entities.Destroy

    let get valueTrait entity =
        entity |> Globals.Instance.Entities.Get valueTrait

    let has someTrait entity =
        entity |> Globals.Instance.Entities.Has someTrait

    let remove someTrait entity =
        entity |> Globals.Instance.Entities.Remove someTrait

    let set valueTrait value entity =
        entity |> Globals.Instance.Entities.Set valueTrait value

    let targetFor relation entity =
        entity |> Globals.Instance.Entities.TargetFor relation

    let targetsFor relation entity =
        entity |> Globals.Instance.Entities.TargetsFor relation
