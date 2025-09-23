module Wilnaatahl.Model.Tests

open Fable.Mocha
open Wilnaatahl.ViewModel.UndoableState

let undoTests =
    testList
        "UndoableState"
        [ test "undo restores previous state" {
              let initial = createUndoableState 1
              let state2 = initial |> saveForUndo id |> setCurrent 2
              let state3 = state2 |> saveForUndo id |> setCurrent 3
              let undone = undo state3
              Expect.equal (current undone) 2 "Should undo to previous state"
          } ]

Mocha.runTests undoTests |> ignore
