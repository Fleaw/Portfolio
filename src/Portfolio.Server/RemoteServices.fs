namespace Portfolio.Server

open System
open Microsoft.AspNetCore.Hosting
open Bolero.Remoting
open Bolero.Remoting.Server
open Portfolio

type MyProjectsService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Main.MyRemoteService>()

    override this.Handler =
        {
            getMyGames = Api.Itchio.myGames

            getMyRepos = Api.GitHub.myRepos

            getCVDriveId = (fun () ->  async { return Environment.GetEnvironmentVariable("CV_DRIVE_ID") })
        }