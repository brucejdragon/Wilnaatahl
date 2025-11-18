module Wilnaatahl.Systems.Traits

open Wilnaatahl.ECS.Relation
open Wilnaatahl.ECS.Trait
open Wilnaatahl.Model

/// Represents a 3D point that can have its individual co-ordinates mutated.
/// Useful when modifying co-ordinates in an inner loop where performance is critical.
type Position3D =
    { mutable x: float
      mutable y: float
      mutable z: float }

    static member Empty = { x = 0; y = 0; z = 0 }

/// Used for entities that represent "tree nodes", i.e. people in the family tree.
let PersonRef = refTrait (fun () -> Person.Empty)

/// Used for any visible entity that has an on-screen position.
let Position = mutableTrait {| x = 0.0; y = 0.0; z = 0.0 |} Position3D.Empty

/// Used for any visible entity that has a position and is animating towards a target position.
/// An entity that has this trait must also have the Position trait, although it's unclear how
/// to enforce this via the ECS.
let TargetPosition = mutableTrait {| x = 0.0; y = 0.0; z = 0.0 |} Position3D.Empty

/// Used to mark tree nodes that are selected.
let Selected = tagTrait ()

/// Represents an ongoing drag operation as a relation to the node being dragged.
let Dragging =
    mutableRelationWith {| x = 0.0; y = 0.0; z = 0.0 |} Position3D.Empty { IsExclusive = true }
