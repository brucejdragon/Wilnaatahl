module Wilnaatahl.Tests.TestUtils

open System.Diagnostics
open System.Threading
open Wilnaatahl.ViewModel.LayoutBox

/// Pauses a test run to allow attaching a debugger to the test host.
let debugBreak () =
    if not Debugger.IsAttached then
        let pid = Process.GetCurrentProcess().Id
        printfn $"Please attach a debugger to process ID: {pid}"

    while not Debugger.IsAttached do
        Thread.Sleep 100

    Debugger.Break()

/// Exercise LayoutBox.visit by calculating the offsets of every person in the tree.
let setPositions (initialPosition, rootBox) =
    let visitLeaf pos personId offset =
        (personId, pos + offset) |> Seq.singleton

    let visitComposite pos results =
        results
        |> Seq.concat
        |> Seq.map (fun (personId, offset) -> personId, pos + offset)

    rootBox |> visit visitLeaf visitComposite initialPosition
