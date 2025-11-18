module Wilnaatahl.Systems.Traits

open Wilnaatahl.ECS.Relation
open Wilnaatahl.ECS.Trait
open Wilnaatahl.Model

/// Represents a 3-component vector that can have its individual components mutated.
/// Useful when modifying co-ordinates or bounds in an inner loop where performance is critical.
type MutableVector3 =
    { mutable x: float
      mutable y: float
      mutable z: float }

    static member Empty = { x = 0; y = 0; z = 0 }

/// Used for entities that represent "tree nodes", i.e. people in the family tree.
let PersonRef = refTrait (fun () -> Person.Empty)

/// Used for any visible entity that has an on-screen position.
/// For Bounding Boxes, this is the top-left-front corner.
let Position = mutableTrait {| x = 0.0; y = 0.0; z = 0.0 |} MutableVector3.Empty

/// Used for any visible entity that has a position and is animating towards a target position.
/// An entity that has this trait must also have the Position trait, although it's unclear how
/// to enforce this via the ECS.
let TargetPosition =
    mutableTrait {| x = 0.0; y = 0.0; z = 0.0 |} MutableVector3.Empty

/// Used to mark tree nodes that are selected.
let Selected = tagTrait ()

/// Represents an ongoing drag operation as a relation to the node being dragged.
let Dragging =
    mutableRelationWith {| x = 0.0; y = 0.0; z = 0.0 |} MutableVector3.Empty { IsExclusive = true }

/// Represents a distance on each axis. For Bounding Boxes, it's the distance the box maintains around the
/// Nodes it contains (taking into account the sizes of the Nodes). For Nodes, it's the distance out from the
/// Node's Position that determines its extent. For rectilinear Nodes, it translates directly to the Node
/// shape's dimensions. For spherical Nodes, the smallest component of this trait becomes the radius.
let Margin = valueTrait {| x = 0.0; y = 0.0; z = 0.0 |}

/// Opposite corner of a Bounding Box (bottom-right-back).
let Bounds = mutableTrait {| x = 0.0; y = 0.0; z = 0.0 |} MutableVector3.Empty

/// Pins the Position of one entity to the mid-point of a line represented by the target entity.
/// The target must have a LineTo relationship to another entity with Position.
let Bisects = tagRelationWith { IsExclusive = true }

/// Represents a line from one entity with Position to another. Which entity represents the line
/// as a whole is arbitrary.
let LineTo = tagRelationWith { IsExclusive = true }

/// Relationship from an entity with Position to a Bounding Box that contains it.
let BoundedBy = tagRelation ()

/// Relationship from one entity with Position to another entity with Position, where the subject's X co-ordinate
/// will automatically track the target's with the given distance.
let FollowsX = valueRelationWith {| x = 0.0 |} { IsExclusive = true }

/// Relationship from one entity with Position to another entity with Position, where the subject's Y co-ordinate
/// will automatically track the target's with the given distance.
let FollowsY = valueRelationWith {| y = 0.0 |} { IsExclusive = true }

/// Relationship from one entity with Position to another entity with Position, where the subject's Z co-ordinate
/// will automatically track the target's with the given distance.
let FollowsZ = valueRelationWith {| z = 0.0 |} { IsExclusive = true }

/// Relationship from one entity representing a line (subject of a LineTo relation) to another, where the subject's
/// endpoint Positions will automatically track the target's so that the lines stay parallel with the given offset.
let Parallels = valueRelationWith {| offset = 0.0 |} { IsExclusive = true }

/// Indicates which Wilp a tree node is rendered in. If an entity has this trait, it must also have a PersonRef,
/// but not necessarily the other way around since this trait is added during layout.
let RenderedInWilp = valueTrait {| wilp = "" |}

/// Marks an entity with Position and MeshRef as representing an "elbow" connector, which is rendered as
/// a small sphere the same diameter and colour as lines.
let Elbow = tagTrait ()

/// Marks an entity such that it shouldn't be rendered (i.e. -- React components won't add a MeshRef trait).
let Hidden = tagTrait ()
