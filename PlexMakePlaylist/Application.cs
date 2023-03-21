using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plex.Api.Factories;
using Plex.Library.ApiModels.Accounts;
using Plex.Library.ApiModels.Servers;
using Plex.ServerApi.Clients.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;


namespace PlexMakePlaylist
{
    public class Application
    {
        public IServiceProvider Services { get; set; }
        private readonly ILogger<Application> _logger;
        private readonly IConfiguration _config;
        public bool ConnectedToServer = false;
        public Server? MyServer = null;

        public Application(IServiceProvider services) {
            if (services ==null) throw new ArgumentNullException(nameof(services));
            Services = services;
            _logger = services.GetService<ILogger<Application>>()!;
            _config = services.GetService<IConfiguration>()!;

            if (_config == null) throw new MissingMemberException("Missing IConfiguration Service");
            if (_logger == null) throw new MissingMemberException("Missing ILogger Service");

            _logger.LogInformation("Application created successfully.");

            var key = _config["plexToken"];
            var servername = _config["plexServer"];
            if (key == null) _logger.LogError("Please specify plexToken in config.json");
            else if (servername == null) _logger.LogError("Please specify plexServer in config.json");
            else MyServer = services.ConnectToPlexServer(key, servername, false);
        }
        public async Task MakePlayList()
        {
            if (MyServer == null) throw new Exception("Not logged into server");

            _logger.LogInformation("Finding My plex Server");
            //var servers = account.Servers().Result;
            //var myServer = servers.Where(c => c.Owned == 1).First();
            // Get Home OnDeck items
            //var plexFactory = Services.GetService<IPlexFactory>();

            var onDeckItems = MyServer.HomeOnDeck().Result;

        }
    }
}
