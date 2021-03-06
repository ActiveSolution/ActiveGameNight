module Domain.Workflows.GameNights

open System
open FsToolkit.ErrorHandling
open Domain
open FSharpPlus.Data
open FSharp.UMX


type CreateProposedGameNightRequest =
    { Games : NonEmptySet<Guid<GameId>>
      Dates : NonEmptySet<DateTime<FutureDate>>
      CreatedBy : string<CanonizedUsername> }
type CreateProposedGameNight = CreateProposedGameNightRequest -> ProposedGameNight

type UpdateProposedGameNightRequest =
    { Games : NonEmptySet<Guid<GameId>>
      Dates : NonEmptySet<DateTime<FutureDate>> 
      OriginalGameNight : ProposedGameNight }
type UpdateProposedGameNight = UpdateProposedGameNightRequest -> ProposedGameNight

type GameVoteRequest =
    { GameNight : ProposedGameNight
      GameId : Guid<GameId>
      User : string<CanonizedUsername> }
type DateVoteRequest =
    { GameNight : ProposedGameNight
      Date : DateTime 
      User : string<CanonizedUsername> }
type AddGameVote = GameVoteRequest -> ProposedGameNight
type AddDateVote = DateVoteRequest -> ProposedGameNight
type RemoveGameVote = GameVoteRequest -> ProposedGameNight
type RemoveDateVote = DateVoteRequest -> ProposedGameNight

type ConfirmGameNightResult =
    | Confirmed of ConfirmedGameNight
    | Cancelled of CancelledGameNight

type ConfirmGameNight = ProposedGameNight -> ConfirmGameNightResult

module ProposeGameNightRequest =
    let create(games, dates, createdBy) =
        if games |> List.length < 1 then
            "Must provide at least one game" |> Error
        elif dates |> List.length < 1 then
            "Must provide at least one date" |> Error
        else
            result {
                let! gameIds =
                    games
                   |> List.map GameId.create
                   |> List.sequenceResultM
                let! dates =
                    dates
                    |> List.choose (fun s -> if String.IsNullOrWhiteSpace s then None else Some s)
                    |> List.map FutureDate.tryParse
                    |> List.sequenceResultM
                    
                return
                    { CreateProposedGameNightRequest.Games = NonEmptySet.create gameIds.Head gameIds.Tail
                      Dates = NonEmptySet.create dates.Head dates.Tail
                      CreatedBy = createdBy } 
            }

module GameVoteRequest =
    let create(gameNight, gameId, user) =
        { GameNight = gameNight
          GameId = gameId
          User = user }
          
module DateVoteRequest =
    let create(gameNight, date, user) =
        { GameNight = gameNight
          Date = date
          User = user }

let createProposedGameNight : CreateProposedGameNight =
    fun req ->
        let games =
            req.Games
            |> NonEmptySet.map (fun g -> g, Set.empty)
            |> NonEmptyMap.ofSeq
        let dates =
            req.Dates
            |> NonEmptySet.map (fun date -> % date, Set.empty)
            |> NonEmptyMap.ofSeq
            
        { ProposedGameNight.Id = GameNightId.newId()
          GameVotes = games
          DateVotes = dates
          CreatedBy = req.CreatedBy }
let updateProposedGameNight : UpdateProposedGameNight = 
    fun req ->
        let games =
            req.Games
            |> NonEmptySet.map (fun g -> g, Set.empty)
            |> NonEmptyMap.ofSeq
        let dates =
            req.Dates
            |> NonEmptySet.map (fun date -> % date, Set.empty)
            |> NonEmptyMap.ofSeq
            
        { req.OriginalGameNight with GameVotes = games 
                                     DateVotes = dates }


let addGameVote : AddGameVote =
    fun req ->
        let newVotes = 
            req.GameNight.GameVotes 
            |> NonEmptyMap.tryFind req.GameId 
            |> Option.defaultValue Set.empty 
            |> Set.add req.User
        let newGames =
            req.GameNight.GameVotes |> NonEmptyMap.add req.GameId newVotes
        { req.GameNight with GameVotes = newGames}
        
let removeGameVote : RemoveGameVote =
    fun req ->
        let newVotes = 
            req.GameNight.GameVotes 
            |> NonEmptyMap.tryFind req.GameId 
            |> Option.defaultValue Set.empty 
            |> Set.remove req.User
        let newGames =
            req.GameNight.GameVotes |> NonEmptyMap.add req.GameId newVotes
        { req.GameNight with GameVotes = newGames}
        
let addDateVote : AddDateVote =
    fun req ->
        let newVotes = 
            req.GameNight.DateVotes
            |> NonEmptyMap.tryFind req.Date
            |> Option.defaultValue Set.empty 
            |> Set.add req.User
        let newDates =
            req.GameNight.DateVotes |> NonEmptyMap.add req.Date newVotes
        { req.GameNight with DateVotes = newDates }
        
let removeDateVote : RemoveDateVote =
    fun req ->
        let newVotes = 
            req.GameNight.DateVotes
            |> NonEmptyMap.tryFind req.Date
            |> Option.defaultValue Set.empty 
            |> Set.remove req.User
        let newDates =
            req.GameNight.DateVotes |> NonEmptyMap.add req.Date newVotes
        { req.GameNight with DateVotes = newDates }
        
let confirmGameNight : ConfirmGameNight =
    fun proposed ->
        let mostVotedDate, players =
            proposed.DateVotes
            |> NonEmptyMap.toList
            |> List.maxBy (fun (_, votes) -> votes.Count)
        
        if players.Count < 2 then
            Cancelled
                { CancelledGameNight.Id = proposed.Id
                  DateVotes = proposed.DateVotes
                  GameVotes = proposed.GameVotes
                  CreatedBy = proposed.CreatedBy }
        else
            Confirmed
                { ConfirmedGameNight.Id = proposed.Id
                  ConfirmedGameNight.Date = mostVotedDate
                  ConfirmedGameNight.Players = NonEmptySet.ofSet players
                  ConfirmedGameNight.GameVotes = proposed.GameVotes
                  ConfirmedGameNight.CreatedBy = proposed.CreatedBy }
                