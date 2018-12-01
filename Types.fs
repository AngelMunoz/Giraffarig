module Types
open MongoDB.Bson

type LoginPayload = { Email: string; Password: string; }

/// Basic Endpoint result for most of the listable endpoints
[<CLIMutable>]
type EndpointResult<'T> = { Count: int64; List: List<'T>; }

[<CLIMutable>]
type User = { Id: BsonObjectId;  Name: string; LastName: string; Role: string; Email: string; Password: string; }

[<CLIMutable>]
type UserDTO = { Id: BsonObjectId;  Name: string; LastName: string; Role: string; Email: string; }

[<CLIMutable>]
type Product = { Id: BsonObjectId;  Name: string; Description: string; }

[<CLIMutable>]
type LoginResponse = { Token: string; User: UserDTO }


