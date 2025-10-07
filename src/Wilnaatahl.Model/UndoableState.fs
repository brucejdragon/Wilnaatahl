namespace Wilnaatahl.ViewModel

/// Tracks past, present, and future states for undo/redo functionality.
module UndoableState =
    type UndoableState<'T> =
        private
            { Past: 'T list
              Present: 'T
              Future: 'T list }

    let createUndoableState initial =
        { Past = []
          Present = initial
          Future = [] }

    let canUndo state = not (List.isEmpty state.Past)
    let canRedo state = not (List.isEmpty state.Future)

    /// Gets the current state.
    let current state = state.Present

    /// Clears the stack of redoable (i.e. -- previously undone) states.
    /// Recommended when an operation that changes the current state completes.
    let clearRedo state = { state with Future = [] }

    /// Pops and redos the last undone state, if any.
    /// The present state becomes the next undoable state.
    let redo state =
        match state.Future with
        | next :: rest ->
            { Past = state.Present :: state.Past
              Present = next
              Future = rest }
        | [] -> state

    /// Saves the current state for potential undo.
    let saveCurrentForUndo state =
        { state with Past = state.Present :: state.Past }

    /// Sets the current state to a new value.
    /// The undo and redo stacks remain unchanged.
    let setCurrent newPresent state = { state with Present = newPresent }

    /// Pops and undoes the last saved state, if any.
    /// The present state becomes the next redoable state.
    let undo state =
        match state.Past with
        | prev :: rest ->
            { Past = rest
              Present = prev
              Future = state.Present :: state.Future }
        | [] -> state
