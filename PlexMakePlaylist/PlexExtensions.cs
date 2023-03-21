using Microsoft.Extensions.DependencyInjection;
using Plex.Api.Factories;
using Plex.Library.ApiModels.Servers;
using Plex.ServerApi.Clients.Interfaces;
using Plex.ServerApi.PlexModels.Account;


namespace PlexMakePlaylist
{
    public static class PlexExtensions
    {
        public static Server? ConnectToPlexServer(this IServiceProvider services, string token, string serverName = "", bool TryLocalFirst = true, bool OwnedOnly = true)
        {
            Server? myserver = null;
            var _plexFactory = services.GetService<IPlexFactory>();
            Plex.ServerApi.PlexModels.Server.PlexServer? server = null;
            if (_plexFactory != null)
            {
                var account = _plexFactory
                    .GetPlexAccount(token);
                var serversum = account.ServerSummaries().Result;
                var server1 = serversum.Servers
                    .Where(x =>
                        (x.Owned == 1 || OwnedOnly == false) &&
                        (x.Name == serverName || serverName == "")
                    )
                    .First();
                var serverClient = services.GetService<IPlexServerClient>();
                if (serverClient == null) throw new MissingMemberException("Missing IPlexServerClient Service");
                var libClient = services.GetService<IPlexLibraryClient>();
                if (libClient == null) throw new MissingMemberException("Missing IPlexLibraryClient Service");

                if (TryLocalFirst)
                {                    
                    myserver = ConnectLocal(server1, serverClient, libClient);
                    if (myserver == null) myserver=ConnectRemote(server1, serverClient, libClient);
                }
                else
                {
                    myserver = ConnectRemote(server1, serverClient, libClient);
                    if (myserver == null) myserver = ConnectLocal(server1, serverClient, libClient);
                }
            }
            return myserver;
        }

        private static Server? ConnectLocal(AccountServer server1, IPlexServerClient? serverClient, IPlexLibraryClient? libClient)
        {
            Server? myserver = null;
            try
            {
                List<string> localIP = new List<string>();
                if (!server1.LocalAddresses.Contains(","))
                    localIP.Add(server1.LocalAddresses);
                else
                    localIP.AddRange(server1.LocalAddresses.Split(","));

                foreach (string ip in localIP)
                {
                    server1.Host = ip;
                    myserver = new Server(serverClient, libClient, server1);
                    break; // assume if this went OK we are connected
                }
            }
            catch 
            { 
                // TODO: don't rely on exception for this somehow?                  
            }

            return myserver;
        }

        private static Server? ConnectRemote(AccountServer server1, IPlexServerClient serverClient, IPlexLibraryClient? libClient)
        {
            Server? myserver = null;
            try
            {
                myserver = new Server(serverClient, libClient, server1);
            }
            catch
            {
                // TODO: don't rely on exception for this somehow?                  
            }

            return myserver;
        }
    }
}
