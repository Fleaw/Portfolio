namespace Portfolio.Client

open System
open System.Web
open System.Threading.Tasks
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client
open Microsoft.JSInterop
open Microsoft.AspNetCore.Components.Routing
open Microsoft.AspNetCore.Http
open Models

module Main =
    /// Routing endpoints definition.
    type Page =
        | [<EndPoint "/#aboutme">] AboutMe
        | [<EndPoint "/#myprojects">] MyProjects

    type DeviceType =
        | Desktop
        | Mobile

    /// The Elmish application's model.
    type Model =
        {
            Page: Page
            DeviceType: DeviceType

            Games: Result<Game[], string> option
            Repos: Result<GitHubRepo[], string> option
        }

    let initModel =
        {
            Page = AboutMe
            DeviceType = Desktop
            Games = None
            Repos = None
        }

    /// Remote Services
    type MyProjectsService =
        {
            getMyGames: unit -> Async<Result<Game[], string>>

            getMyRepos: unit -> Async<Result<GitHubRepo[], string>>
        }
        interface IRemoteService with
            member this.BasePath = "/"

    /// The Elmish application's update messages.
    type Message =
        | SetPage of Page
        | Initialized of string
        | SetDeviceType of bool
        | LocationChanged of string

        | GetGames
        | GotGames of Result<Game[], string>

        | GetGitubRepos
        | GotGitubRepos of Result<GitHubRepo[], string>

        | ErrorMsg of exn


    let jsConsoleError (js:IJSRuntime) (msg:string) =
        Task.Run(fun () -> js.InvokeVoidAsync("console.error", msg).AsTask()) |> ignore
        Task.Run(fun () -> js.InvokeVoidAsync("alert", msg).AsTask()) |> ignore

    let update remote (js: IJSRuntime) message model =
        match message with
        | SetPage page ->
            (*
            let cmd =
                match page with
                | AboutMe -> Cmd.none
                | MyProjects ->
                    let getGamesCmd = Cmd.ofMsg GetGames
                    let getReposCmd = Cmd.ofMsg GetGitubRepos

                    match model.Games, model.Repos with
                    | Some _, Some _ -> Cmd.none
                    | None , Some _ -> getGamesCmd
                    | Some _, None -> getReposCmd
                    | None, None -> Cmd.batch [getGamesCmd; getReposCmd]
            *)
            { model with Page = page }, Cmd.none
        | Initialized url ->
            let deviceTypeCmd = Cmd.OfJS.perform js "isMobile" [||] SetDeviceType
            let getGamesCmd = Cmd.ofMsg GetGames
            let getReposCmd = Cmd.ofMsg GetGitubRepos
            
            model, Cmd.batch [deviceTypeCmd; getGamesCmd; getReposCmd; Cmd.ofMsg (LocationChanged url)]
        | SetDeviceType isMobile ->
            let device =
                match isMobile with
                | true -> Mobile
                | false -> Desktop
            { model with DeviceType = device } , Cmd.none
        | LocationChanged url ->
            let cmd =
                match url.Split('#') with
                | [|_|] -> AboutMe
                | [|_; "aboutme"|] -> AboutMe
                | [|_; "myprojects"|] -> MyProjects
                | [|_; _|] -> AboutMe
                | _ -> AboutMe
                |>  SetPage
                |> Cmd.ofMsg
            model, cmd
        /// Games
        | GetGames ->
            let cmd = Cmd.OfAsync.either remote.getMyGames () GotGames ErrorMsg
            { model with Games = None }, cmd
        | GotGames result ->
            let cmd =
                match result with
                | Error msg -> ErrorMsg (Exception $"Itch.io error : {msg}") |> Cmd.ofMsg
                | Ok _ -> Cmd.none
            { model with Games = Some result }, cmd
        /// Repositories
        | GetGitubRepos ->
            let cmd = Cmd.OfAsync.either remote.getMyRepos () GotGitubRepos ErrorMsg
            { model with Repos = None }, cmd
        | GotGitubRepos result ->
            let cmd =
                match result with
                | Error msg -> ErrorMsg (Exception $"GitHub error : {msg}") |> Cmd.ofMsg
                | Ok _ -> Cmd.none
            { model with Repos = Some result }, cmd
        /// Error
        | ErrorMsg exn ->
            jsConsoleError js exn.Message
            model, Cmd.none

    
    type Main = Template<"wwwroot/main.html">

    let aboutMePage device =
        let driveFileID = "1wlB2JzVHqUgwlYbH9SDS7dolkNVwgXar"
        let cvUrl = sprintf "https://drive.google.com/file/d/%s/preview" driveFileID
        let downloadUrl = sprintf "https://drive.google.com/uc?export=download&id=%s" driveFileID

        let download =
            div [attr.id "download"] [
                a [attr.href downloadUrl] [text "Download"]
            ]

        let pdfViewer =
            object [attr.data cvUrl; attr.``type`` "application/pdf"; attr.width (if device = Desktop then "40%" else "80%"); attr.height "75%"] [
                p [attr.style "color: #fff"] [text "PDF preview not supported by your device"]
            ]

        div [attr.id "cv"] [
            pdfViewer
            download
        ]

    let displayGames device gameList =
        match gameList with
        | None ->
            Main.GettingGames().Elt()
        | Some result ->
            let width =
                match device with
                | Mobile -> "208"
                | Desktop -> "552"

            let displayScreenshots game =
                game.Screenshots
                |> List.mapi (fun i url ->
                    a [attr.href url; "data-lightbox" => $"{game.Title}-gallery"; "data-title" => ""] [if i = 0 then img [attr.``class`` "cover"; attr.src game.Cover; attr.height "158"; attr.alt ""]]
                )
                |> concat

            let showGame game =
                Main.Game()
                    .Id(game.Id |> string)
                    .Width(width)
                    .Title(game.Title)
                    .Url(game.Url)
                    .Screenshots(displayScreenshots game)
                    .Elt()

            match result with
            | Ok games ->
                forEach games showGame
            | Error _ -> Main.GamesError().Elt()


    let displayRepos repoList =
        match repoList with
        | None ->
            Main.GettingRepos().Elt()
        | Some result ->
            let showLanguage lang =
                a [
                    attr.``class`` "language"
                    attr.href (sprintf "https://github.com/search?q=user:Fleaw+language:%s&type=Repositories" (HttpUtility.HtmlEncode lang))
                ] [text lang]

            let showTopic topic =
                a [
                    attr.``class`` "topic"
                    attr.href (sprintf "https://github.com/search?q=user:Fleaw+topic:%s&type=Repositories" (HttpUtility.HtmlEncode topic))
                ] [text topic]

            let showRepository repo =
                Main.Repository()
                    .Url(repo.Url)
                    .Name(repo.Name)
                    .Languages(forEach repo.Languages showLanguage)
                    .Description(repo.Description |> Option.defaultValue "")
                    .Topics(forEach repo.Topics showTopic)
                    .Elt()
        
            match result with
            | Ok repos ->
                forEach repos showRepository
            | Error _ -> Main.ReposError().Elt()

    let view model dispatch =
        Main()
            .AboutMe(aboutMePage model.DeviceType)
            .Games(displayGames model.DeviceType model.Games)
            .Repositories(displayRepos model.Repos)
            .Elt()


    type MyApp() =
        inherit ProgramComponent<Model, Message>()
        
        let runTask task =
            Task.Run(fun () -> task) |> ignore

        override this.OnAfterRenderAsync (firstRender:bool) : Task =
            match firstRender with
            | true ->
                let baseTask = base.OnAfterRenderAsync firstRender
                runTask baseTask
                this.Dispatch (Initialized this.NavigationManager.Uri)
                this.JSRuntime.InvokeVoidAsync("import", "./javascript/jquery.pagepiling.js").AsTask()
            | false ->
                Task.CompletedTask

        override this.Program =
            let onLocationChanged (dispatch: Message -> unit) =
                this.NavigationManager.LocationChanged.Subscribe (fun e -> dispatch (LocationChanged e.Location)) |> ignore

            let gameService = this.Remote<MyProjectsService>()
            let update = update gameService this.JSRuntime
            Program.mkProgram (fun _ -> initModel, Cmd.none) update view
            |> Program.withSubscription (fun _ -> Cmd.ofSub onLocationChanged)
    #if DEBUG
            |> Program.withHotReload
    #endif
            