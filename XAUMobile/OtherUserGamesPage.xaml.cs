using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;

namespace XAUMobile
{
    public partial class OtherUserGamesPage : ContentPage
    {
        private readonly HttpClient _client = ApiManagerService.Instance.GetXboxApiClient();
        private readonly MemoryCache _cache;
        private readonly string _xuid;
        private readonly string _gamertag;
        public ObservableCollection<GameItem> Games { get; } = new ObservableCollection<GameItem>();
        private const int TotalGamesToDisplay = 10000;
        private int _currentPage = 1;
        private const int PageSize = 50;
        private int _totalGames = 0;
        private string _selectedFilter = "All Games";
        private string _searchText = "";

        public OtherUserGamesPage(string xuid, string gamertag)
        {
            InitializeComponent();
            _cache = MemoryCache.Default;
            _xuid = xuid;
            _gamertag = gamertag;
            BindingContext = this;
            Title = $"{_gamertag}'s Games";
            _ = LoadGamesAsync();
        }

        private async Task LoadGamesAsync()
        {
            try
            {
                GamesIndicator.IsRunning = true;
                GamesIndicator.IsVisible = true;

                var cachedGameData = GetCachedGamesData();

                if (cachedGameData == null)
                {
                    var jsonResponse = await GetOrFetchGamesDataAsync();
                    if (jsonResponse != null)
                    {
                        CacheGamesData(jsonResponse);
                        ProcessGamesResponse(jsonResponse);
                    }
                }
                else
                {
                    ProcessGamesResponse(cachedGameData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading games: {ex.Message}");
                await CallToActionHelper.ShowMessage(CallToActionControl, "Error loading games", $"Error loading games: {ex.Message}", "erroric50.png", "RedError");
            }
            finally
            {
                GamesIndicator.IsRunning = false;
                GamesIndicator.IsVisible = false;
            }
        }

        private void ProcessGamesResponse(JObject jsonResponse)
        {
            if (jsonResponse == null)
            {
                return;
            }

            var games = jsonResponse["titles"]?.Children<JToken>() ?? Enumerable.Empty<JToken>();
            _totalGames = games.Count();
            labelTotalGames.Text = $"{AppResources.TotalGames}: {_totalGames}";

            Games.Clear();

            // Enable the export button once games are loaded
            ExportCsvButton.IsEnabled = games.Any();

            // Apply filter before pagination
            IEnumerable<JToken> filteredGames = games;
            if (_selectedFilter == "Incomplete Games")
            {
                filteredGames = games.Where(g =>
                    g["achievement"]?["progressPercentage"]?.ToString() != "100" &&
                    g["achievement"]?["totalGamerscore"]?.ToString() != "0");
            }

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filteredGames = filteredGames.Where(g => g["name"]?.ToString()?.ToLower().Contains(_searchText.ToLower()) == true);
            }

            labelTotalGames.Text = $"{AppResources.TotalGames}: {filteredGames.Count()}";

            if (!filteredGames.Any())
            {
                noGamesLabel.IsVisible = true;
                return;
            }

            noGamesLabel.IsVisible = false;

            int startIndex = (_currentPage - 1) * PageSize;
            var paginatedGames = filteredGames.Skip(startIndex).Take(PageSize);

            foreach (var game in paginatedGames)
            {
                string gameName = game["name"]?.ToString() ?? "Unknown";
                string titleId = game["titleId"]?.ToString() ?? "Unknown";

                var achievement = game["achievement"];
                string currentAchievements = achievement?["currentAchievements"]?.ToString() ?? "0";
                string currentGamerscore = achievement?["currentGamerscore"]?.ToString() ?? "0";
                string totalGamerscore = achievement?["totalGamerscore"]?.ToString() ?? "0";
                string progressPercentage = achievement?["progressPercentage"]?.ToString() ?? "0%";

                string displayImage = game["displayImage"]?.ToString() ?? "default.png";

                Games.Add(new GameItem
                {
                    Name = gameName,
                    TitleId = titleId,
                    CurrentAchievements = currentAchievements,
                    CurrentGamerscore = currentGamerscore,
                    TotalGamerscore = totalGamerscore,
                    ProgressPercentage = progressPercentage,
                    DisplayImage = displayImage
                });
            }
        }

        private async Task<JObject> GetOrFetchGamesDataAsync()
        {
            string cacheKey = $"OtherUserGames_{_xuid}";

            if (_cache.Get(cacheKey) is JObject cachedData)
            {
                return cachedData;
            }
            else
            {
                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
                _client.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
                _client.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
                _client.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-US");
                _client.DefaultRequestHeaders.Add(HeaderNames.Authorization, XAUTHService.AuthToken);
                _client.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.TitleHub);
                _client.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);

                var responseString = await _client.GetStringAsync($"https://{Hosts.TitleHub}/users/xuid({_xuid})/titles/titleHistory/decoration/Achievement,scid?maxItems={TotalGamesToDisplay}");
                var jsonResponse = JObject.Parse(responseString);

                var cachePolicy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(30) };
                _cache.Set(cacheKey, jsonResponse, cachePolicy);

                return jsonResponse;
            }
        }

        private JObject? GetCachedGamesData()
        {
            string cacheKey = $"OtherUserGames_{_xuid}";
            return _cache.Get(cacheKey) as JObject;
        }

        private void CacheGamesData(JObject data)
        {
            string cacheKey = $"OtherUserGames_{_xuid}";
            var cachePolicy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(30) };
            _cache.Set(cacheKey, data, cachePolicy);
        }

        private async void OnGameItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is GameItem gameItem)
            {
                if (gameItem.TotalGamerscore == "0")
                {
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.NoAchievements}", $"{AppResources.NoAchievementsMessage}.", "erroric50.png", "RedError");
                    return;
                }
                else
                {
                    await Navigation.PushAsync(new OtherUserAchievementsPage(_xuid, gameItem));
                }
            }
        }

        private void OnNextPageClicked(object sender, EventArgs e)
        {
            var cachedGameData = GetCachedGamesData();
            var games = cachedGameData?["titles"]?.Children<JToken>() ?? Enumerable.Empty<JToken>();

            IEnumerable<JToken> filteredGames = games;
            if (_selectedFilter == "Incomplete Games")
            {
                filteredGames = games.Where(g =>
                    g["achievement"]?["progressPercentage"]?.ToString() != "100" &&
                    g["achievement"]?["totalGamerscore"]?.ToString() != "0");
            }

            if ((_currentPage * PageSize) < filteredGames.Count())
            {
                _currentPage++;
                if (cachedGameData != null)
                {
                    ProcessGamesResponse(cachedGameData);
                }
            }
        }

        private async void OnPreviousPageClicked(object sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                var cachedGameData = GetCachedGamesData();
                if (cachedGameData != null)
                {
                    ProcessGamesResponse(cachedGameData);
                }
                else
                {
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", "No game data available.", "erroric50.png", "RedError");
                }
            }
        }

        private async void OnRefreshPageClicked(object sender, EventArgs e)
        {
            _currentPage = 1;
            _cache.Remove($"OtherUserGames_{_xuid}");
            await LoadGamesAsync();
            _searchText = "";
            GameSearchBar.Text = "";
        }

        private void OnToggleSearchClicked(object sender, EventArgs e)
        {
            GameSearchBar.IsVisible = !GameSearchBar.IsVisible; 
            if (GameSearchBar.IsVisible)
            {
                GameSearchBar.Focus();
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue ?? "";

            // Only search if there are at least 3 characters to avoid lag. And when length = 0, needed to refresh listview
            if (_searchText.Length > 0 && _searchText.Length < 3)
            {
                return;
            }
            _currentPage = 1;
            var cachedGameData = GetCachedGamesData();
            if (cachedGameData != null)
            {
                ProcessGamesResponse(cachedGameData);
            }
        }

        private async void OnExportCsvClicked(object sender, EventArgs e)
        {
            try
            {
                var cachedGameData = GetCachedGamesData();
                var allGames = cachedGameData?["titles"]?.Children<JToken>() ?? Enumerable.Empty<JToken>();

                if (!allGames.Any())
                {
                    await DisplayAlert($"{AppResources.Error}", $"{AppResources.NoGamesToExport}", $"{AppResources.OK}");
                    return;
                }

                IEnumerable<JToken> filteredGames = allGames;
                if (_selectedFilter == "Incomplete Games")
                {
                    filteredGames = allGames.Where(g =>
                        g["achievement"]?["progressPercentage"]?.ToString() != "100" &&
                        g["achievement"]?["totalGamerscore"]?.ToString() != "0");
                }

                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    filteredGames = filteredGames.Where(g => g["name"]?.ToString()?.ToLower().Contains(_searchText.ToLower()) == true);
                }

                StringBuilder csvContent = new StringBuilder();
                csvContent.AppendLine("Name,TitleId,CurrentAchievements,CurrentGamerscore,TotalGamerscore,ProgressPercentage");

                foreach (var game in filteredGames)
                {
                    string gameName = game["name"]?.ToString() ?? "Unknown";
                    string titleId = game["titleId"]?.ToString() ?? "Unknown";
                    string currentAchievements = game["achievement"]?["currentAchievements"]?.ToString() ?? "0";
                    string currentGamerscore = game["achievement"]?["currentGamerscore"]?.ToString() ?? "0";
                    string totalGamerscore = game["achievement"]?["totalGamerscore"]?.ToString() ?? "0";
                    string progressPercentage = game["achievement"]?["progressPercentage"]?.ToString() ?? "0%";

                    string EscapeCsvField(string field) =>
                        field.Contains(",") || field.Contains("\"") || field.Contains("\n")
                        ? $"\"{field.Replace("\"", "\"\"")}\""
                        : field;

                    csvContent.AppendLine(
                        $"{EscapeCsvField(gameName)},{EscapeCsvField(titleId)},{EscapeCsvField(currentAchievements)},{EscapeCsvField(currentGamerscore)},{EscapeCsvField(totalGamerscore)},{EscapeCsvField(progressPercentage)}");
                }

                var csvBytes = Encoding.UTF8.GetBytes(csvContent.ToString());
                using var stream = new MemoryStream(csvBytes);

                var fileSaverResult = await FileSaver.Default.SaveAsync($"{_gamertag}_GamesExport.csv", stream);

                if (fileSaverResult.IsSuccessful)
                {
                    int gamesExportedCount = filteredGames.Count();
                    string savedFileName = Path.GetFileName(fileSaverResult.FilePath);
                    string successMessage = $"{AppResources.GamesExportedSuccessfully}!\n{AppResources.TotalGames}: {gamesExportedCount}\n{AppResources.FileName}: {savedFileName}";
                    await CallToActionHelper.ShowMessage(CallToActionControl, "Success", successMessage, "successic50.png", "Primary");
                }
                else
                {
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.FailedToExportGames}: {fileSaverResult.Exception?.Message}", "erroric50.png", "RedError");
                }
            }
            catch (Exception ex)
            {
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.FailedToExportGames}: {ex.Message}", "erroric50.png", "RedError");
            }
        }
    }
}
