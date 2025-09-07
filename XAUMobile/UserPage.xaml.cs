using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;

namespace XAUMobile
{
    public partial class UserPage : ContentPage
    {
        private readonly HttpClient _client = ApiManagerService.Instance.GetXboxApiClient();
        private static string _xuid = string.Empty;
        private MemoryCache _cache;

        public UserPage()
        {
            InitializeComponent();

            _cache = MemoryCache.Default;

            TestXAUTH();
        }

        public static string Xuid
        {
            get { return _xuid; }
            private set { _xuid = value; }
        }

        private async void TestXAUTH()
        {
            try
            {
                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
                _client.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
                _client.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
                _client.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-US");  //todo: use settings lang
                _client.DefaultRequestHeaders.Add(HeaderNames.Authorization, XAUTHService.AuthToken);
                _client.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Profile);
                _client.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);

                var responseString = await _client.GetStringAsync($"https://{Hosts.Profile}/users/me/profile/settings?settings=Gamertag");
                var jsonResponse = JObject.Parse(responseString);

                _xuid = jsonResponse["profileUsers"]?[0]?["id"]?.ToString() ?? string.Empty;

                GrabProfile();
                await CallToActionHelper.ShowPopup(CallToActionControl, $"{AppResources.LoginSuccessful}", $"{AppResources.UserPageLoginMessageSuccess}", "successic50.png", "Primary");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request Error: {ex.Message}");

                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.LoginFailed}", $"{AppResources.UserPageLoginMessageFail}.", "erroric50.png", "RedError");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async void GrabProfile()
        {
            try
            {
                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion5);
                _client.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
                _client.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
                _client.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-US");  //todo: use settings lang
                _client.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.PeopleHub);
                _client.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
                _client.DefaultRequestHeaders.Add(HeaderNames.Authorization, XAUTHService.AuthToken);

                var jsonResponse = await GetOrFetchProfileDataAsync();
                var profileData = jsonResponse["people"]?[0];

                if (profileData != null)
                {
                    var gamerAvatar = profileData["displayPicRaw"]?.ToString();

                    var xboxOneRep = profileData["xboxOneRep"]?.ToString();
                    labelXboxOneRep.Text = !string.IsNullOrEmpty(xboxOneRep) ? $"{AppResources.UserPageXboxRep}: {xboxOneRep}" : "Xbox One Rep: N/A";

                    if (profileData["presenceDetails"] is JArray presenceDetails && presenceDetails.Count > 0)
                    {
                        var device = presenceDetails[0]?["Device"]?.ToString();
                        labelDevice.Text = !string.IsNullOrEmpty(device) ? $"{AppResources.Device}: {device}" : "Device: N/A";
                    }

                    var presenceState = profileData["presenceState"]?.ToString();
                    labelPresenceState.Text = !string.IsNullOrEmpty(presenceState) ? $"{AppResources.UserPagePresenceState}: {presenceState}" : "Presence State: N/A";

                    var presenceText = profileData["presenceText"]?.ToString();
                    labelPresenceText.Text = !string.IsNullOrEmpty(presenceText) ? $"{AppResources.UserPagePresenceText}: {presenceText}" : "Presence Text: N/A";

                    var followerCount = profileData["detail"]?["followerCount"]?.Value<int>() ?? 0;
                    var followingCount = profileData["detail"]?["followingCount"]?.Value<int>() ?? 0;
                    labelFollowerCount.Text = $"{AppResources.FollowerCount}: {followerCount}";
                    labelFollowingCount.Text = $"{AppResources.FollowingCount}: {followingCount}";

                    var gamerTag = profileData["gamertag"]?.ToString();
                    var gamerScore = profileData["gamerScore"]?.ToString();

                    if (SettingsService.IsPrivacyEnabled)
                    {
                        avatarImage.Source = "privacyuser.png";
                        labelGamertag.Text = $"{AppResources.Gamertag} Hidden";
                        labelXuid.Text = "XUID: Hidden";
                        labelGamerScore.Text = $"{AppResources.Gamerscore} Hidden";
                    }
                    else
                    {
                        avatarImage.Source = !string.IsNullOrEmpty(gamerAvatar) ? ImageSource.FromUri(new Uri(gamerAvatar)) : null;
                        labelGamertag.Text = !string.IsNullOrEmpty(gamerTag) ? $"{gamerTag}" : "Gamertag: N/A";
                        labelXuid.Text = $"XUID: {_xuid}";
                        labelGamerScore.Text = !string.IsNullOrEmpty(gamerScore) ? $"{AppResources.Gamerscore}: {gamerScore}" : "Gamerscore: N/A";
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task<JObject> GetOrFetchProfileDataAsync()
        {
            const string cacheKey = "UserProfile";

            if (_cache.Get(cacheKey) is JObject cachedData)
            {
                return cachedData;
            }
            else
            {
                var responseString = await _client.GetStringAsync($"https://{Hosts.PeopleHub}/users/me/people/xuids({_xuid})/decoration/detail,preferredColor,presenceDetail,multiplayerSummary");
                var jsonResponse = JObject.Parse(responseString);

                // Cache the response for future use so we dont do an api call every time we visit the users page. expires 10 mins.
                var cachePolicy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(10) };
                _cache.Set(cacheKey, jsonResponse, cachePolicy);

                return jsonResponse;
            }
        }

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            _cache.Remove("UserProfile"); // Clear cached profile data
            TestXAUTH(); // Fetch fresh profile data again
        }
    }
}