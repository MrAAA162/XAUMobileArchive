using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;

namespace XAUMobile
{
    public partial class OtherUserAchievementsPage : ContentPage
    {
        private readonly HttpClient _client = ApiManagerService.Instance.GetXboxApiClient();
        private readonly string _xuid;
        private readonly GameItem _gameItem;
        public ObservableCollection<Achievement> Achievements { get; } = new ObservableCollection<Achievement>();
        public ObservableCollection<Achievement> FilteredAchievements { get; } = new ObservableCollection<Achievement>();

        private string currentFilter = "All";
        private string currentSort = "Default";

        public OtherUserAchievementsPage(string xuid, GameItem gameItem)
        {
            InitializeComponent();
            _xuid = xuid;
            _gameItem = gameItem;
            BindingContext = this;
            
            LoadAchievements(_gameItem);
        }

        private async void LoadAchievements(GameItem selectedGame)
        {
            try
            {
                // Clear existing achievements
                AchievementsIndicator.IsRunning = true;
                AchievementsIndicator.IsVisible = true;
                Achievements.Clear();
                FilteredAchievements.Clear();

                string apiUrl = $"https://{Hosts.Achievements}/users/xuid({_xuid})/achievements?titleId={selectedGame.TitleId}&maxItems=1000";

                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion4);
                _client.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
                _client.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
                _client.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-US"); //todo: use settings lang
                _client.DefaultRequestHeaders.Add(HeaderNames.Authorization, XAUTHService.AuthToken);
                _client.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Achievements);
                _client.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);

                HttpResponseMessage response = await _client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                string responseContent = await response.Content.ReadAsStringAsync();

                dynamic achievementResponse = JObject.Parse(responseContent);

                foreach (var achievement in achievementResponse["achievements"])
                {
                    var titleAssociation = achievement["titleAssociations"]?[0];
                    var titleId = titleAssociation?["id"]?.ToString() ?? string.Empty;

                    var gsValue = (achievement["rewards"] != null && achievement["rewards"].HasValues)
                        ? achievement["rewards"][0]?["value"]?.ToString() ?? string.Empty
                        : string.Empty;

                    var newAchievement = new Achievement
                    {
                        Id = achievement["id"]?.ToString() ?? string.Empty,
                        ServiceConfigId = achievement["serviceConfigId"]?.ToString() ?? string.Empty,
                        Name = achievement["name"]?.ToString() ?? string.Empty,
                        Description = achievement["description"]?.ToString() ?? string.Empty,
                        ProgressState = achievement["progressState"]?.ToString() ?? string.Empty,
                        TitleId = titleId,
                        GameName = string.Empty,
                        TimeUnlocked = achievement["progression"]["timeUnlocked"]?.ToString() ?? string.Empty,
                        GSValue = gsValue,
                        CurrentCategory = achievement["rarity"]["currentCategory"]?.ToString() ?? string.Empty,
                        CurrentPercentage = achievement["rarity"]["currentPercentage"]?.ToString() ?? string.Empty,
                        Requirements = achievement["progression"]["requirements"]?.ToObject<List<Requirement>>() ?? new List<Requirement>()
                    };
                    Achievements.Add(newAchievement);
                }

                FilterAndSortAchievements();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading achievements: {ex.Message}");
                await CallToActionHelper.ShowMessage(CallToActionControl, "Error", $"Error loading achievements: {ex.Message}", "erroric50.png", "RedError");
            }
            finally
            {
                AchievementsIndicator.IsRunning = false;
                AchievementsIndicator.IsVisible = false;
            }
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            var filterOptions = new []
            { AppResources.AchievementFilterAll, AppResources.AchievementFilterUnlocked, AppResources.AchievementFilterLocked };

            var action = DisplayActionSheet(AppResources.AchievementFilter, AppResources.Cancel, null, filterOptions);

            action.ContinueWith(task =>
            {
                if (task.Result != AppResources.Cancel)
                {
                    string selectedFilter = task.Result == AppResources.AchievementFilterUnlocked ? "Unlocked" :
                                            task.Result == AppResources.AchievementFilterLocked ? "Locked" : "All";

                    if (selectedFilter != currentFilter)
                    {
                        currentFilter = selectedFilter;
                        FilterAndSortAchievements();
                    }
                }
            });
        }

        private void OnSortClicked(object sender, EventArgs e)
        {
            var sortOptions = new[] { "Default (ID)", "Unlock Time (Oldest First)" };

            var action = DisplayActionSheet("Sort Achievements", AppResources.Cancel, null, sortOptions);

            action.ContinueWith(task =>
            {
                if (task.Result != AppResources.Cancel)
                {
                    string selectedSort = task.Result == "Unlock Time (Oldest First)" ? "UnlockTime" : "Default";

                    if (selectedSort != currentSort)
                    {
                        currentSort = selectedSort;
                        FilterAndSortAchievements();
                    }
                }
            });
        }

        private void FilterAndSortAchievements()
        {
            FilteredAchievements.Clear();

            // apply filter
            IEnumerable<Achievement> filteredAchievements;
            if (currentFilter == "Unlocked")
            {
                filteredAchievements = Achievements.Where(a => a.ProgressState == "Achieved");
            }
            else if (currentFilter == "Locked")
            {
                filteredAchievements = Achievements.Where(a => a.ProgressState != "Achieved");
            }
            else
            {
                filteredAchievements = Achievements;
            }

            IEnumerable<Achievement> sortedAchievements;
            if (currentSort == "UnlockTime")
            {
                sortedAchievements = filteredAchievements
                    .OrderBy(a => a.ProgressState != "Achieved") // Achieved first
                    .ThenBy(a => a.ProgressState == "Achieved" ? a.ParsedTimeUnlocked : DateTime.MaxValue);
            }
            else
            {
                sortedAchievements = filteredAchievements;
            }

            foreach (var achievement in sortedAchievements)
            {
                FilteredAchievements.Add(achievement);
            }

            this.Dispatcher.Dispatch(() =>
            {
                NoAchievementsLabel.IsVisible = FilteredAchievements.Count == 0;
            });
        }

        public class Achievement : INotifyPropertyChanged
        {
            private string _progressState = string.Empty;
            private string _timeUnlocked = string.Empty;
            private DateTime? _parsedTimeUnlocked;

            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;

            public string ProgressState
            {
                get => _progressState;
                set
                {
                    if (_progressState != value)
                    {
                        _progressState = value;
                        OnPropertyChanged(nameof(ProgressState));
                    }
                }
            }

            public string? TimeUnlocked
            {
                get => _timeUnlocked;
                set
                {
                    if (_timeUnlocked != value)
                    {
                        _timeUnlocked = value ?? string.Empty;
                        _parsedTimeUnlocked = null;
                        OnPropertyChanged(nameof(TimeUnlocked));
                    }
                }
            }

            public DateTime ParsedTimeUnlocked
            {
                get
                {
                    if (_parsedTimeUnlocked.HasValue)
                        return _parsedTimeUnlocked.Value;

                    if (DateTime.TryParse(_timeUnlocked, out DateTime result))
                    {
                        _parsedTimeUnlocked = result;
                        return result;
                    }

                    _parsedTimeUnlocked = DateTime.MaxValue;
                    return DateTime.MaxValue;
                }
            }

            public string ServiceConfigId { get; set; } = string.Empty;
            public string TitleId { get; set; } = string.Empty;
            public string GameName { get; set; } = string.Empty;
            public string GSValue { get; set; } = string.Empty;
            public string CurrentCategory { get; set; } = string.Empty;
            public string CurrentPercentage { get; set; } = string.Empty;
            public List<Requirement> Requirements { get; set; } = new List<Requirement>();

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class Requirement
        {
            public string Id { get; set; } = string.Empty;
            public string Current { get; set; } = string.Empty;
            public string Target { get; set; } = string.Empty;
        }
    }
}
