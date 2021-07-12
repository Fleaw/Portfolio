namespace Portfolio.Server

open Microsoft.AspNetCore.Hosting
open Bolero.Remoting
open Bolero.Remoting.Server
open Portfolio

type GameService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Main.GameService>()

    override this.Handler =
        {
            getMyGames = Api.Itchio.myGames
        }