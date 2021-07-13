namespace Portfolio.Server

open FSharp.Data
open Portfolio
open System

[<AutoOpen>]
module Api =

    module Itchio =
    
        let apiKey = Environment.GetEnvironmentVariable("ITCHIO_API_KEY")

        [<Literal>]
        let myGamesSample =
            """
            [
                {
                    "games": [
                        {
                            "purchases_count": 0,
                            "embed": {
                                "fullscreen": true,
                                "height": 1000,
                                "width": 507
                            },
                            "p_osx": false,
                            "id": 123,
                            "published": true,
                            "published_at": "2021-05-26 15:51:09",
                            "views_count": 0,
                            "url": "https://",
                            "can_be_bought": false,
                            "p_android": false,
                            "p_linux": false,
                            "created_at": "2021-05-26 14:16:10",
                            "user": {
                                "url": "https:",
                                "cover_url": "https://",
                                "username": "Username",
                                "id": 123
                            },
                            "downloads_count": 0,
                            "has_demo": false,
                            "title": "Title",
                            "in_press_system": false,
                            "p_windows": false,
                            "cover_url": "https://",
                            "min_price": 0,
                            "classification": "game",
                            "short_text": "Description",
                            "type": "html"
                        }
                    ]
                },
                {
                    "errors": [
                        "invalid key"
                    ]
                }
            ]
            """

        let myGamesUrl = $"https://itch.io/api/1/{apiKey}/my-games"

        type MyGames = JsonProvider<myGamesSample, SampleIsList=true>

        let myGames () : Async<Result<Client.Main.Game[], string>> = async {
            let! jsonResponse = MyGames.AsyncLoad(myGamesUrl)
        
            let result =
                match jsonResponse.Errors.Length with
                | x when x > 0 ->
                    let t = jsonResponse.Errors.[0]
                    jsonResponse.Errors.[0]
                    |> Error
                | _ ->
                    jsonResponse.Games
                    |> Seq.map (fun g ->
                        {
                            Client.Main.Game.id = g.Id
                            Client.Main.Game.title = g.Title
                            Client.Main.Game.description = g.ShortText
                            Client.Main.Game.url = g.Url
                        }
                    )
                    |> Seq.toArray
                    |> Ok

            return result
        }