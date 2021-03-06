[<RequireQualifiedAccess>]
module Backend.Api.Shared.GameNightViews

open Backend.Extensions
open Giraffe.ViewEngine
open Backend.Api.Shared
open Domain
open FsHotWire.Giraffe
open Backend.Extensions
open FSharp.UMX
open System

let addVoteButton addVoteUrl target =
    div [ _class "level-item" ] [
        form [ 
            _targetTurboFrame target 
            _method "POST"
            _action addVoteUrl
        ] [
            div [ _class "field" ] [
                div [ _class "control" ] [
                    button [
                        Stimulus.controller "css-class"
                        Stimulus.cssClass { Controller = "css-class" ; ClassName = "name"; ClassValue = "is-loading" }
                        Stimulus.action { DomEvent = "click"; Controller = "css-class"; Action = "addClass" } 
                        _addVoteButton 
                        _class "button is-primary is-small"
                        _type "submit"
                    ] [ 
                        "add vote" |> str 
                    ]
                ]
            ]
        ]
    ]
    
let removeVoteButton removeVoteUrl (user: string<CanonizedUsername>) target =

    div [ _class "level-item" ]
        [
            form 
                [ 
                    _targetTurboFrame target 
                    _method "POST"
                    _action (removeVoteUrl + "/" + %user)
            ] [
                input [
                    _type "hidden"
                    _name "_method"
                    _value "delete"
                ]
                div [ _class "field" ] [
                    div [ _class "control" ] [
                        button [
                            Stimulus.controllers [ "css-class"; "remove-vote-button" ]
                            Stimulus.cssClass { Controller = "css-class"; ClassName = "name"; ClassValue = "is-loading" }
                            Stimulus.value { Controller = "remove-vote-button"; ValueName = "text"; Value = User.toDisplayName user }
                            Stimulus.actions 
                                [ { DomEvent = "click"; Controller = "css-class"; Action = "addClass" }
                                  { DomEvent = "mouseenter"; Controller = "remove-vote-button"; Action = "setActive" } 
                                  { DomEvent = "mouseleave"; Controller = "remove-vote-button"; Action = "setInactive" } ]
                            _removeVoteButton 
                            _class "button is-info is-small"
                            _type "submit"
                        ] [ user |> User.toDisplayName |> str ]
                    ]
                ]
            ]
        ]
    
let otherUsersVoteButton id (user: string<CanonizedUsername>) =
    div [
        _id id
        _class "level-item" 
    ] [ 
        div [ _class "field"] [
            div [ _class "control" ] [ 
                button [ 
                    _class "button is-small is-primary no-hover" 
                    _disabled
                ] [ user |> User.toDisplayName |> str ]
            ]
        ]
    ]
    
let gameVoteButtons (gnId: Guid<GameNightId>) (gameId: Guid<GameId>) (currentUser: string<CanonizedUsername>) votes removeVoteUrl target = [
    for user in Set.toList votes do
        if user = currentUser then
            removeVoteButton removeVoteUrl currentUser target 
        else
            let id = sprintf "proposed-game-night-game-vote-%s-%s-%s" (gnId.ToString()) (gameId.ToString()) %user
            otherUsersVoteButton id user
    ]
    
let private dateVoteButtons (gnId: Guid<GameNightId>) (date:DateTime) (currentUser: string<CanonizedUsername>) votes removeVoteUrl target = [
    for user in Set.toList votes do
        if user = currentUser then
            removeVoteButton removeVoteUrl currentUser target
        else
            let id = sprintf "proposed-game-night-date-vote-%s-%s-%s" (gnId.ToString()) date.AsString %user
            otherUsersVoteButton id user
    ]
    
let hasVoted votes user =
    votes
    |> Set.contains user
    

let dateCard (gnId: Guid<GameNightId>) (date: DateTime) votes currentUser actionUrl voteUpdateTarget =
    article [
        _class "media"
        _dataDate date.AsString
    ] [
        figure [ _class "media-left" ] [ 
            p [ _class "image is-64x64" ] [ 
                img [ _src "/Images/calendar.jpg" ]  
            ] 
        ]
        div [ _class "media-content" ] [
            p [] [ date.AsString |> str ]
            nav [ _class "level" ] [ 
                div [ 
                    _id (sprintf "proposed-game-night-date-votes-%s-%s" (gnId.ToString()) date.AsString)
                    _class "level-left" 
                ] [
                    yield! dateVoteButtons gnId date currentUser votes actionUrl voteUpdateTarget
                    if hasVoted votes currentUser then
                        ()
                    else 
                        addVoteButton actionUrl voteUpdateTarget
                ]
            ]
        ]
    ]