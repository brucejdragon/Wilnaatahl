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

    /// Gets the current state.
    let current state = state.present

    /// Clears the stack of redoable (i.e. -- previously undone) states.
    /// Recommended when an operation that changes the current state completes.
    let clearRedo state = { state with future = [] }

    /// Pops and redos the last undone state, if any.
    /// The present state becomes the next undoable state.
    let redo state =
        match state.future with
        | next :: rest ->
            { past = state.present :: state.past
              present = next
              future = rest }
        | [] -> state

    /// Saves the current state for potential undo.
    let saveCurrentForUndo state =
        { state with past = state.present :: state.past }

    /// Sets the current state to a new value.
    /// The undo and redo stacks remain unchanged.
    let setCurrent newPresent state = { state with present = newPresent }

    /// Pops and undoes the last saved state, if any.
    /// The present state becomes the next redoable state.
    let undo state =
        match state.past with
        | prev :: rest ->
            { past = rest
              present = prev
              future = state.present :: state.future }
        | [] -> state
