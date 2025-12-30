module Wilnaatahl.Systems.Traits

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Relation
open Wilnaatahl.ECS.Trait
open Wilnaatahl.Model
open Wilnaatahl.ViewModel

/// Represents a 3-component vector that can have its individual components mutated.
/// Useful when modifying co-ordinates or bounds in an inner loop where performance is critical.
type MutableVector3 = {
    mutable x: float
    mutable y: float
    mutable z: float
} with

    static member Empty = { x = 0; y = 0; z = 0 }

    member this.ToVector3() =
        Vector.fromComponents (this.x, this.y, this.z)

/// Used for entities that represent "tree nodes", i.e. people in the family tree.
let PersonRef = refTrait (fun () -> Person.Empty)

/// Used for any visible entity that has an on-screen position.
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
/// nodes it contains (taking into account the sizes of the nodes). For nodes, it's the distance out from the
/// node's Position that determines its extent. For rectilinear nodes, it translates directly to the node
/// shape's dimensions. For spherical nodes, the smallest component of this trait becomes the radius.
let Size = valueTrait {| x = 0.0; y = 0.0; z = 0.0 |}

/// Pins the Position of one entity to the mid-point of a target Line entity.
let Bisects = tagRelationWith { IsExclusive = true }

/// Represents a line between two entities with Position. Acts as the target of the EndpointOf relation.
let Line = tagTrait ()

/// Marks an entity with Position as being an endpoint of the target Line entity. A Line should have exactly
/// two subject entities referring to it via this relation.
let EndpointOf = tagRelationWith { IsExclusive = true }

/// Relationship from a Bounding Box to an entity with Position that it contains.
let BoundingBoxOn = tagRelation () // TODO: Upgrade Koota so it doesn't kill perf to have one trait per node

/// Marks an entity with Position as being a corner of the target BoundingBox entity. A BoundingBox should
/// have exactly two subject entities referring to it via this relation. One of the corners is closest to
/// the origin (the left-bottom-back corner, to fit the Three.js co-ordinate system which has higher X moving
/// right, higher Y moving up, and higher Z moving towards the camera). The other is its opposite corner
/// (right-top-front), indicated by IsBounds = true.
let CornerOf = valueRelationWith {| IsBounds = false |} { IsExclusive = true }

/// Relationship from one entity with Position to another entity with Position, where the subject's X co-ordinate
/// will automatically track the target's with the given distance.
let SnapToX = valueRelationWith {| x = 0.0 |} { IsExclusive = true }

/// Relationship from one entity with Position to another entity with Position, where the subject's Y co-ordinate
/// will automatically track the target's with the given distance.
let SnapToY = valueRelationWith {| y = 0.0 |} { IsExclusive = true }

/// Relationship from one entity with Position to another entity with Position, where the subject's Z co-ordinate
/// will automatically track the target's with the given distance.
let SnapToZ = valueRelationWith {| z = 0.0 |} { IsExclusive = true }

/// Relationship from one Line entity to another, where the subject's endpoint Positions will automatically track
/// the target's so that the lines stay parallel. The subject line is offset from the target line by a vector that is
/// the projection of the vertical axis onto the plane that is perpendicular to the target line. To put it another way,
/// it is vertically offset in such a way that it appears co-incident with the target line when viewed from above or below,
/// no matter what the direction of the line is in 3D space. It is not just a simple translation on the Y-axis.
/// The absolute value of the given offset determines the distance between the lines while the sign determines the direction.
/// Negative offsets place the line below, positive offsets above. When the target line is vertical, the subject line will be
/// offset in the x or z direction, depending on which is greater. Note that the line will stay above or below the target line
/// no matter how its endpoints are moved in space.
let Parallels = valueRelationWith {| offset = 0.0 |} { IsExclusive = true }

/// Indicates which Wilp a tree node is rendered in. If an entity has this relation, it must also have a PersonRef,
/// but not necessarily the other way around since this relation is added during layout. <-- TODO: Is it though?
let RenderedIn = tagRelationWith { IsExclusive = true }

/// Identifies an entity that represents a rendered Wilp, which is a special BoundingBox that contains, directly or
/// indirectly, all tree nodes representing wilp members.
let Wilp = valueTrait {| wilpName = "" |}

/// Marks an entity with Position and MeshRef as representing an "elbow" connector, which is rendered as
/// a small sphere the same diameter and colour as lines.
let Elbow = tagTrait ()

/// Marks an entity such that it shouldn't be rendered (i.e. -- React components won't add a MeshRef trait).
let Hidden = tagTrait ()

/// Used to mark connector entities for ease of cleanup later.
let Connector = tagTrait ()
