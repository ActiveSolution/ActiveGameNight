[<RequireQualifiedAccess>]
module Backend.Api.Shared.GameNight

open Feliz.Bulma.ViewEngine
open Feliz.ViewEngine
open Backend.Api.Shared
open Domain
open FsHotWire
open FsHotWire.Feliz

let addVoteButton addVoteUrl (target: TurboFrameId) =
    Bulma.levelItem [
        Html.form [
            prop.targetTurboFrame target
            prop.method "POST"
            prop.action addVoteUrl
            prop.children [
                Partials.fieldControl [
                    Bulma.button.button [
                        color.isPrimary
                        prop.type'.submit
                        button.isSmall
                        prop.text "add vote"
                    ]
                ]
            ]
        ]
    ]
    
    
let removeVoteButton removeVoteUrl (user: User) (target: TurboFrameId) =
    Bulma.levelItem [
        Html.form [
            prop.targetTurboFrame target
            prop.method "POST"
            prop.action (removeVoteUrl + "/" + user.Canonized)
            prop.children [
                Html.input [
                    prop.type'.hidden
                    prop.name "_method"
                    prop.value "delete"
                ]
                Partials.fieldControl [    
                    Bulma.button.button [
                        color.isInfo
                        prop.type'.submit
                        prop.custom ("onmouseover","this.style.backgroundColor='#feecf0';this.style.color='#cc0f35';")
                        prop.custom ("onmouseout","this.style.backgroundColor='#3298dc';this.style.color='white'")
                        button.isSmall
                        prop.text user.Val
                    ]
                ]
            ]
        ]
    ]
    
let otherUsersVoteButton (user: User) =
    Bulma.levelItem [ 
        Partials.fieldControl [    
            Bulma.button.button [
                prop.classes [ "no-hover" ]
                color.isPrimary
                button.isSmall
                prop.text user.Val
            ]
        ]
    ]
    
let gameVoteButtons (currentUser: User) votes removeVoteUrl target = [
    for user in Set.toList votes do
        if user = currentUser then
            removeVoteButton removeVoteUrl currentUser target 
        else
            otherUsersVoteButton user
    ]
    
let private dateVoteButtons (currentUser: User) votes removeVoteUrl target = [
    for (user: User) in Set.toList votes do
        if user = currentUser then
            removeVoteButton removeVoteUrl currentUser target
        else
            otherUsersVoteButton user
    ]
    
let private hasVoted votes user =
    votes
    |> Set.contains user
    
let gameCard (gameName: GameName) votes currentUser actionUrl voteUpdateTarget =
    Bulma.media [
        Bulma.mediaLeft [
            Bulma.image [
                image.is64x64
                prop.children [
                    Html.img [
                        prop.src "http://via.placeholder.com/64"
                    ]
                ]
            ]
        ]
        Bulma.mediaContent [
            Bulma.content [
                Html.p gameName.Val
            ]
            Bulma.level [
                Bulma.levelLeft [
                    yield! gameVoteButtons currentUser votes actionUrl voteUpdateTarget 
                    if hasVoted votes currentUser then
                        Html.none
                    else
                        addVoteButton actionUrl voteUpdateTarget
                ]
            ]
        ]
    ]

let dateCard date votes currentUser actionUrl voteUpdateTarget =
    Bulma.media [
        Bulma.mediaLeft [
            Bulma.image [
                image.is64x64
                prop.children [
                    Html.img [
                        prop.src "/Images/calendar.jpg"
                    ]
                ]
            ]
        ]
        Bulma.mediaContent [
            Bulma.content [
                Html.p (date |> Date.asString)
            ]
            Bulma.level [
                Bulma.levelLeft [
                    yield! dateVoteButtons currentUser votes actionUrl voteUpdateTarget
                    if hasVoted votes currentUser then
                        Html.none
                    else
                        addVoteButton actionUrl voteUpdateTarget 
                ]
            ]
        ]
    ]
