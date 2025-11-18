namespace Wilnaatahl.ECS

#if FABLE_COMPILER
open Fable.Core

/// Exposes Koota functionality via wrappers defined in F# and implemented in TypeScript.
module private KootaWrapper =
    [<Import("createTraitFactory", "../../ecs/kootaWrapper.ts")>]
    let createTraitFactory: unit -> ITraitFactory = nativeOnly

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

module ECS =
    let tagTrait () = Globals.Instance.Traits.TagTrait()
    let traitWith data = Globals.Instance.Traits.TraitWith data
