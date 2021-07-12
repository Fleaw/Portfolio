namespace Portfolio.Server

open FSharp.Data
open Portfolio
open System

[<AutoOpen>]
module Api =

    module Itchio =
    
        let apiKey = Environment.GetEnvironmentVariable("ITCHIO_API_KEY")

        type MyGames = JsonProvider<"""https://itch.io/api/1/***REMOVED***/my-games""">

        let myGames () = async {
            let! games = MyGames.AsyncGetSample()
        
            return games.Games
            |> Seq.map (fun g -> 
                {
                    Client.Main.Game.id = g.Id
                    Client.Main.Game.title = g.Title
                    Client.Main.Game.description = g.ShortText
                    Client.Main.Game.url = g.Url
                }
            )
            |> Seq.toArray
        }