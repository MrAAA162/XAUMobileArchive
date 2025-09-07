using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;

namespace XAUMobile
{
    public partial class StatsPage : ContentPage
    {
        private readonly HttpClient _client = ApiManagerService.Instance.GetXboxApiClient();
        public ObservableCollection<StatItem> StatsList { get; set; } = new();

        private StatItem? _selectedStatItem;

        public StatsPage()
        {
            InitializeComponent();
            LST_Stats.ItemsSource = StatsList;
        }

        // most copy pasta from draff's xau v1. need to re-write later
        private async void LoadStats(object sender, EventArgs e)
        {
            Debug.WriteLine("Load stats button clicked.");
            LST_Stats.SelectedItem = null;
            StatsList.Clear();

            if (string.IsNullOrWhiteSpace(TXT_TitleID.Text))
            {
                Write_Stats.IsVisible = false;
                LBL_SelectedStat.Text = "Selected Stat: None";
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.EnterValidTitleId}.", "erroric50.png", "RedError");
                return;
            }

            try
            {
                var requestBody = new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        arrangebyfield = "xuid",
                        xuids = new[] { UserPage.Xuid },
                        groups = new[] { new { name = "Hero", titleId = TXT_TitleID.Text } }
                    }), Encoding.UTF8, "application/json");

                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
                _client.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
                _client.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
                _client.DefaultRequestHeaders.Add(HeaderNames.Authorization, XAUTHService.AuthToken);
                _client.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-US");  //todo: use settings lang

                var response = await _client.PostAsync($"https://{Hosts.UserStats}/batch", requestBody);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"API Response: {content}");

                var jsonResponse = JObject.Parse(content);
                string formattedJson = jsonResponse.ToString(Formatting.Indented);
                Debug.WriteLine($"API Response: {formattedJson}");

                if (jsonResponse["groups"]?[0]?["statlistscollection"]?[0]?["stats"] is JArray stats)
                {
                    foreach (var stat in stats)
                    {
                        var displayName = stat["groupproperties"]?["DisplayName"]?.ToString();
                        var value = stat["value"]?.ToString() ?? "N/A";
                        var scid = stat["scid"]?.ToString();
                        var name = stat["name"]?.ToString();

                        if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(name))
                        {
                            StatsList.Add(new StatItem
                            {
                                DisplayName = displayName,
                                Value = value,
                                Name = name,
                                Scid = scid
                            });
                        }
                        else
                        {
                            Debug.WriteLine("Missing DisplayName or Name in stat.");
                        }
                    }

                    LST_Stats.ItemsSource = null; // Reset list to refresh UI
                    LST_Stats.ItemsSource = StatsList;
                    Write_Stats.IsVisible = true;
                }
                else
                {
                    Debug.WriteLine("Invalid response structure or no stats available.");
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"HTTP Error loading stats: {ex.Message}");
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.FailedToLoadStats}.", "erroric50.png", "RedError");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"General Error loading stats: {ex.Message}");
                Write_Stats.IsVisible = false;
                LBL_SelectedStat.Text = "Selected Stat: None";
                await DisplayAlert("Error", "An unexpected error occurred.", "OK");
            }
        }

        private void LST_Stats_SelectedIndexChanged(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is StatItem selectedStat)
            {
                _selectedStatItem = selectedStat;
                TXT_Stat.Text = selectedStat.Value;
                LBL_SelectedStat.Text = $"{selectedStat.DisplayName}";
                Debug.WriteLine($"Selected stat: {selectedStat.DisplayName} - {selectedStat.Value}");
            }
        }

        private async void WriteStats(object sender, EventArgs e)
        {
            if (_selectedStatItem is null || string.IsNullOrWhiteSpace(TXT_Stat.Text))
            {
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.NoStatsSelected}.", "erroric50.png", "RedError");
                return;
            }

            try
            {
                if (_selectedStatItem?.Name == null)
                {
                    await DisplayAlert("Error", "Selected stat does not have a valid name.", "OK");
                    return;
                }

                var currentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ");
                long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var statsDict = new Dictionary<string, object>
                {
                    { _selectedStatItem.Name, new { value = TXT_Stat.Text } }
                };

                var requestBody = new
                {
                    schema = "http://stats.xboxlive.com/2017-1/schema#",
                    previousRevision = unixTime,
                    revision = unixTime + 1,
                    stats = new
                    {
                        title = statsDict
                    },
                    timestamp = currentTime
                };

                var requestBodyJson = JsonConvert.SerializeObject(requestBody);
                Debug.WriteLine($"Request Body: {requestBodyJson}");

                var requestContent = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

                var response = await _client.PatchAsync($"https://{Hosts.StatsWrite}/stats/users/{UserPage.Xuid}/scids/{_selectedStatItem.Scid}", requestContent);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Update Response: {result}");
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Success}", $"{AppResources.StatsUpdateRequestSent}.", "infoic50.png", "Primary");
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"Error writing stat: {ex.Message}");
                await DisplayAlert("Error", "Failed to update stat.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing stat: {ex.Message}");
                await DisplayAlert("Error", "An unexpected error occurred.", "OK");
            }
        }
    }

    public class StatItem
    {
        public string? DisplayName { get; set; }
        public string? Value { get; set; }
        public string? Name { get; set; }
        public string? Scid { get; set; }
        public string? Type { get; set; }
    }
}