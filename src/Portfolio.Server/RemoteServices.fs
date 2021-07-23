namespace Portfolio.Server

open Microsoft.AspNetCore.Hosting
open Bolero.Remoting
open Bolero.Remoting.Server
open Portfolio

type MyProjectsService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Main.MyProjectsService>()

    override this.Handler =
        {
            getMyGames = Api.Itchio.myGames

            getMyRepos = Api.GitHub.myRepos
        }