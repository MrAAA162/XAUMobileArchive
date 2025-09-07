using System.Net;

namespace XAUMobile
{
    public static class HeaderNames
    {
        public const string ContractVersion = "x-xbl-contract-version";
        public const string AcceptEncoding = "Accept-Encoding";
        public const string Accept = "accept";
        public const string Authorization = "Authorization";
        public const string AcceptLanguage = "accept-language";
        public const string Host = "Host";
        public const string Connection = "Connection";
        public const string Signature = "Signature";
        public const string UserAgent = "User-Agent";
        public const string XAUVersion = "x-xau-version";
        public const string XAULanguage = "x-xau-language";
        public const string XAU = "x-xau";
    }

    public static class HeaderValues
    {
        public const string ContractVersion1 = "1";
        public const string ContractVersion2 = "2";
        public const string ContractVersion3 = "3";
        public const string ContractVersion4 = "4";
        public const string ContractVersion5 = "5";
        public const string AcceptEncoding = "gzip, deflate";
        public const string Accept = "application/json";
        public const string KeepAlive = "Keep-Alive";
        public const string UserAgentMeowMeow = "Meow Meow";
        public const string XAU = "xaumobileapk";
        public static string Signature => SettingsService.GetCurrentSignature();
    }

    public static class Hosts
    {
        public const string Achievements = "achievements.xboxlive.com";
        public const string Profile = "profile.xboxlive.com";
        public const string PeopleHub = "peoplehub.xboxlive.com";
        public const string TitleHub = "titlehub.xboxlive.com";
        public const string UserStats = "userstats.xboxlive.com";
        public const string StatsWrite = "statswrite.xboxlive.com";
        public const string PresenceHeartBeat = "presence-heartbeat.xboxlive.com";
        public const string Telemetry = "v20.events.data.microsoft.com";
        public const string GitHubApi = "api.github.com";
        public const string GitHubRaw = "raw.githubusercontent.com";
        public const string GamepassCatalog = "catalog.gamepass.com";
        public const string XAUApi = "xau.lol";
        public const string XboxCom = "xbox.com";
        public const string XAUDiscord = "discord.gg/qRkTgnUMFt";
        public const string XAUMobileGithubRelease = "github.com/MrAAA162/XAUMobile-Release/releases";
    }

    public class ApiManagerService : IDisposable
    {
        private static readonly Lazy<ApiManagerService> _instance = new(() => new ApiManagerService());
        public static ApiManagerService Instance => _instance.Value;

        private readonly HttpClient _xboxApiClient;
        private readonly HttpClient _xauApiClient;
        private readonly HttpClient _spoofingClient;

        private ApiManagerService()
        {
            var xboxHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _xboxApiClient = new HttpClient(xboxHandler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _xauApiClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            var spoofingHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _spoofingClient = new HttpClient(spoofingHandler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }


        public HttpClient GetXboxApiClient()
        {
            return _xboxApiClient;
        }

        public HttpClient GetXAUApiClient()
        {
            return _xauApiClient;
        }

        public HttpClient GetSpoofingClient()
        {
            return _spoofingClient;
        }

        public void Dispose()
        {
            _xboxApiClient?.Dispose();
            _xauApiClient?.Dispose();
            _spoofingClient?.Dispose();
        }
    }
}