module Handlers.Users

open FSharp.Control.Tasks.Builders
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Db
open Helpers
open Giraffe
open MongoDB.Driver
open Types
open System

let getUsers: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let (limit, offset) = getPagination ctx

            let count = UserCollection.CountDocuments(Builders.Filter.Empty)

            let users =
                UserCollection.Find(Builders.Filter.Empty).Skip(Nullable(offset)).Limit(Nullable(limit)).ToEnumerable()
                |> Seq.map (fun user -> userToDTO (user))
                |> Seq.toList
            return! Successful.OK
                        ({ Count = count
                           List = users }) next ctx
        }
