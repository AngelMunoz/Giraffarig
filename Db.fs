module Db

open MongoDB.Driver
open Types
open System

let connectionString = Environment.GetEnvironmentVariable "MONGO_URL"

[<Literal>]
let DatabaseName = "fs_database_name"

let client = MongoClient(connectionString)

let db = client.GetDatabase(DatabaseName)

let UserCollection = db.GetCollection<User> "fs_users"
