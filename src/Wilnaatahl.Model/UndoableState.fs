namespace Wilnaatahl.ViewModel

/// UndoableState: tracks past, present, and future states for undo/redo functionality.
type UndoableState<'T> private (past: 'T list, present: 'T, future: 'T list) =
    new(present) = UndoableState<'T>([], present, [])
    member _.CanUndo = not (List.isEmpty past)
    member _.CanRedo = not (List.isEmpty future)
    member _.Current = present

    member _.ClearRedo() = UndoableState<'T>(past, present, [])

    member _.CopyCurrentToUndo() =
        UndoableState<'T>(present :: past, present, future)

    member this.DiscardLastUndo() =
        match past with
        | _ :: rest -> UndoableState<'T>(rest, present, future)
        | [] -> this // nothing to pop

    member this.Redo() =
        match future with
        | next :: rest -> UndoableState<'T>(present :: past, next, rest)
        | [] -> this // nothing to redo

    member _.SetCurrent newPresent =
        // Keep past and future.
        UndoableState<'T>(past, newPresent, future)

    member this.Undo() =
        match past with
        | prev :: rest -> UndoableState<'T>(rest, prev, present :: future)
        | [] -> this // nothing to undo

module UndoableState =
    let create<'T> initial = UndoableState<'T>(initial)
