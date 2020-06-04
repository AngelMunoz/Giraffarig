module Helpers

open Microsoft.AspNetCore.Http
open Giraffe
open Types
open System
open Microsoft.IdentityModel.Tokens
open System.Text
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt

/// Gets the Parts of the query string related to the pagination items
let getPagination (ctx: HttpContext) =
    let limit =
        match ctx.TryGetQueryStringValue "limit" with
        | None -> 10
        | Some limit ->
            try
                limit |> int
            with _ -> 10

    let offset =
        match ctx.TryGetQueryStringValue "page" with
        | None -> 0
        | Some page ->
            try
                limit * ((page |> int) - 1)
            with _ -> 0

    limit, offset

/// Takes a user and replaces it's password
let updateUserPassword (user, password): User =
    { user with Password = password }

/// converts a user into a userdto
let userToDTO (user: User): UserDTO =
    let { Id = id; Name = name; LastName = lastname; Role = role; Email = email; Password = password } = user
    { Id = id
      Name = name
      LastName = lastname
      Role = role
      Email = email }

/// Gets the security key from the environment JWT_SECRET var
let getSimmetricKey =
    Environment.GetEnvironmentVariable "JWT_SECRET"
    |> Encoding.UTF8.GetBytes
    |> SymmetricSecurityKey

/// Generates signing credentials from a security key
let getSigningCredentials secret = SigningCredentials(secret, SecurityAlgorithms.HmacSha256Signature)

/// Creates a JWT (in it's string form) from a user model
let issueJWT (user: User) =
    let date = DateTime.UtcNow
    let descriptor = SecurityTokenDescriptor()
    descriptor.SigningCredentials <- getSigningCredentials getSimmetricKey
    descriptor.IssuedAt <- Nullable(date)
    descriptor.Expires <- Nullable(date.AddDays(1.0))
    descriptor.Subject <-
        ClaimsIdentity
            ([ Claim(ClaimTypes.Name, user.Name)
               Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) ])

    let handler = JwtSecurityTokenHandler()
    let token = handler.CreateToken descriptor
    handler.WriteToken token
 