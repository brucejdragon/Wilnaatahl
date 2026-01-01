module Wilnaatahl.Tests.TestUtils

open System.Diagnostics
open System.Threading

let debugBreak () =
    if not Debugger.IsAttached then
        let pid = Process.GetCurrentProcess().Id
        printfn $"Please attach a debugger to process ID: {pid}"

    while not Debugger.IsAttached do
        Thread.Sleep 100

    Debugger.Break()
