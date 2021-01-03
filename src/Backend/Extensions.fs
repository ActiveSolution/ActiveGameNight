[<AutoOpen>]
module Backend.Extensions

open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling
open Domain
open Giraffe
open Saturn
open Backend.Turbo


type BasePath with
    member this.Val = this |> fun (BasePath bp) -> bp

module HttpContext = 
    let usernameKey = "username"
    
module ApiResultHelpers =
    let handleResult ctx okFunc res : HttpFuncResult =
        match res with
        | Ok value ->
            okFunc value ctx
        | Error (ApiError.Validation (ValidationError err)) -> 
            Response.badRequest ctx err
        | Error (ApiError.Domain (DomainError err)) -> 
            Response.badRequest ctx err
        | Error (ApiError.MissingUser _) -> 
            Turbo.redirect "/user/add" ctx
        | Error (ApiError.NotFound _) -> 
            Response.notFound ctx ()
        | Error (ApiError.Duplicate)  -> 
            Response.internalError ctx ()
        
    let turboRedirect location ctx = Turbo.redirect location ctx
    let turboStream ts ctx = TurboStream.writeTurboStreamContent ts ctx
    
    let fullPageHtml (env: #ITemplateBuilder) content ctx =
        content
        |> env.Templates.FullPage
        |> Controller.html ctx
        
    let fragment (env: #ITemplateBuilder) content ctx =
        content
        |> env.Templates.Fragment
        |> Controller.html ctx
    
type HttpContext with
    member this.GetUser() =
        this.Session.GetString(HttpContext.usernameKey)
        |> Option.ofObj
        |> Result.requireSome "Missing user in HttpContext"
        |> Result.mapError ValidationError
        |> Result.bind User.create
        
    member this.SetUsername(user: User) =
        this.Session.SetString(HttpContext.usernameKey, user.Val)
        
    member this.ClearUsername() =
        this.Session.Remove(HttpContext.usernameKey)

    
    member ctx.RespondWithHtmlFragment (env, content) =
        ApiResultHelpers.fragment env content ctx
    member ctx.RespondWithHtml (env, content) =
        ApiResultHelpers.fullPageHtml env content ctx
    member ctx.RespondWithHtml (env, contentResult) =
        contentResult
        |> ApiResultHelpers.handleResult ctx (ApiResultHelpers.fullPageHtml env)
    member ctx.RespondWithHtml (env, contentTaskResult) =
        contentTaskResult
        |> Task.bind (ApiResultHelpers.handleResult ctx (ApiResultHelpers.fullPageHtml env))
    
    member ctx.RespondWithRedirect(location) = Turbo.redirect location ctx
    member ctx.RespondWithRedirect(locationResult) =
        locationResult 
        |> ApiResultHelpers.handleResult ctx (ApiResultHelpers.turboRedirect)
    member ctx.RespondWithRedirect(locationResult) =
        locationResult
        |> Task.bind (ApiResultHelpers.handleResult ctx (ApiResultHelpers.turboRedirect))  
    
    member ctx.OfTurboStream ts = ApiResultHelpers.turboStream ts ctx
    
        
module Map =
    let keys map = map |> Map.toList |> List.map fst
    let values map = map |> Map.toList |> List.map snd
    
    let change key f map =
        Map.tryFind key map
        |> f
        |> function
        | Some v -> Map.add key v map
        | None -> Map.remove key map

    let tryFindWithDefault defaultValue key map =
        map
        |> Map.tryFind key
        |> Option.defaultValue defaultValue
        

module Async =
    let map f xAsync =
        async {
            let! x = xAsync
            return f x
        }

module Result =
    let toOption xResult = 
        match xResult with 
        | Error _ -> None 
        | Ok v -> Some v
