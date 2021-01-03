namespace Domain

open System

module GameName =
    let create str =
        let str = Helpers.canonize str
        if String.IsNullOrWhiteSpace str then
            Error (ValidationError "GameName cannot be empty")
        else GameName str |> Ok
    let value (GameName v) = Helpers.unCanonize v

[<AutoOpen>]
module GameNameExtensions =
    type GameName with
        member this.Val = GameName.value this
        member this.Canonized = this |> fun (GameName raw) -> raw
