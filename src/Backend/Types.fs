namespace Backend

open Feliz.ViewEngine


type ApiError =
    | Duplicate 
    | NotFound
    | Validation of string
    | Domain of string
    | MissingUser of string
    
// Web
type BasePath = BasePath of string
    with member this.Val = this |> function BasePath basePath -> basePath
type Domain = Domain of string
    with member this.Val = this |> function Domain basePath -> basePath
type ITemplateSettings =
    abstract BasePath : BasePath
    abstract Domain : Domain
type ITemplates =
    abstract FullPage : ReactElement -> string
    abstract Fragment : ReactElement -> string
type ITemplateBuilder =
    abstract Templates : ITemplates
    
    
