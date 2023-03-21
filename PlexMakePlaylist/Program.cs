// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Plex.Api.Factories;
using Plex.Library.Factories;
using Plex.ServerApi;
using Plex.ServerApi.Api;
using Plex.ServerApi.Clients;
using Plex.ServerApi.Clients.Interfaces;
using Plex.ServerApi.PlexModels.Account;
using PlexMakePlaylist;
using System.Net.Http;

internal class Program
{

    //async Task Main feature allowed from C# 7.1+
    public static async Task Main(string[] args)
    {
        //Composition root
        IServiceProvider services = ConfigureServices();
        ILogger logger = NullLogger.Instance;

        IHttpClientFactory clientFactory = services.GetRequiredService<IHttpClientFactory>();
        HttpClient client = clientFactory.CreateClient();

        IConfiguration configuration = services.GetRequiredService<IConfiguration>();

        var app = new Application(services);
        await app.MakePlayList();
        //await RunAsync(logger, client, configuration);
    }

    private static async Task RunAsync(ILogger log, HttpClient client, IConfiguration configuration)
    {

        //...
        //Console.WriteLine("Hello, World!");
        //var plexFactory = Services.GetService<IPlexFactory>();

        //// Signin with Username, Password
        //PlexAccount account = plexFactory
        //    .GetPlexAccount("username", "password");

        //// or use and Plex Auth token
        //PlexAccount account = plexFactory
        //    .GetPlexAccount("access_token_here");

    }

    public static IServiceProvider ConfigureServices()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddHttpClient();

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", optional: true, reloadOnChange: true)
            .Build();
        services.AddSingleton<IConfigurationRoot>(configuration);
        services.AddSingleton<IConfiguration>(configuration);

        // Create Client Options
        var apiOptions = new ClientOptions
        {
            Product = "PlexMakePlaylist",
            DeviceName = "API_UnitTests",
            ClientId = "MakeSurroundMusicPlaylist",
            Platform = "Web",
            Version = "v1"
        };


        // Setup Dependency Injection
        //var services = new ServiceCollection();
        services.AddSingleton(apiOptions);
        services.AddTransient<IPlexServerClient, PlexServerClient>();
        services.AddTransient<IPlexAccountClient, PlexAccountClient>();
        services.AddTransient<IPlexLibraryClient, PlexLibraryClient>();
        services.AddTransient<IApiService, ApiService>();
        services.AddTransient<IPlexFactory, PlexFactory>();
        services.AddTransient<IPlexRequestsHttpClient, PlexRequestsHttpClient>();
        services.AddLogging(configure => configure.AddSimpleConsole(options=>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        }));
        return services.BuildServiceProvider();
    }

}