using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;

namespace XAUMobile
{
    public partial class SearchGamertagPage : ContentPage
    {
        private readonly HttpClient _client = ApiManagerService.Instance.GetXboxApiClient();
        private string? _searchedXuid;

        public SearchGamertagPage()
        {
            InitializeComponent();
        }
        private async void SearchGamertag(object sender, EventArgs e)
        {
            string? gamertag = GamertagEntryField.Text?.Trim();
            if (string.IsNullOrWhiteSpace(gamertag))
            {
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.EnterValidGamertag}.", "erroric50.png", "RedError");
                return;
            }

            try
            {
                GamertagSearchIndicator.IsVisible = true;
                GamertagSearchIndicator.IsRunning = true;

                Debug.WriteLine($"Searching for gamertag: {gamertag}");
                var profileData = await GetGamertagProfileAsync(gamertag);
                if (profileData == null)
                {
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.FailedGamertagInfo}.", "erroric50.png", "RedError");
                    return;
                }

                DisplayProfileInfo(profileData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in search operation: {ex.Message}");
                await CallToActionHelper.ShowMessage(CallToActionControl, "Error", $"An error occurred: {ex.Message}", "erroric50.png", "RedError");
            }
            finally
            {
                GamertagSearchIndicator.IsRunning = false;
                GamertagSearchIndicator.IsVisible = false;
            }
        }

        private void DisplayProfileInfo(JObject profileData)
        {
            try
            {
                var profileUser = profileData["profileUsers"]?[0];
                if (profileUser == null)
                {
                    Debug.WriteLine("No profile user found in data");
                    return;
                }

                string xuid = profileUser["id"]?.ToString() ?? "N/A";
                _searchedXuid = xuid;

                string profilePicUrl = string.Empty;
                string gamerscore = string.Empty;
                string gamertagValue = string.Empty;

                var settings = profileUser["settings"];
                foreach (var setting in settings)
                {
                    string id = setting["id"].ToString();
                    string value = setting["value"].ToString();

                    if (id == "GameDisplayPicRaw")
                        profilePicUrl = value;
                    else if (id == "Gamerscore")
                        gamerscore = value;
                    else if (id == "Gamertag")
                        gamertagValue = value;
                }

                MainThread.BeginInvokeOnMainThread(() => {
                    if (!string.IsNullOrEmpty(profilePicUrl))
                    {
                        GamerProfileImage.Source = ImageSource.FromUri(new Uri(profilePicUrl));
                    }

                    LBL_GamerXUID.Text = xuid;
                    LBL_GamertagValue.Text = gamertagValue;
                    LBL_GamerscoreValue.Text = gamerscore;

                    GamerProfileSection.IsVisible = true;
                    ViewGamesButton.IsVisible = true;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error displaying profile info: {ex.Message}");
            }
        }

        private async Task<JObject?> GetGamertagProfileAsync(string gamertag)
        {
            if (string.IsNullOrWhiteSpace(gamertag))
            {
                return null;
            }

            string url = $"https://{Hosts.Profile}/users/gt({gamertag})/profile/settings?settings=GameDisplayPicRaw,Gamerscore,Gamertag";

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
            _client.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
            _client.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
            _client.DefaultRequestHeaders.Add(HeaderNames.Authorization, XAUTHService.AuthToken);
            _client.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Profile);
            _client.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-US"); //todo: use settings lang

            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"API Response: {jsonResponse}");
            return JObject.Parse(jsonResponse);
        }

        private async void OnViewGamesClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_searchedXuid) || _searchedXuid == "N/A")
            {
                await CallToActionHelper.ShowMessage(CallToActionControl, "Error", "No XUID found. Please search for a gamertag first.", "erroric50.png", "RedError");
                return;
            }
            Debug.WriteLine($"Navigating to OtherUserGamesPage for XUID: {_searchedXuid}, Gamertag: {LBL_GamertagValue.Text}");
            await Navigation.PushAsync(new OtherUserGamesPage(_searchedXuid, LBL_GamertagValue.Text));
        }

    }
}