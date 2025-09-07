using Newtonsoft.Json.Linq;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;
using System.Net.Http;

namespace XAUMobile
{
    public partial class SearchGamesPage : ContentPage
    {
        private readonly HttpClient _client = ApiManagerService.Instance.GetXAUApiClient();
        private Dictionary<string, (string productId, string xboxTitleId)> gameDetails = new Dictionary<string, (string, string)>();

        public SearchGamesPage()
        {
            InitializeComponent();
        }

        private async void OnSearchButtonClicked(object sender, EventArgs e)
        {
            SearchButton.IsEnabled = false;
            try
            {
                GameSearchIndicator.IsRunning = true;
                GameSearchIndicator.IsVisible = true;

                GameNamesListView.ItemsSource = null;
                CopyInstructionLabel.IsVisible = false;
                gameDetails.Clear();

                var searchQueryText = SearchEntry.Text;

                if (string.IsNullOrWhiteSpace(searchQueryText))
                {
                    GameSearchIndicator.IsRunning = false;
                    GameSearchIndicator.IsVisible = false;
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.SearchEnterValidGameName}.", "erroric50.png", "RedError");
                    return;
                }

                var content = new StringContent("{\"titleName\":\"" + searchQueryText + "\"}", System.Text.Encoding.UTF8, "application/json");

                string currentVersion = UpdateService.GetAppVersion()?.Trim() ?? "unknown";
                string currentLanguage = SettingsService.SelectedLanguage ?? "unknown";

                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, HeaderValues.UserAgentMeowMeow);
                _client.DefaultRequestHeaders.Add(HeaderNames.XAUVersion, currentVersion);
                _client.DefaultRequestHeaders.Add(HeaderNames.XAULanguage, currentLanguage);

                var response = await _client.PostAsync($"https://{Hosts.XAUApi}/api/search/games", content);

                if (response.StatusCode == (System.Net.HttpStatusCode)426)
                {
                    GameSearchIndicator.IsRunning = false;
                    GameSearchIndicator.IsVisible = false;
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.UpgradeRequired}", $"{AppResources.UpgradeRequiredMessage}.", "erroric50.png", "RedError");

                    return;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    GameSearchIndicator.IsRunning = false;
                    GameSearchIndicator.IsVisible = false;
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.RateLimitMessage}!", "erroric50.png", "RedError");

                    return;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    GameSearchIndicator.IsRunning = false;
                    GameSearchIndicator.IsVisible = false;
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.SearchNoGamesFound}", $"{AppResources.SearchNoGamesFoundMessage}.", "erroric50.png", "RedError");

                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    GameSearchIndicator.IsRunning = false;
                    GameSearchIndicator.IsVisible = false;
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"Unexpected Error: {response.StatusCode}. Please try again later.", "erroric50.png", "RedError");

                    return;
                }

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseContent);

                var success = json["success"]?.Value<bool>() ?? false;
                if (!success)
                {
                    GameSearchIndicator.IsRunning = false;
                    GameSearchIndicator.IsVisible = false;
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"API returned unsuccessful response.", "erroric50.png", "RedError");
                    return;
                }

                var results = json["results"] as JArray;
                if (results == null || results.Count == 0)
                {
                    GameSearchIndicator.IsRunning = false;
                    GameSearchIndicator.IsVisible = false;
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.SearchNoGamesFound}", $"{AppResources.SearchNoGamesFoundMessage}.", "erroric50.png", "RedError");
                    return;
                }

                foreach (var game in results)
                {
                    var name = game["title"]?.ToString();
                    var productId = game["id"]?.ToString();
                    var xboxTitleId = game["xboxTitleId"]?.ToString();

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(xboxTitleId))
                    {
                        gameDetails[name] = (productId, xboxTitleId);
                    }
                }

                GameNamesListView.ItemsSource = gameDetails.Select(g => new { Name = g.Key, TitleId = g.Value.xboxTitleId }).ToList();

                GameSearchIndicator.IsRunning = false;
                GameSearchIndicator.IsVisible = false;
                SearchTitleLabel.IsVisible = false;
                CopyInstructionLabel.IsVisible = true;
            }
            catch (HttpRequestException ex)
            {
                await CallToActionHelper.ShowMessage(CallToActionControl, "Network Error", $"Please check your connection and try again. {ex.Message}", "erroric50.png", "RedError");
                Console.WriteLine($"HTTP request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                await CallToActionHelper.ShowMessage(CallToActionControl, "Error", $"An unexpected error occurred: {ex.Message}.", "erroric50.png", "RedError");
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await Task.Delay(3000);
                SearchButton.IsEnabled = true;
            }
        }

        private async void OnGameLinkSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
                return;

            try
            {
                var selectedGame = e.SelectedItem as dynamic;
                if (selectedGame == null)
                    return;

                var titleId = selectedGame.TitleId;

                await Clipboard.SetTextAsync(titleId);
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.CopiedToClipboard}", $"{titleId} {AppResources.CopiedToClipboardMessage}.", "successic50.png", "Primary");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}