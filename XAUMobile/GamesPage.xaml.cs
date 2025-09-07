using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Runtime.Caching;
using System.Text;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;

namespace XAUMobile
{
    public partial class GamesPage : ContentPage
    {
        private readonly MemoryCache _cache;
        private readonly HttpClient _client = ApiManagerService.Instance.GetXboxApiClient();
        public ObservableCollection<GameItem> Games { get; } = new ObservableCollection<GameItem>();
        private const int TotalGamesToDisplay = 10000;
        private int _currentPage = 1;
        private const int PageSize = 50;
        private int _totalGames = 0;
        private string _selectedFilter = "All Games";
        private string _searchText = "";

        public GamesPage()
        {
            InitializeComponent();
            _cache = MemoryCache.Default;
            BindingContext = this;
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

            // pagination
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
            const string cacheKey = "GamesData";

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
                _client.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-US"); //todo: use settings lang
                _client.DefaultRequestHeaders.Add(HeaderNames.Authorization, XAUTHService.AuthToken);
                _client.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.TitleHub);
                _client.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);

                var responseString = await _client.GetStringAsync($"https://{Hosts.TitleHub}/users/xuid({UserPage.Xuid})/titles/titleHistory/decoration/Achievement,scid?maxItems={TotalGamesToDisplay}");
                var jsonResponse = JObject.Parse(responseString);

                var cachePolicy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(30) };
                _cache.Set(cacheKey, jsonResponse, cachePolicy);

                return jsonResponse;
            }
        }

        private JObject? GetCachedGamesData()
        {
            const string cacheKey = "GamesData";
            return _cache.Get(cacheKey) as JObject;
        }

        private void CacheGamesData(JObject data)
        {
            const string cacheKey = "GamesData";
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
                    await Navigation.PushAsync(new AchievementsPage(gameItem));
                }
            }
        }

        private async void OnManualAchievementsClicked(object sender, EventArgs e)
        {
            string manualTitleId = await DisplayPromptAsync($"{AppResources.EnterTitleID}", $"{AppResources.LoadManualAchievementsWarning}. {AppResources.EnterValidTitleId}:");

            if (manualTitleId == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(manualTitleId) || !long.TryParse(manualTitleId.Trim(), out _))
            {
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.EnterValidTitleId}.", "erroric50.png", "RedError");
                return;
            }

            var gameItem = new GameItem
            {
                TitleId = manualTitleId.Trim()
            };

            await Navigation.PushAsync(new AchievementsPage(gameItem));
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

                var fileSaverResult = await FileSaver.Default.SaveAsync("GamesExport.csv", stream);

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
            else
            {
                // when enable error, add async to function private async void OnNextPageClicked(object sender, EventArgs e)
                //await CallToActionHelper.ShowPopup(CallToActionControl, "Oops!", "There are no more games to display.", "infoic50.png", "Primary");
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
            else
            {
                //await CallToActionHelper.ShowPopup(CallToActionControl, "Oops!", "You are already on the first page.", "infoic50.png", "Primary");
            }
        }

        private async void OnRefreshPageClicked(object sender, EventArgs e)
        {
            _currentPage = 1;
            _cache.Remove("GamesData");
            await LoadGamesAsync();

            _searchText = ""; // Clear search text
            GameSearchBar.Text = ""; // Clear the search bar's text
        }


        private async void OnFilterMenuButtonClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet($"{AppResources.FilterGames}", $"{AppResources.Cancel}", null, $"{AppResources.FilterAllGamesOption}", $"{AppResources.FilterIncompleteGamesOption}");

            // if "Cancel" was pressed and exit the method early
            if (action == $"{AppResources.Cancel}")
            {
                return;
            }

            if (action == $"{AppResources.FilterAllGamesOption}")
            {
                _selectedFilter = "All Games";
            }
            else if (action == $"{AppResources.FilterIncompleteGamesOption}")
            {
                _selectedFilter = "Incomplete Games";
            }

            // Retrieve cached game data and process the response
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

        private async void OnCopyTitleIdSwipeInvoked(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipeItem && swipeItem.BindingContext is GameItem gameItem)
            {
                string titleId = gameItem.TitleId;
                string gameName = gameItem.Name;

                string message = string.Format(AppResources.TitleIdClipboard, titleId, gameName);

                await Clipboard.Default.SetTextAsync(titleId);
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.TitleID}", message, "successic50.png", "Primary");
            }
        }

        private void OnToggleSearchClicked(object sender, EventArgs e)
        {
            GameSearchBar.IsVisible = !GameSearchBar.IsVisible;
            if (GameSearchBar.IsVisible)
            {
                GameSearchBar.Focus();
            }
        }
    }

    public class GameItem
    {
        public string Name { get; set; } = string.Empty;
        public string TitleId { get; set; } = string.Empty;
        public string CurrentAchievements { get; set; } = string.Empty;
        public string CurrentGamerscore { get; set; } = string.Empty;
        public string TotalGamerscore { get; set; } = string.Empty;
        public string ProgressPercentage { get; set; } = string.Empty;

        public string GamerscoreCombined
        {
            get => $"{CurrentGamerscore} / {TotalGamerscore} ({ProgressPercentage}%)";
        }

        private string _displayImage = string.Empty;

        public string DisplayImage
        {
            get => _displayImage;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _displayImage = "cirno.png";
                }
                else
                {
                    _displayImage = AddImageParameters(value);
                }
            }
        }

        public double ProgressValue
        {
            get
            {
                if (double.TryParse(ProgressPercentage, out double percentage))
                {
                    return percentage / 100.0;
                }
                return 0.0;
            }
        }

        private string AddImageParameters(string imageUrl)
        {
            if (imageUrl.Contains("store-images.s-microsoft.com"))
            {
                return $"{imageUrl}?w=256&h=256&format=jpg";
            }
            return imageUrl;
        }
    }
}