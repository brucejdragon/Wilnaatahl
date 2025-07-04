namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model

type Node = {
    id: string
    position: float * float * float
    children: string list
    person: Person option
}