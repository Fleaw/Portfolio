namespace Portfolio.Server

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Bolero
open Bolero.Remoting.Server
open Bolero.Server
open Portfolio
open Bolero.Templating.Server
open Microsoft.AspNetCore.StaticFiles

type Startup() =

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    member this.ConfigureServices(services: IServiceCollection) =
        services.AddMvc() |> ignore
        services.AddServerSideBlazor() |> ignore
        services
            .AddAuthorization()
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .Services
            .AddRemoting<BookService>()
            .AddBoleroHost(
                server = true,
                prerendered = true,
#if DEBUG
                devToggle = true
#else
                devToggle = false
#endif
            )
#if DEBUG
            .AddHotReload(templateDir = __SOURCE_DIRECTORY__ + "/../Portfolio.Client")
#endif
        |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        
        let provider = new FileExtensionContentTypeProvider()
        provider.Mappings.Remove(".data") |> ignore
        provider.Mappings.[".data"] <- "application/octet-stream"
        provider.Mappings.Remove(".wasm") |> ignore
        provider.Mappings.[".wasm"] <- "application/wasm"
        provider.Mappings.Remove(".symbols.json") |> ignore
        provider.Mappings.[".symbols.json"] <- "application/octet-stream"
        //provider.Mappings.Remove(".js") |> ignore
        //provider.Mappings.[".js"] <- "application/javascript"

        let staticFileOptions = new StaticFileOptions()
        staticFileOptions.ContentTypeProvider <- provider

        app
            .UseAuthentication()
            .UseRemoting()
            //.UseStaticFiles()
            .UseStaticFiles(staticFileOptions)
            .UseStaticFiles()
            .UseRouting()
            .UseBlazorFrameworkFiles()
            .UseEndpoints(fun endpoints ->
#if DEBUG
                endpoints.UseHotReload()
#endif
                endpoints.MapBlazorHub() |> ignore
                endpoints.MapFallbackToBolero(Index.page) |> ignore)
        |> ignore

module Program =

    [<EntryPoint>]
    let main args =
        WebHost
            .CreateDefaultBuilder(args)
            .UseStaticWebAssets()
            .UseStartup<Startup>()
            .Build()
            .Run()
        0
