module Backend.Api.Game

open System
open Domain
open FsHotWire.Giraffe
open Giraffe
open Infrastructure
open Saturn
open Backend.Extensions
open Backend
open FSharp.UMX
open FsToolkit.ErrorHandling
open Backend.Api.Shared
open Backend.Validation
open Microsoft.AspNetCore.Http
open Domain

module Metadata =
    open Inputs

    let name = Metadata.create("game-name-input", "Name (*)", "Name")
    let imageUrl = Metadata.create("game-image-input", "Thumbnail image url", "ImageUrl")
    let link = Metadata.create ("game-link-input", "External link", "Link")
    let numberOfPlayers = Metadata.create ("game-number-of-players", "Number of players", "NumberOfPlayers")
    let notes = Metadata.create ("game-notes-input", "Notes", "Notes")


[<CLIMutable>]
type CreateGameForm =
    { Name : string
      NumberOfPlayers : string
      Link : string
      ImageUrl : string
      Notes : string }
    with 
        member private this.OkInputs(submitButtonId) = 
            [ Inputs.okTextInput Metadata.name this.Name |> TurboStream.replace Metadata.name.Id
              Inputs.okTextInput Metadata.imageUrl this.ImageUrl |> TurboStream.replace Metadata.imageUrl.Id
              Inputs.okTextInput Metadata.link this.Link |> TurboStream.replace Metadata.link.Id
              Inputs.okTextInput Metadata.numberOfPlayers this.NumberOfPlayers |> TurboStream.replace Metadata.numberOfPlayers.Id
              Inputs.okTextareaInput Metadata.notes this.Notes |> TurboStream.replace Metadata.notes.Id ]
              |> List.append [ TurboStream.replace submitButtonId (Partials.loadingButton submitButtonId "Save") ]
        member this.CreateFormValidationError(errors) =

            TurboStream.mergeByTargetId (this.OkInputs("add-game-submit-button")) errors
            |> FormValidationError 
        member this.EditFormValidationError (gameId: Guid<GameId>) errors =
            TurboStream.mergeByTargetId (this.OkInputs(sprintf "edit-game-%A-submit-button" gameId)) errors
            |> FormValidationError 

module private Views =
    open Giraffe.ViewEngine
    
    let gameNotFound =
        div [ _class "box" ] [ str "Game not found" ]

    let gameCard isInline (game: Game option) =
        match game with
        | None -> gameNotFound
        | Some game ->
            let imageStr = game.ImageUrl |> Option.map (fun x -> x.ToString()) |> Option.defaultValue "http://via.placeholder.com/64"
            let notes = match game.Notes with Some n -> str n | None -> emptyText
            let numPlayers = match game.NumberOfPlayers with Some num -> str ("Number of players: " + num) | None -> emptyText
            let link = match game.Link with Some l -> a [ _href l; _target "_blank" ] [ str l ] | None -> emptyText

            turboFrame [ _id ("game-" + %game.Id.ToString()) ] [
                div [ _class "box mb-5" ] [
                    article [ 
                        _class "media" 
                        _dataGameName %game.Name
                    ] [
                        figure [ _class "media-left" ] [ 
                            p [ _class "image is-64x64" ] [ img [ _src imageStr ]  ] 
                        ]
                        div [ _class "media-content" ] [
                            p [] [ strong [] [ str (GameName.toDisplayName game.Name) ] ]
                            p [] [ numPlayers ]
                            p [] [ i [] [ notes ] ]
                            p [] [ link ]
                            a [ 
                                _class "button is-primary is-small" 
                                _href (sprintf "/proposedgamenight/add?game=%A" %game.Id)
                                _targetTurboFrame "content"
                            ] [ 
                                str "I wanna play this!"
                            ]
                        ]
                        div [ _class "media-right" ] [ 
                            a [ 
                                _href (sprintf "/game/%A/edit?inline=%b" game.Id isInline) 
                                if not isInline then _targetTurboFrame "_top"
                            ] [ str "edit" ] 
                        ]
                    ]
                ]
            ]

    let showGameView isInline (game: Game option) =
        section [ _class "section" ] [
            div [ _class "container" ] [
                gameCard isInline game
            ]
        ]

    let addGameLink isInline =
        div [ ] [
            turboFrame 
                [ _id "add-game" ] [
                    a [ 
                        _id "add-game-link"
                        _href (sprintf "/game/add?inline=%b" isInline) 
                    ] [ 
                        Icons.plusIcon
                        str "Add new game"
                    ]
                ]
            ]

    let games (games: seq<Game>) =
        match games |> Seq.toList with
        | [] ->
            section [ _class "section"] [
                div [ _class "container" ] [
                    h2 [ _class "title is-2" ] [ str "No games" ]
                    addGameLink false
                ]
            ]
        | games ->
            turboFrame [ _id "games"] [ 
                section [ _class "section"] [ 
                    div [ _class "container"] [ 
                        for game in games do gameCard true (Some game)
                        addGameLink true
                    ]
                ]
            ]

    let addGameView isInline =
        // let target = if isInline then "games" else "_top"
        section [ _class "section" ] [
            div [ _class "container" ] [
                h2 [ _class "title is-2" ] [ str "Add a new game"]
                turboFrame [ _id "add-game"; _autoscroll ] [ 
                    form [
                        _class "box"
                        _method "POST"
                        _action "/game" 
                        _targetTurboFrame (if isInline then "games" else "_top")
                    ] [
                        Inputs.textInput Metadata.name None
                        Inputs.textInput Metadata.imageUrl None
                        Inputs.textInput Metadata.link None
                        Inputs.textInput Metadata.numberOfPlayers None
                        Inputs.textareaInput Metadata.notes None

                        div [ _class "field" ] [
                            div [ _class "control" ] [
                                if isInline then 
                                    Partials.submitButtonWithCancel "add-game-submit-button" "Save" "Cancel" "/fragments/game/addgamelink" (if isInline then "add-game" else "_top") 
                                else
                                    Partials.submitButton "add-game-submit-button" "Save"
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let editGameView isInline (game: Game option) =
        match game with
        | None -> gameNotFound
        | Some game ->
            let id = "game-" + %game.Id.ToString()
            let target = if isInline then id else "_top"
            section [ _class "section" ] [
                div [ _class "container" ] [
                    h2 [ _class "title is-2" ] [ str "Edit game"]
                    turboFrame [ _id id; _autoscroll; _target target ] [ 
                        article [ _class "box media mb-5" ] [
                            div [ _class "media-content" ] [
                                form [
                                    _method "POST"
                                    _action (sprintf "/game/%A/edit" game.Id) 
                                ] [
                                    Inputs.textInput Metadata.name (game.Name |> GameName.toDisplayName |> Some)
                                    Inputs.textInput Metadata.imageUrl game.ImageUrl
                                    Inputs.textInput Metadata.link game.Link
                                    Inputs.textInput Metadata.numberOfPlayers game.NumberOfPlayers
                                    Inputs.textareaInput Metadata.notes game.Notes

                                    div [ _class "field" ] [
                                        div [ _class "control" ] [
                                            Partials.submitButtonWithCancel (sprintf "edit-game-%A-submit-button" %game.Id) "Save" "Cancel" (sprintf "/game/%A?inline=%b" game.Id isInline) target
                                        ]
                                    ]
                                ]
                            ]
                            div [ _class "media-right" ] [
                                a [ 
                                      _href (sprintf "/game/%A?inline=%b" game.Id isInline) 
                                      if not isInline then _targetTurboFrame "_top"
                                ] [ 
                                    span [ _class "icon" ] [ 
                                        i [ _class "fas fa-times-circle fa-lg has-text-grey-lighter" ] [ ] 
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

module private Validation =
    let private validateGameName existing gameName : Result<string<CanonizedGameName>, TurboStream list> =
        let validateDuplicate onError items item = if Seq.contains item items then Error onError else Ok item
        gameName
        |> Option.ofString
        |> Result.requireSome "Game name missing"
        |> Result.bind GameName.create
        |> Result.bind (validateDuplicate "A game with this name already exists" existing)
        |> Result.mapError (fun err -> 
            Inputs.errorTextInput Metadata.name gameName err 
            |> TurboStream.replace Metadata.name.Id 
            |> List.singleton)

    let private tryParseUrl errorMsg u = 
        match Uri.TryCreate(u, UriKind.Absolute) with
        | true, _ -> Ok u
        | false, _ -> Error errorMsg

    let private validateLink link : Result<string option, TurboStream list> =
        link
        |> Option.ofString
        |> function 
        | None -> Ok None
        | Some link ->
            link
            |> (tryParseUrl "Not a valid link" >> Result.map Some)
            |> (Result.mapError (fun err ->
                Inputs.errorTextInput Metadata.link link err 
                |> TurboStream.replace Metadata.link.Id 
                |> List.singleton))

    let private validateImageUrl imageUrl : Result<string option, TurboStream list> =
        imageUrl
        |> Option.ofString
        |> function 
        | None -> Ok None
        | Some imageUrl ->
            imageUrl
            |> (tryParseUrl "Not a valid imageUrl" >> Result.map Some)
            |> (Result.mapError (fun err ->
                Inputs.errorTextInput Metadata.imageUrl imageUrl err 
                |> TurboStream.replace Metadata.imageUrl.Id 
                |> List.singleton))

    open Workflows.Game
    let validateCreateGameForm gameId user existingGameNames (form: CreateGameForm) : Result<Workflows.Game.AddGameRequest, ApiError> =
            
        validation {
            let! gameName = validateGameName existingGameNames form.Name
            and! link = validateLink form.Link
            and! imageUrl = validateImageUrl form.ImageUrl
            return 
                { Workflows.Game.AddGameRequest.Id = gameId
                  GameName = gameName
                  CreatedBy = user
                  ImageUrl = imageUrl
                  Link = link
                  Notes = form.Notes |> Option.ofString
                  NumberOfPlayers = form.NumberOfPlayers |> Option.ofString
                  ExistingGames = existingGameNames }
        }
        |> Result.mapError form.CreateFormValidationError

    let validateUpdateGameForm gameId existingGameNames user (form: CreateGameForm) : Result<Workflows.Game.UpdateGameRequest, ApiError> =
            
        validation {
            let! gameName = validateGameName [] form.Name
            and! link = validateLink form.Link
            and! imageUrl = validateImageUrl form.ImageUrl
            return 
                { Workflows.Game.UpdateGameRequest.Id = gameId
                  GameName = gameName
                  CreatedBy = user
                  ImageUrl = imageUrl
                  Link = link
                  Notes = form.Notes |> Option.ofString
                  NumberOfPlayers = form.NumberOfPlayers |> Option.ofString }
        }
        |> Result.mapError (form.EditFormValidationError gameId)

let getAll env : HttpFunc =
    fun ctx -> 
        taskResult {
            let! games = Storage.Games.getAllGames env
            return Views.games games 
        }
        |> (fun view -> ctx.RespondWithHtml(env, Page.Games, view))

let showGame env (ctx: HttpContext) gameIdStr =
    taskResult {
        let isInline = 
            ctx.TryGetQueryStringValue "inline" 
            |> Option.bind bool.tryParse 
            |> Option.defaultValue false
        let! gameId = GameId.parse gameIdStr |> Result.mapError ApiError.BadRequest
        let! game = Storage.Games.getGame env gameId
        return Views.showGameView isInline game
    }
    |> (fun view -> ctx.RespondWithHtml(env, Page.Games, view))

let addGame env : HttpFunc =
    fun ctx ->
        let isInline = 
            ctx.TryGetQueryStringValue "inline" 
            |> Option.bind bool.tryParse 
            |> Option.defaultValue false
        ctx.RespondWithHtml(env, Page.Games, Views.addGameView isInline)

let editGame env (ctx: HttpContext) gameIdStr =
    taskResult {
        let isInline = 
            ctx.TryGetQueryStringValue "inline" 
            |> Option.bind bool.tryParse 
            |> Option.defaultValue false
        let! gameId = GameId.parse gameIdStr |> Result.mapError ApiError.BadRequest
        let! game = Storage.Games.getGame env gameId
        return Views.editGameView isInline game 
    }
    |> (fun view -> ctx.RespondWithHtml(env, Page.Games, view))

let saveGame env (ctx: HttpContext): HttpFuncResult =
    taskResult {
        let! form = ctx.BindFormAsync<CreateGameForm>()
        let! user = ctx.GetCurrentUser() |> Result.mapError ApiError.MissingUser
        let! existingGames = Storage.Games.getAllGames env |> Async.map (Set.ofSeq >>Set.map (fun x -> x.Name))
        let gameId = GameId.newId() 
        let! request = Validation.validateCreateGameForm gameId user.Name existingGames form
        let! game = Workflows.Game.addGame request |> Result.mapError ApiError.BadRequest
        let! _ = Storage.Games.addGame env game
        return "/game"
    }
    |> ctx.RespondWithRedirect

let updateGame env (ctx: HttpContext) gameIdStr =
    taskResult {
        let! gameId = GameId.parse gameIdStr |> Result.mapError ApiError.BadRequest 
        let! form = ctx.BindFormAsync<CreateGameForm>()
        let! user = ctx.GetCurrentUser() |> Result.mapError ApiError.MissingUser
        let! existingGames = Storage.Games.getAllGames env 
        let existingGameNames = existingGames |> Set.ofSeq |> Set.map (fun x -> x.Name)
        let! request = Validation.validateUpdateGameForm gameId existingGameNames user.Name form 
        let game = Workflows.Game.updateGame request 
        let! _ = Storage.Games.addGame env game

        return "/game"
    }
    |> ctx.RespondWithRedirect


let controller env = controller {
    plug [ All ] CommonHttpHandlers.requireUsername
    plug [ Add ] (CommonHttpHandlers.privateCachingWithQueries (TimeSpan.FromHours 24.) [| "*" |])
    
    index (getAll env)
    show (showGame env)
    add (addGame env)
    create (saveGame env)
    edit (editGame env)
    update (updateGame env)
}

module Fragments =
    let addGameLinkFragment env : HttpHandler =
        fun _ ctx -> 
            ctx.RespondWithHtmlFragment(env, Views.addGameLink true)
