module Backend.Startup

open System
open Giraffe
open Microsoft.AspNetCore.Http
open Saturn
open Microsoft.Extensions.Logging
        
let endpointPipe =
    pipeline {
        plug putSecureBrowserHeaders
        plug fetchSession
        plug head
    }
    
let rewriteHttpMethod : HttpHandler =
    fun next (ctx: HttpContext) ->
        match ctx.GetFormValue("_method") with
        | Some method when method = "delete" ->
            ctx.Request.Method <- "delete"
        | Some method when method = "put" ->
            ctx.Request.Method <- "put"
        | _ -> ()
        next ctx

let browserRouter =
    router {
        pipe_through endpointPipe
        forward "" CompositionRoot.Api.gameNightController
        forward "/user" CompositionRoot.Api.userController
        forward "/confirmedgamenight" CompositionRoot.Api.confirmedGameNightController
        get "/proposedgamenight/fragment/addgame" CompositionRoot.Api.addGameInputFragment
        get "/proposedgamenight/fragment/adddate" CompositionRoot.Api.addDateInputFragment
        forward "/proposedgamenight" CompositionRoot.Api.proposedGameNightController
        forward "/gamenight" CompositionRoot.Api.gameNightController
        get "/navbar" CompositionRoot.Api.navbarPage
        get "/version" CompositionRoot.Api.versionPage
        get "/about" CompositionRoot.Api.versionPage
    }
    
let notFoundHandler : HttpHandler =
    setStatusCode 404 >=> text "Not found"
        
let topRouter =
    router {
        pipe_through rewriteHttpMethod
        forward "" browserRouter
    }
        
let webApp =
    choose [ topRouter
             notFoundHandler ]

let errorHandler: ErrorHandler =
    fun exn logger _next ctx ->
        match exn with
        | :? ArgumentException as a -> Response.badRequest ctx a.Message
        | _ ->
            let msg =
                sprintf "Exception for %s%s" ctx.Request.Path.Value ctx.Request.QueryString.Value

            logger.LogError(exn, msg)
            Response.internalError ctx ()
            
            
let configureLogging (log:ILoggingBuilder) =
    log.ClearProviders() |> ignore
    log.AddConsole() |> ignore
    
application {
    url ("http://*:" + CompositionRoot.config.ServerPort.ToString() + "/")
    error_handler errorHandler
    pipe_through endpointPipe
    use_router webApp
    use_gzip
    logging configureLogging
    memory_cache
    use_static CompositionRoot.config.PublicPath
}
|> run
