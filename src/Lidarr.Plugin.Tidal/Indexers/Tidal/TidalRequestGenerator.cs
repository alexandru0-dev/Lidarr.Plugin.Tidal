using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Music;
using NzbDrone.Plugin.Tidal;

namespace NzbDrone.Core.Indexers.Tidal
{
    public class TidalRequestGenerator : IIndexerRequestGenerator
    {
        private const int PageSize = 100;
        private const int MaxPages = 3;
        public TidalIndexerSettings Settings { get; set; }
        public Logger Logger { get; set; }

        private static readonly Regex TIDAL_REGEX = new Regex(@"(https?://)?(listen.)?tidal\.com\/(\w*)\/(\d*)?\/?$", RegexOptions.Compiled);

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            // this is a lazy implementation, just here so that lidarr has something to test against when saving settings
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.AddTier(GetRequests("never gonna give you up"));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(AlbumSearchCriteria searchCriteria)
        {
            var chain = new IndexerPageableRequestChain();

            chain.AddTier(GetRequests($"{searchCriteria.ArtistQuery} {searchCriteria.AlbumQuery}"));

            return chain;
        }

        public IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var chain = new IndexerPageableRequestChain();

            chain.AddTier();
            /*chain.AddTier(GetRequests(searchCriteria.ArtistQuery));*/

            foreach (Links item in searchCriteria.Artist.Metadata.Value.Links.Where(x => x.Name == "tidal"))
            {
                Logger.Info($"\t link: \"{item.Url}\"");
                /*chain.Add(GetArtistRequests(TIDAL_REGEX.Replace(item.Url, "$4")));*/
                chain.Add(GetArtistTracksRequests(TIDAL_REGEX.Replace(item.Url, "$4")));
            }
            return chain;
        }

        private IEnumerable<IndexerRequest> GetRequests(string searchParameters)
        {
            if (DateTime.UtcNow > TidalAPI.Instance.Client.ActiveUser.ExpirationDate)
            {
                // ensure we always have an accurate expiration date
                if (TidalAPI.Instance.Client.ActiveUser.ExpirationDate == DateTime.MinValue)
                    TidalAPI.Instance.Client.ForceRefreshToken().Wait();
                else
                    TidalAPI.Instance.Client.IsLoggedIn().Wait(); // calls an internal function which handles refreshes if needed
            }

            for (var page = 0; page < MaxPages; page++)
            {
                var data = new Dictionary<string, string>()
                {
                    ["query"] = searchParameters,
                    ["limit"] = $"{PageSize}",
                    ["types"] = "albums,tracks",
                    ["offset"] = $"{page * PageSize}",
                };

                var url = TidalAPI.Instance!.GetAPIUrl("search", data);
                var req = new IndexerRequest(url, HttpAccept.Json);
                req.HttpRequest.Method = System.Net.Http.HttpMethod.Get;
                req.HttpRequest.Headers.Add("Authorization", $"{TidalAPI.Instance.Client.ActiveUser.TokenType} {TidalAPI.Instance.Client.ActiveUser.AccessToken}");
                yield return req;
            }
        }

        private IEnumerable<IndexerRequest> GetArtistRequests(string id)
        {
            if (DateTime.UtcNow > TidalAPI.Instance.Client.ActiveUser.ExpirationDate)
            {
                TidalAPI.Instance.Client.IsLoggedIn().Wait(); // calls an internal function which handles refreshes if needed
            }

            for (var page = 0; page < MaxPages; page++)
            {
                var data = new Dictionary<string, string>()
                {
                    ["limit"] = $"{PageSize}",
                    ["offset"] = $"{page * PageSize}",
                };

                var url = TidalAPI.Instance!.GetAPIUrl("artists/" + id + "/albums", data);
                var req = new IndexerRequest(url, HttpAccept.Json);
                req.HttpRequest.Method = System.Net.Http.HttpMethod.Get;
                req.HttpRequest.Headers.Add("Authorization", $"{TidalAPI.Instance.Client.ActiveUser.TokenType} {TidalAPI.Instance.Client.ActiveUser.AccessToken}");
                yield return req;
            }
        }

        private IEnumerable<IndexerRequest> GetArtistTracksRequests(string id)
        {
            if (DateTime.UtcNow > TidalAPI.Instance.Client.ActiveUser.ExpirationDate)
            {
                TidalAPI.Instance.Client.IsLoggedIn().Wait(); // calls an internal function which handles refreshes if needed
            }

            for (var page = 0; page < MaxPages; page++)
            {
                var data = new Dictionary<string, string>()
                {
                    ["filter"] = "EPSANDSINGLES",
                    ["limit"] = $"{PageSize}",
                    ["offset"] = $"{page * PageSize}",
                };

                var url = TidalAPI.Instance!.GetAPIUrl("artists/" + id + "/albums", data);
                var req = new IndexerRequest(url, HttpAccept.Json);
                req.HttpRequest.Method = System.Net.Http.HttpMethod.Get;
                req.HttpRequest.Headers.Add("Authorization", $"{TidalAPI.Instance.Client.ActiveUser.TokenType} {TidalAPI.Instance.Client.ActiveUser.AccessToken}");
                yield return req;
            }
        }


    }
}
