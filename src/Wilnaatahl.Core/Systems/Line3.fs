module Wilnaatahl.Systems.Line3

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity
open Wilnaatahl.ECS.Relation
open Wilnaatahl.ECS.TraitExtensions
open Wilnaatahl.ViewModel
open Wilnaatahl.Systems.Traits

// NOTE: Many connectors have initial position at the origin for convenience. Those positions
// will be dynamically updated the first time the Movement system runs.
let zeroPosition = {| x = 0.0; y = 0.0; z = 0.0 |}

let spawn firstPos secondPos (world: IWorld) =
    let firstEndpointId =
        world.Spawn(Position.Val firstPos, Hidden.Tag(), Connector.Tag())

    let secondEndpointId =
        world.Spawn(Position.Val secondPos, Hidden.Tag(), Connector.Tag())

    let lineId = world.Spawn(Line.Tag(), Connector.Tag())
    firstEndpointId |> add (EndpointOf => lineId)
    secondEndpointId |> add (EndpointOf => lineId)
    lineId

let spawnDynamic (world: IWorld) =
    world |> spawn zeroPosition zeroPosition

let spawnHidden firstPos secondPos (world: IWorld) =
    let lineId = world |> spawn firstPos secondPos
    lineId |> add Hidden
    lineId

let snapToWithOffset targetId (x, y, z) subjectId =
    subjectId |> addWith (SnapToX => targetId) {| x = x |}
    subjectId |> addWith (SnapToY => targetId) {| y = y |}
    subjectId |> addWith (SnapToZ => targetId) {| z = z |}

let getEndpoints (world: IWorld) lineId =
    let endpoints = world.Query(With(EndpointOf => lineId)) |> Array.ofSeq

    if endpoints.Length = 2 then
        endpoints[0], endpoints[1]
    else
        failwith $"Found Line {lineId} with {endpoints.Length} endpoints."

let updateEndpoints (world: IWorld) changeOption f1 f2 lineId =
    let mutable i = 0

    world.QueryTrait(Position, With(EndpointOf => lineId)).UpdateEachWith changeOption
    <| fun (pos, _) ->
        i <- i + 1

        match i with
        | 1 -> f1 pos
        | 2 -> f2 pos
        | _ -> failwith $"Found Line {lineId} with {i} endpoints during update."

let snapTo (world: IWorld) firstPosId secondPosId lineId =
    let zeroDistance = 0.0, 0.0, 0.0

    let firstEndpointId, secondEndpointId = lineId |> getEndpoints world
    firstEndpointId |> snapToWithOffset firstPosId zeroDistance
    secondEndpointId |> snapToWithOffset secondPosId zeroDistance

    lineId

let getPositions (world: IWorld) lineId =
    let firstEndpointId, secondEndpointId = lineId |> getEndpoints world

    match firstEndpointId |> get Position, secondEndpointId |> get Position with
    | Some firstPos, Some secondPos -> Vector.fromPosition firstPos, Vector.fromPosition secondPos
    | Some _, None
    | None, Some _
    | None, None -> failwith $"Found Line {lineId} with endpoint(s) that have no Position."
