module Backend.Api.ConfirmedGameNight

open Giraffe
open FSharpPlus.Data
open Saturn
open FsToolkit.ErrorHandling
open Backend
open Feliz.Bulma.ViewEngine
open Domain
open Feliz.ViewEngine
open Backend.Api.Shared
open FsHotWire.Feliz
open FSharp.UMX
    
    
let confirmedGameNightCard currentUser (gn: ConfirmedGameNight) =
    let turboFrameId = "confirmed-game-night-" + gn.Id.ToString()
    Html.turboFrame [
        prop.id turboFrameId 
        prop.children [
            Bulma.card [
                prop.classes [ "mb-5" ]
                prop.dataGameNightId gn.Id
                prop.children [
                    Bulma.cardHeader [
                        Bulma.cardHeaderTitle.p (%gn.CreatedBy + " wants to play")
                    ]
                    Bulma.cardContent [
                        for gameName, votes in gn.GameVotes |> NonEmptyMap.toList do
                            let actionUrl = sprintf "/proposedgamenight/%s/game/%s/vote" (gn.Id.ToString()) %gameName
                            Html.unorderedList [
                                Html.listItem [
                                    GameNightViews.gameCard gameName votes currentUser actionUrl turboFrameId
                                ] 
                            ] 
                        let actionUrl = sprintf "/proposedgamenight/%s/game/%s/vote" (gn.Id.ToString()) gn.Date.AsString
                        GameNightViews.dateCard gn.Date (gn.Players |> NonEmptySet.toSet) currentUser actionUrl turboFrameId
                    ]
                ]
            ]
        ]
    ]
    
let gameNightsView currentUser confirmed =
    Html.turboFrame [
        prop.id "confirmed-game-nights"
        prop.children [
            Bulma.container [
                Bulma.title.h2 "Confirmed game nights"
                Bulma.section [
                    for gameNight in confirmed do confirmedGameNightCard currentUser gameNight
                ]
            ]
        ]
    ]

let getAll env : HttpFunc =
    fun ctx -> 
        taskResult {
            let! confirmed = Storage.getAllConfirmedGameNights env
            let! currentUser = ctx.GetUser() |> Result.mapError ApiError.MissingUser
            return gameNightsView currentUser confirmed 
        } 
        |> (fun view -> ctx.RespondWithHtml(env, view))
        
let controller env = controller {
    
    index (getAll env)
}
