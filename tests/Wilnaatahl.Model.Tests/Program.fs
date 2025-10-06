module Wilnaatahl.Tests.Program

open Expecto

let private allTests =
    testList
        "AllTests"
        [ NodeStateTests.tests
          ViewModelTests.tests
          UndoableStateTests.tests ]

[<EntryPoint>]
let main argv = runTestsWithCLIArgs [] argv allTests
