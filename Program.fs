module Giraffarig.App

open System
open System.IO
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Cors.Infrastructure
open Giraffe
open Handlers.Auth
open Handlers.Users
open Giraffe.Serialization
open Thoth.Json.Giraffe

let authorize =
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

// ---------------------------------
// Web app
// ---------------------------------
let webApp =
    choose
        [ GET
          >=> choose
                  [ route "/" >=> text "Hello world"
                    route "/api/users" >=> authorize >=> getUsers ]
          POST
          >=> choose
                  [ route "/api/auth/login" >=> login
                    route "/api/auth/signup" >=> signup ]
          setStatusCode 404 >=> text "Not Found" ]

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse
    >=> setStatusCode 500
    >=> text ex.Message
// ---------------------------------
// Config and Main
// ---------------------------------
let configureDevCORS (builder: CorsPolicyBuilder) =
    builder
        .WithOrigins("http://localhost:8080")
        .AllowAnyMethod()
        .AllowAnyHeader()
        |> ignore
// add your production CORS builder

let configureApp (app: IApplicationBuilder) =
    let env =
        app.ApplicationServices.GetService<IWebHostEnvironment>()

    
    let _app =
            match env.EnvironmentName with
            | "Development" -> 
               app
                   .UseDeveloperExceptionPage()
                   .UseCors(configureDevCORS)
            | _ -> 
               app
                   .UseGiraffeErrorHandler errorHandler
                   //.UseCors(configureProdCORS)
    _app
        .UseAuthentication()
        .UseHttpsRedirection()
        .UseStaticFiles()
        .UseGiraffe(webApp)

let authenticationOptions (o: AuthenticationOptions) =
    o.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
    o.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme

let jwtBearerOptions (cfg: JwtBearerOptions) =
    cfg.RequireHttpsMetadata <- false
    cfg.SaveToken <- true
    cfg.IncludeErrorDetails <- true
    cfg.TokenValidationParameters <- TokenValidationParameters()
    cfg.TokenValidationParameters.ValidateIssuerSigningKey <- true
    cfg.TokenValidationParameters.IssuerSigningKey <- Helpers.getSimmetricKey
    cfg.TokenValidationParameters.ValidateActor <- false
    cfg.TokenValidationParameters.ValidateIssuer <- false
    cfg.TokenValidationParameters.ValidateAudience <- false
    cfg.TokenValidationParameters.ValidateLifetime <- true

let configureServices (services: IServiceCollection) =
    services
        .AddCors()
        .AddGiraffe()
        .AddAuthentication(authenticationOptions)
        .AddJwtBearer(Action<JwtBearerOptions> jwtBearerOptions)
        |> ignore
    services
        .AddSingleton(typeof<IJsonSerializer>, ThothSerializer())
        |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder
        .AddFilter(fun l -> l.Equals LogLevel.Error)
        .AddConsole()
        .AddDebug()
        |> ignore

[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0
