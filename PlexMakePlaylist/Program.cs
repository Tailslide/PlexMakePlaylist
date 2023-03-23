// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Configuration;
using Plex.Api.Factories;
using Plex.Library.Factories;
using Plex.ServerApi;
using Plex.ServerApi.Api;
using Plex.ServerApi.Clients;
using Plex.ServerApi.Clients.Interfaces;
using Plex.ServerApi.PlexModels.Account;
using PlexMakePlaylist;
using System.Diagnostics;
using System;
using System.Net.Http;

internal class Program
{

    //async Task Main feature allowed from C# 7.1+
    public static async Task Main(string[] args)
    {
        IServiceProvider services = ConfigureServices();
        var app = new Application(services);
        if (app.MyServer != null) await app.MakePlaylists();
    }

    public static IServiceProvider ConfigureServices()
    {
        // Setup Dependency Injection
        IServiceCollection services = new ServiceCollection();

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", optional: true, reloadOnChange: true)
            .Build();

        //services.AddSingleton<IConfigurationRoot>(configuration);
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

        services.AddSingleton(apiOptions);
        services.AddTransient<IPlexServerClient, PlexServerClient>();
        services.AddTransient<IPlexAccountClient, PlexAccountClient>();
        services.AddTransient<IPlexLibraryClient, PlexLibraryClient>();
        services.AddTransient<IApiService, ApiService>();
        services.AddTransient<IPlexFactory, PlexFactory>();
        services.AddTransient<IPlexRequestsHttpClient, PlexRequestsHttpClient>();
        services.AddLogging(configure => configure
            .AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            })
            .AddFilter((provider, category, logLevel) =>
            {
                return (category != "Plex.ServerApi.Api.ApiService" || logLevel > LogLevel.Information);
            })
            .SetMinimumLevel(LogLevel.Information)            
        );
        
        return services.BuildServiceProvider();        
    }

}