module Wilnaatahl.Tests.UndoableStateTests

open Xunit
open Swensen.Unquote
open Wilnaatahl.ViewModel.UndoableState

[<Fact>]
let ``createUndoableState initializes correctly`` () =
    let s = createUndoableState 42
    current s =! 42
    canUndo s =! false
    canRedo s =! false

[<Fact>]
let ``canUndo returns false for new state, true after saveCurrentForUndo`` () =
    let s = createUndoableState 1
    canUndo s =! false
    let s2 = s |> saveCurrentForUndo
    canUndo s2 =! true

[<Fact>]
let ``canRedo returns false for new state, true after undo`` () =
    let s = createUndoableState 1 |> saveCurrentForUndo |> setCurrent 2

    let s2 = undo s
    canRedo s2 =! true

[<Fact>]
let ``current returns present value`` () =
    let s = createUndoableState 99
    current s =! 99

[<Fact>]
let ``clearRedo disables redo after undo`` () =
    let s = createUndoableState 1 |> saveCurrentForUndo |> setCurrent 2 |> undo

    let s2 = clearRedo s
    canRedo s2 =! false

[<Fact>]
let ``redo restores next future state`` () =
    let s = createUndoableState 1 |> saveCurrentForUndo |> setCurrent 2 |> undo

    let s2 = redo s
    current s2 =! 2

[<Fact>]
let ``redo does nothing if future is empty`` () =
    let s = createUndoableState 1
    let s2 = redo s
    current s2 =! 1

[<Fact>]
let ``setCurrent changes present, leaves undo/redo unchanged`` () =
    let s = createUndoableState 1 |> saveCurrentForUndo |> setCurrent 2

    current s =! 2
    canUndo s =! true

[<Fact>]
let ``undo restores previous state and enables redo`` () =
    let s = createUndoableState 1 |> saveCurrentForUndo |> setCurrent 2

    let s2 = undo s
    current s2 =! 1
    canRedo s2 =! true

[<Fact>]
let ``undo does nothing if past is empty`` () =
    let s = createUndoableState 1
    let s2 = undo s
    current s2 =! 1
