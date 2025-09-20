namespace Wilnaatahl.ViewModel

/// Tracks past, present, and future states for undo/redo functionality.
module UndoableState =
    type UndoableState<'T> =
        private
            { past: 'T list
              present: 'T
              future: 'T list }

    let createUndoableState initial =
        { past = []
          present = initial
          future = [] }

    let canUndo state = not (List.isEmpty state.past)
    let canRedo state = not (List.isEmpty state.future)
    let current state = state.present

    let clearRedo state = { state with future = [] }

    let copyCurrentToUndo state =
        { state with past = state.present :: state.past }

    let redo state =
        match state.future with
        | next :: rest ->
            { past = state.present :: state.past
              present = next
              future = rest }
        | [] -> state

    let setCurrent newPresent state =
        // Keep past and future as is.
        { state with present = newPresent }

    let undo state =
        match state.past with
        | prev :: rest ->
            { past = rest
              present = prev
              future = state.present :: state.future }
        | [] -> state
