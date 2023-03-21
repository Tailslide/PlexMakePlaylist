using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PlexMakePlaylist
{
    public class Application
    {
        public IServiceProvider Services { get; set; }
        private readonly ILogger<Application> _logger;

        public Application(IServiceProvider services) {
            _logger = services.GetService<ILogger<Application>>();
            Services = services;
            _logger.LogInformation("Application created successfully.");
        }
        public async Task MakePlayList()
        {
            _logger.LogInformation("Hello, World");
            //var plexFactory = Services.GetService<IPlexFactory>();

            //// Signin with Username, Password
            //PlexAccount account = plexFactory
            //    .GetPlexAccount("username", "password");

            //// or use and Plex Auth token
            //PlexAccount account = plexFactory
            //    .GetPlexAccount("access_token_here");
        }
    }
}
