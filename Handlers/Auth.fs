module Handlers.Auth

open BCrypt.Net
open Db
open Helpers
open FSharp.Control.Tasks
open System.Threading.Tasks
open Giraffe
open Microsoft.AspNetCore.Http
open MongoDB.Driver
open System
open Types

let login: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! payload = ctx.BindJsonAsync<LoginPayload>()
            let maybe = UserCollection.Find(fun user -> user.Email = payload.Email).ToEnumerable() |> Seq.tryHead
            return! match maybe with
                    | None -> RequestErrors.NOT_FOUND "Not Found" next ctx
                    | Some user ->
                        match BCrypt.Verify(payload.Password, user.Password) with
                        | false -> RequestErrors.BAD_REQUEST "Invalid Password" next ctx
                        | true ->
                            Successful.OK
                                ({ Token = issueJWT user
                                   User = userToDTO user }) next ctx
        }

let signup: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! payload = ctx.BindJsonAsync<User>()
            let updated = updateUserPassword (payload, BCrypt.HashPassword(payload.Password, 10))
            try
                do! UserCollection.InsertOneAsync updated
                let maybe = UserCollection.Find(fun user -> user.Email = payload.Email).ToEnumerable() |> Seq.tryHead
                match maybe with
                | Some doc -> return! Successful.OK (userToDTO (doc)) next ctx
                | None -> return! RequestErrors.BAD_REQUEST "Invalid User" next ctx
            with :? Exception -> return! ServerErrors.INTERNAL_ERROR "Something Went Wrong" next ctx
        }
