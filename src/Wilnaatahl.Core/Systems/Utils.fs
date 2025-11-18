module Wilnaatahl.Systems.Utils

open Wilnaatahl.ECS
open Wilnaatahl.ECS.Entity

let removeAll (world: IWorld) someTrait =
    // There could be a lot of entities to update, so we pull out the removal for
    // each entity into a standalone function for better perf.
    let remove (_, entity) = entity |> remove someTrait
    world.Query(With someTrait).UpdateEach remove
