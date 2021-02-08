[<AutoOpen>]
module Backend.Extensions

open System
open System.Security.Claims
open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling
open Domain
open Giraffe
open Saturn
open FsHotWire
open FsHotWire.Giraffe

type BasePath with
    member this.Val = this |> fun (BasePath bp) -> bp

module Result =
    let toOption xResult = 
        match xResult with 
        | Error _ -> None 
        | Ok v -> Some v

module bool =
    let tryParse (input: string) =
        match bool.TryParse input with
        | true, value -> Some value
        | false, _ -> None
        
module HttpContext = 
    let userKey = "username"
    
module ClaimsPrincipal =
    let private getClaim ct (principal: ClaimsPrincipal) =
        if principal = null then Error "No user principal"
        elif principal.Claims = null || principal.Claims |> Seq.isEmpty then Error "No user claims"
        else
            principal.Claims
            |> Seq.filter (fun c ->
                c.Type = ct)
            |> Seq.exactlyOne
            |> (fun c -> Ok c.Value)
        
    let private getNameIdentifier = getClaim ClaimTypes.NameIdentifier
        
    let private getName = getClaim ClaimTypes.Name
    let getUser (principal: ClaimsPrincipal) =
        result {
            let! userId = principal |> getNameIdentifier |> Result.bind User.parseUserId
            let! username = principal |> getName |> Result.bind User.createUsername
            return   
                { User.Id = userId
                  User.Name = username }
        }
    
module ApiResultHelpers =
    let handleResult ctx okFunc res : HttpFuncResult =
        match res with
        | Ok value ->
            okFunc value ctx
        | Error (ApiError.BadRequest err) -> 
            Response.badRequest ctx err
        | Error (ApiError.Domain err) -> 
            Response.badRequest ctx err
        | Error (ApiError.MissingUser _) -> 
            Turbo.redirect "/user/add" ctx
        | Error (ApiError.NotFound _) -> 
            Response.notFound ctx ()
        | Error (ApiError.Duplicate)  -> 
            Response.internalError ctx ()
        | Error (ApiError.FormValidationError ts) ->
            match ctx.Request with
            | AcceptTurboStream ->
                TurboStream.writeTurboStreamContent 422 ts ctx
            | _ -> Response.badRequest ctx ""
        

    let getUsername (ctx: HttpContext) =
        ctx.User 
        |> ClaimsPrincipal.getUser
        |> Result.toOption
        |> Option.map (fun u -> u.Name)
    let fullPageHtml (env: #ITemplateBuilder) page content ctx =
        content
        |> Seq.singleton
        |> env.Templates.FullPage (getUsername ctx) page
        |> Controller.html ctx

    let fullPageHtmlMultiple (env: #ITemplateBuilder) page content (ctx: HttpContext) =
        content
        |> env.Templates.FullPage (getUsername ctx) page
        |> Controller.html ctx
        
    let fragment (env: #ITemplateBuilder) content ctx =
        content
        |> env.Templates.Fragment
        |> Controller.html ctx
        
[<RequireQualifiedAccess>]
type ApiFormResponse =
    | Redirect of string
    | TurboStream of TurboStream list
    
   
type HttpContext with

    member this.GetCurrentUser() =
        this.User |> ClaimsPrincipal.getUser
    member this.SetUser(user: User) =
        this.Session.SetString(HttpContext.userKey, user |> User.serialize)
        
    member this.ClearUsername() =
        this.Session.Remove(HttpContext.userKey)
    
    member ctx.RespondWithHtmlFragment (env, content) =
        ApiResultHelpers.fragment env content ctx
    member ctx.RespondWithHtmlFragment (env, contentTaskResult) =
        contentTaskResult
        |> Task.bind (ApiResultHelpers.handleResult ctx (ApiResultHelpers.fragment env))
    member ctx.RespondWithHtml (env, page, content) =
        ApiResultHelpers.fullPageHtml env page content ctx
    member ctx.RespondWithHtml (env, page, contentResult) =
        contentResult
        |> ApiResultHelpers.handleResult ctx (ApiResultHelpers.fullPageHtml env page)
    member ctx.RespondWithHtml (env, page, contentTaskResult) =
        contentTaskResult
        |> Task.bind (ApiResultHelpers.handleResult ctx (ApiResultHelpers.fullPageHtml env page))
    member ctx.RespondWithHtml (env, page, content) =
        ApiResultHelpers.fullPageHtmlMultiple env page content ctx
    member ctx.RespondWithHtml (env, page, contentResult) =
        contentResult
        |> ApiResultHelpers.handleResult ctx (ApiResultHelpers.fullPageHtmlMultiple env page)
    member ctx.RespondWithHtml (env, page, contentTaskResult) =
        contentTaskResult
        |> Task.bind (ApiResultHelpers.handleResult ctx (ApiResultHelpers.fullPageHtmlMultiple env page))
    
    member ctx.RespondWithRedirect(location) = Turbo.redirect location ctx
    member ctx.RespondWithRedirect(locationResult) =
        locationResult 
        |> ApiResultHelpers.handleResult ctx (Turbo.redirect)
    member ctx.RespondWithRedirect(locationTaskResult) =
        locationTaskResult
        |> Task.bind (ApiResultHelpers.handleResult ctx (Turbo.redirect))  
    
    member ctx.RespondWithTurboStream ts = TurboStream.writeTurboStreamContent 200 ts ctx
    member ctx.RespondWithTurboStream tsResult =
        tsResult
        |> ApiResultHelpers.handleResult ctx (TurboStream.writeTurboStreamContent 200)
    member ctx.RespondWithTurboStream tsTaskResult =
        tsTaskResult
        |> Task.bind (ApiResultHelpers.handleResult ctx (TurboStream.writeTurboStreamContent 200))
    
    member ctx.Respond formTaskResult =
        let handleFormResponse value ctx =
            match value with
            | ApiFormResponse.Redirect location -> Turbo.redirect location ctx
            | ApiFormResponse.TurboStream ts -> TurboStream.writeTurboStreamContent 200 ts ctx
            
        formTaskResult
        |> Task.bind (ApiResultHelpers.handleResult ctx handleFormResponse)
        
        
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

module TurboStream =
    let mergeByTargetId okInputs errors : seq<TurboStream> =
        okInputs @ errors 
        |> List.map (fun (ts: TurboStream) -> ts.TargetId, ts) 
        |> Map.ofList 
        |> Map.values :> _

        

module Async =
    let map f xAsync =
        async {
            let! x = xAsync
            return f x
        }

module Giraffe =
    module ViewEngine =
        open Giraffe.ViewEngine
        let _dataGameNightId = attr "data-game-night-id"
        let _addVoteButton = flag "data-add-vote-button"
        let _removeVoteButton = flag "data-remove-vote-button"
        let _dataGameName = attr "data-game-name"
        let _dataDate = attr "data-date"

module Option =
    let ofString str =
        if String.IsNullOrWhiteSpace str then
            None
        else 
            Some str
            
module Set =
    
    let tryFind predicate set =
        set
        |> Set.toList
        |> List.tryFind predicate
            