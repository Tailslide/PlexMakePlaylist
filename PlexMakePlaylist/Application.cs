using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plex.Api.Factories;
using Plex.Library.ApiModels.Accounts;
using Plex.Library.ApiModels.Libraries;
using Plex.Library.ApiModels.Servers;
using Plex.ServerApi.Clients.Interfaces;
using Plex.ServerApi.PlexModels.Folders;
using Plex.ServerApi.PlexModels.Library.Search;
using Plex.ServerApi.PlexModels.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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
            else MyServer = services.ConnectToPlexServer(key, servername);
        }

        public async Task MakePlaylists()
        {
            // could run these in parallel?
            await MakeMoviePlaylist();
            await MakeMusicPlaylist();
        }

        public async Task MakeMoviePlaylist()
        {
            using (_logger.BeginScope("MakeMoviePlayList()"))
            {
                if (MyServer == null) throw new Exception("Not logged into server");

                var libraries = await MyServer.Libraries();
                var foundLibrary = libraries.FirstOrDefault(x => x.Scanner == "Plex Movie");
                if (foundLibrary == null) throw new Exception("No Movie Library Found on Server");
                var movieLibrary = (Plex.Library.ApiModels.Libraries.MovieLibrary)foundLibrary;
                var playlists = await MyServer.Playlists();

                var listName = _config["plexLarge1080pMoviesPlaylist"];
                if (listName == null) _logger.LogError("Please specify plexLargeMoviesPlaylist in config.json");
                else
                {
                    var bigMovieList = playlists.Metadata.FirstOrDefault(x => x.Title.Trim().ToUpper() == listName.ToUpper().Trim());
                    int limit = 999999;

                    var movies = await movieLibrary.AllMovies("", 0, limit);
                    var media = movies.Media;
                    _logger.LogInformation($"Found {media.Count()} movies");
                    Thread.Sleep(100);  // give logs a chance to flush - can't for the life of me force them to dispose

                    // find movies bigger than 20gb
                    //var surroundTracks = media.Where(x => x.Media.Any(x => x.Part.Sum(y=>y.Size) > 21474836480));
                    for (int x = 0; x < media.Count(); x++)
                    {
                        var bigMovies2 = media.Take(x).OrderByDescending(x => x.Media.Max(y => y.Part.Sum(z => z.Size)));
                    }
                    // count only 1080p non 3d movies
                    var bigMovies = media.Where(x=>x.Media != null && x.Media.Any(y=>y.VideoResolution == "1080"))
                        .OrderByDescending(x => x.Media.Where(q=>q.VideoResolution == "1080").Max(y=> y.Part.Sum(z=>z.File.Contains("3D") ? 0 : z.Size)));
                    foreach (var movie in bigMovies.Take(10)) // add ten biggest media
                    {
                        _logger.LogInformation($"Adding movie {movie.Title} to playlist {listName}");
                        await MyServer.AddPlaylistItems(bigMovieList, new List<string>() { movie.RatingKey });
                    }
                }
            }
            Thread.Sleep(1500); // give logs a chance to flush - can't for the life of me force them to dispose
        }


        public async Task MakeMusicPlaylist()
        {
            using (_logger.BeginScope("MakeMusicPlayList()"))
            {
                if (MyServer == null) throw new Exception("Not logged into server");

                var libraries = await MyServer.Libraries();
                var foundLibrary = libraries.FirstOrDefault(x => x.Scanner == "Plex Music");
                if (foundLibrary == null) throw new Exception("No Music Library Found on Server");
                var musicLibrary = (Plex.Library.ApiModels.Libraries.MusicLibrary)foundLibrary;
                var playlists = await MyServer.Playlists();

                var listName = _config["plexSurroundPlaylist"];
                if (listName == null) _logger.LogError("Please specify plexSurroundPlaylist in config.json");
                else
                {
                    var surroundList = playlists.Metadata.FirstOrDefault(x => x.Title.Trim().ToUpper() == listName.ToUpper().Trim());

                    int limit = 999999;

                    //var items = await musicLibrary.AllArtists("artist.title:asc", 0, 9999999);
                    //var allArtists = items.Media;
                    //_logger.LogInformation($"Found {allArtists.Count()} artists");
                    //var albumitems = await musicLibrary.AllAlbums("", 0, limit);
                    //var allAlbums = albumitems.Media;
                    //_logger.LogInformation($"Found {allAlbums.Count()} albums");
                    var trackitems = await musicLibrary.AllTracks("", 0, limit);
                    var allTracks = trackitems.Media;
                    _logger.LogInformation($"Found {allTracks.Count()} tracks");
                    Thread.Sleep(100);  // give logs a chance to flush - can't for the life of me force them to dispose

                    var surroundTracks = allTracks.Where(x => x.Media.Any(x => x.AudioChannels > 2));
                    _logger.LogInformation($"Found {surroundTracks.Count()} surround tracks");
                    foreach (var track in surroundTracks)
                    {
                        _logger.LogInformation($"Adding Track {track.GrandparentTitle} - {track.ParentTitle} - {track.Title}  to playlist {listName}");
                        await MyServer.AddPlaylistItems(surroundList, new List<string>() { track.RatingKey });
                    }
                    //foreach (var artist in allArtists.Where(x => allAlbums.Any(y => (y.ParentGuid == x.Guid) && surroundTracks.Any(z => z.ParentGuid == y.Guid))))

                        // sample code to loop all artists albums and tracks
                        //foreach (var artist in allArtists)
                        //{
                        //    string Title = artist.Title;
                        //    _logger.LogInformation($"{artist.Title}");

                        //    //var hasparent = new List<FilterRequest> { new() {Field = "artist.title", Operator = Operator.Is, Values = new List<string> { "3rd Bass" }}};
                        //    //var albums = await musicLibrary.SearchAlbums(string.Empty, string.Empty, hasparent, 0, 10);
                        //    var artistAlbums = allAlbums.Where(x => x.ParentGuid == artist.Guid);
                        //    foreach (var album in artistAlbums)
                        //    {
                        //        _logger.LogInformation($"\tAlbum {album.Title}");
                        //        var albumTracks = allTracks.Where(x => x.ParentGuid == album.Guid);
                        //        foreach (var track in albumTracks)
                        //        {
                        //            _logger.LogInformation($"\t\tTrack {track.Title}");
                        //            //foreach (var media in track.Media)
                        //            //{
                        //            //    _logger.LogInformation($"\t\t\tChannels: {media.AudioChannels}");

                        //            //    if (media.AudioChannels > 2)
                        //            //    {
                        //            //        _logger.LogInformation($"\t\t\t*** ^^^^ multichannel ^^^ ***");
                        //            //    }
                        //            //}
                        //            if (track.Media.Any(x=>x.AudioChannels > 2))
                        //                await MyServer.AddPlaylistItems(surroundList, new List<string>() { track.RatingKey });
                        //        }
                        //    }
                        //}
                }
            }
            Thread.Sleep(1500); // give logs a chance to flush - can't for the life of me force them to dispose
        }
    }
}

