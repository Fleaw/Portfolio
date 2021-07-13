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

module Main =
    /// Routing endpoints definition.
    type Page =
        | [<EndPoint "/#home">] Home
        | [<EndPoint "/#games">] Games

    type DeviceType =
        | Desktop
        | Mobile

    /// The Elmish application's model.
    type Model =
        {
            Page: Page
            Games: Game[] option
            DeviceType: DeviceType option
        }
    and Game =
        {
            id: int
            title: string
            description: string
            url: string
        }

    let initModel =
        {
            Page = Home
            Games = None
            DeviceType = None
        }

    type GameService =
        {
            getMyGames: unit -> Async<Game[]>
        }
        interface IRemoteService with
            member this.BasePath = "/"

    /// The Elmish application's update messages.
    type Message =
        | SetPage of Page
        | Initialized
        | SetDeviceType of bool
        | LocationChanged of LocationChangedEventArgs
        | GetGames
        | GotGames of Game[]
        | Error of exn

    let update remote (js: IJSRuntime) message model =
        match message with
        | SetPage page ->
            let cmd =
                match model.Games with
                | Some _ -> Cmd.none
                | None ->
                    match page with
                    | Home -> Cmd.none
                    | Games -> Cmd.ofMsg GetGames

            { model with Page = page }, cmd
        | Initialized ->
            let cmd =
                match model.DeviceType with
                    | None -> Cmd.OfJS.perform js "isDevice" [||] SetDeviceType
                    | Some _ -> Cmd.none
            model, cmd
        | SetDeviceType mobile ->
            let device =
                match mobile with
                | true -> Mobile
                | false -> Desktop
            { model with DeviceType = Some device } , Cmd.none
        | LocationChanged e ->
            let cmd =
                match e.Location.Split('#') with
                | [|_|] -> Home
                | [|_; "home"|] -> Home
                | [|_; "games"|] -> Games
                | [|_; _|] -> Home
                | _ -> Home
                |>  SetPage
                |> Cmd.ofMsg
            model, cmd
        | GetGames ->
            let cmd = Cmd.OfAsync.either remote.getMyGames () GotGames Error
            { model with Games = None }, cmd
        | GotGames games ->
            { model with Games = Some games }, Cmd.none
        | Error exn ->
            //TODO: Display something
            model, Cmd.none

    
    type Main = Template<"wwwroot/main.html">

    let homePage model =
        let width, height =
            match model.DeviceType with
            | Some device ->
                match device with
                | Mobile -> "Auto", "70%"
                | Desktop -> "33%", "80%"
            | None -> "33%", "80%"
        //iframe [attr.src "../CV Hasslauer Johan.pdf#toolbar=0"; attr.width width; attr.height height] []
        object [attr.data "../CV_Hasslauer_Johan.pdf#toolbar=0"; attr.``type`` "application/pdf"; attr.width width; attr.height height] [
            p [attr.style "color: #fff"] [text "PDF preview not supported by your device"]
            a [attr.href "../CV_Hasslauer_Johan.pdf"; attr.download "CV Hasslauer Johan"; attr.style "color: #15F; text-decoration: none;"] [text "Download"]
        ]

    let gamePage model =
        cond model.Games <| function
        | None ->
            Main.EmptyData().Elt()
        | Some games ->
            concat [
                forEach games <| fun game ->
                    iframe [attr.src $"https://itch.io/embed/{game.id}?border_width=5&dark=true"; "frameborder" => "0"; attr.width "552"; attr.height "167"; attr.style "height:167px"] [
                        a [attr.href game.url] [text $"{game.title} by Fleaw"]
                    ]
            ]

    let view model dispatch =
        Main()
            .Home(homePage model)
            .Games(gamePage model)
            .Elt()

    type MyApp() =
        inherit ProgramComponent<Model, Message>()        

        override this.OnAfterRenderAsync (firstRender:bool) : Task =
            match firstRender with
            | true ->
                let baseTask = base.OnAfterRenderAsync firstRender
                Task.Run(fun () -> baseTask) |> ignore
                this.Dispatch Initialized
                this.JSRuntime.InvokeVoidAsync("import", "./javascript/jquery.pagepiling.js").AsTask()
            | false ->
                Task.CompletedTask

        override this.Program =
            let onLocationChanged (dispatch: Message -> unit) =
                this.NavigationManager.LocationChanged.Subscribe (LocationChanged >> dispatch) |> ignore

            let gameService = this.Remote<GameService>()
            let update = update gameService this.JSRuntime
            Program.mkProgram (fun _ -> initModel, Cmd.none) update view
            |> Program.withSubscription (fun _ -> Cmd.ofSub onLocationChanged)
    #if DEBUG
            |> Program.withHotReload
    #endif
            