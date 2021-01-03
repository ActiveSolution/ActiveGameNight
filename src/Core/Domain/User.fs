namespace Domain

module User =
    open System

    let create str =
        let str = Helpers.canonize str
        if String.IsNullOrWhiteSpace str then
            Error (ValidationError "User cannot be empty")
        else User str |> Ok
    
    let value (User username) = Helpers.unCanonize username
    
[<AutoOpen>]
module UserExtensions =
    type User with    
        member this.Val = this |> User.value
        member this.Canonized = this |> fun (User raw) -> raw

    
