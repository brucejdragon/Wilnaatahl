module Wilnaatahl.Model.UndoableStateTests

open Fable.Mocha
open Wilnaatahl.ViewModel.UndoableState

let undoableStateTests =
    testList
        "UndoableState"
        [ test "createUndoableState initializes correctly" {
              let s = createUndoableState 42
              Expect.equal (current s) 42 "Initial present value should be set"
              Expect.isFalse (canUndo s) "Should not be able to undo initially"
              Expect.isFalse (canRedo s) "Should not be able to redo initially"
          }
          test "canUndo returns false for new state, true after saveCurrentForUndo" {
              let s = createUndoableState 1
              Expect.isFalse (canUndo s) "Should not be able to undo initially"
              let s2 = s |> saveCurrentForUndo
              Expect.isTrue (canUndo s2) "Should be able to undo after saveCurrentForUndo"
          }
          test "canRedo returns false for new state, true after undo" {
              let s =
                  createUndoableState 1
                  |> saveCurrentForUndo
                  |> setCurrent 2

              let s2 = undo s
              Expect.isTrue (canRedo s2) "Should be able to redo after undo"
          }
          test "current returns present value" {
              let s = createUndoableState 99
              Expect.equal (current s) 99 "Current should return present value"
          }
          test "clearRedo disables redo after undo" {
              let s =
                  createUndoableState 1
                  |> saveCurrentForUndo
                  |> setCurrent 2
                  |> undo

              let s2 = clearRedo s
              Expect.isFalse (canRedo s2) "Redo should be disabled after clearRedo"
          }
          test "redo restores next future state" {
              let s =
                  createUndoableState 1
                  |> saveCurrentForUndo
                  |> setCurrent 2
                  |> undo

              let s2 = redo s
              Expect.equal (current s2) 2 "Redo should restore next future state"
          }
          test "redo does nothing if future is empty" {
              let s = createUndoableState 1
              let s2 = redo s
              Expect.equal (current s2) 1 "Redo should do nothing if future is empty"
          }
          test "setCurrent changes present, leaves undo/redo unchanged" {
              let s =
                  createUndoableState 1
                  |> saveCurrentForUndo
                  |> setCurrent 2

              Expect.equal (current s) 2 "Present should be updated"
              Expect.isTrue (canUndo s) "Should still be able to undo"
          }
          test "undo restores previous state and enables redo" {
              let s =
                  createUndoableState 1
                  |> saveCurrentForUndo
                  |> setCurrent 2

              let s2 = undo s
              Expect.equal (current s2) 1 "Undo should restore previous state"
              Expect.isTrue (canRedo s2) "Redo should be enabled after undo"
          }
          test "undo does nothing if past is empty" {
              let s = createUndoableState 1
              let s2 = undo s
              Expect.equal (current s2) 1 "Undo should do nothing if past is empty"
          } ]

Mocha.runTests undoableStateTests |> ignore
