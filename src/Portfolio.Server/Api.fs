namespace Portfolio.Server

open FSharp.Data
open Portfolio
open System
open Client.Models

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

        let myGames () : Async<Result<Game[], string>> = async {
            let! jsonResponse = MyGames.AsyncLoad(myGamesUrl)
        
            let getScreenshots (gameUrl:string) =
                HtmlDocument.Load(gameUrl).Descendants ["a"]
                |> Seq.choose (fun x ->
                    x.TryGetAttribute("href")
                    |> Option.map (fun a ->  a.Value())
                )
                |> Seq.filter (fun url -> url.StartsWith("https://img.itch.zone"))
                |> Seq.toList

            let result =
                match jsonResponse.Errors.Length with
                | x when x > 0 ->
                    let t = jsonResponse.Errors.[0]
                    jsonResponse.Errors.[0]
                    |> Error
                | _ ->
                    jsonResponse.Games
                    |> Array.map (fun g ->
                        {
                            Id = g.Id
                            Title = g.Title
                            Description = g.ShortText
                            Url = g.Url
                            Cover = g.CoverUrl.Replace("\\", "")
                            Screenshots = getScreenshots g.Url
                        }
                    )
                    |> Ok

            return result
        }

    module GitHub =
        open System.Text
        open System.Text.RegularExpressions
        
        [<Literal>]
        let appName = "Portfolio"

        [<Literal>]
        let githubUser = "Fleaw"

        let token  = Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN")

        let authorization = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes($"{githubUser}:{token}"))
            
        [<Literal>]
        let topicsResponse =
            """
            {
              "names": [
                "something"
              ]
            }
            """

        let headers = 
            [
                "User-Agent", appName
                "Authorization", $"Basic {authorization}"
            ]

        type GitHubRepos = JsonProvider<"https://api.github.com/users/google/repos">
        type RepoTopics = JsonProvider<topicsResponse>

        let myRepos () : Async<Result<GitHubRepo[], string>> = async {
            try
                let! response = Http.AsyncRequestString($"https://api.github.com/users/{githubUser}/repos", headers = headers)

                let repos = GitHubRepos.Parse(response)

                let filterRepo (repo:GitHubRepos.Root) =
                    not repo.Fork && repo.StargazersCount > 0

                let getRepoTopics repoName =
                    let response = Http.AsyncRequestString($"https://api.github.com/repos/{githubUser}/{repoName}/topics", headers = ("Accept", "application/vnd.github.mercy-preview+json") :: headers) |> Async.RunSynchronously
                    RepoTopics.Parse response

                let getRepoLanguages languagesUrl =
                    let response = Http.AsyncRequestString(languagesUrl, headers=headers) |> Async.RunSynchronously
                    Regex.Replace(response.Replace("{","").Replace("}",""), "[^A-Z,a-z,#]", "").Split ","

                let result =
                    repos
                    |> Array.filter filterRepo
                    |> Array.map (fun r ->
                        let topics = getRepoTopics r.Name
                        let languages = getRepoLanguages r.LanguagesUrl

                        {
                            Name = r.Name
                            Description = r.Description
                            Topics = topics.Names |> Array.toList
                            Languages = languages |> Array.toList
                            Url = r.HtmlUrl
                        }
                    )

                return Ok result
            with ex ->
                return Error ex.Message
        }
        