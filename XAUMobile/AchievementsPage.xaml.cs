using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;

namespace XAUMobile
{
    public partial class AchievementsPage : ContentPage
    {
        private readonly HttpClient _client = ApiManagerService.Instance.GetXboxApiClient();
        public ObservableCollection<Achievement> Achievements { get; } = new ObservableCollection<Achievement>();
        public ObservableCollection<Achievement> FilteredAchievements { get; } = new ObservableCollection<Achievement>();
        public ICommand UnlockCommand { get; }
        public bool IsEventBasedGame { get; set; }

        private string currentFilter = "All";

        public AchievementsPage(GameItem selectedGame)
        {
            InitializeComponent();
            BindingContext = this;

            // Initialize the Unlock Command/Achievement
            UnlockCommand = new Command<Achievement>(UnlockAchievement);

            // Load achievements for the selected game
            LoadAchievements(selectedGame);
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

                string apiUrl = $"https://{Hosts.Achievements}/users/xuid({UserPage.Xuid})/achievements?titleId={selectedGame.TitleId}&maxItems=1000";

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

                // Enable to debug console response
                //// Console.WriteLine(achievementResponse);

                // Get game name once from the first achievement
                string gameName = string.Empty;
                if (achievementResponse["achievements"].Count > 0)
                {
                    var firstAchievement = achievementResponse["achievements"][0];
                    var titleAssociation = firstAchievement["titleAssociations"]?[0];
                    gameName = titleAssociation?["name"]?.ToString() ?? string.Empty;
                }

                // Check for event-based achievements
                bool isEventBasedGame = false;
                foreach (var achievement in achievementResponse["achievements"])
                {
                    if (achievement["progression"]["requirements"] != null &&
                        achievement["progression"]["requirements"].Count > 0)
                    {
                        foreach (var requirement in achievement["progression"]["requirements"])
                        {
                            if (requirement["id"].ToString() != "00000000-0000-0000-0000-000000000000")
                            {
                                isEventBasedGame = true;
                                break;
                            }
                        }
                        if (isEventBasedGame) break;
                    }
                }

                // set isEventBasedGame to disable the achievement cards for that game and show err message
                IsEventBasedGame = isEventBasedGame;

                // Show message if it's an event-based game
                if (isEventBasedGame)
                {
                    _ = CallToActionHelper.ShowMessage(CallToActionControl,
                        $"{AppResources.UnsupportedTitle}",
                        $"{gameName} {AppResources.UnsupportedTitleMessage}.",
                        "erroric50.png",
                        "RedError");
                }

                // Process each achievement item
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
                        GameName = gameName,
                        TimeUnlocked = achievement["progression"]["timeUnlocked"]?.ToString() ?? string.Empty,
                        GSValue = gsValue,
                        CurrentCategory = achievement["rarity"]["currentCategory"]?.ToString() ?? string.Empty,
                        CurrentPercentage = achievement["rarity"]["currentPercentage"]?.ToString() ?? string.Empty,
                        Requirements = achievement["progression"]["requirements"]?.ToObject<List<Requirement>>() ?? new List<Requirement>()
                    };
                    newAchievement.UpdateSwipeEnabled(isEventBasedGame);
                    Achievements.Add(newAchievement);
                }

                FilterAchievements("All");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading achievements: {ex.Message}");
            }
            finally
            {
                AchievementsIndicator.IsRunning = false;
                AchievementsIndicator.IsVisible = false;
            }
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            var filterOptions = new[]
            { AppResources.AchievementFilterAll, AppResources.AchievementFilterUnlocked, AppResources.AchievementFilterLocked };

            var action = DisplayActionSheet(AppResources.AchievementFilter, AppResources.Cancel, null, filterOptions);

            action.ContinueWith(task =>
            {
                if (task.Result != AppResources.Cancel)
                {
                    // Map the localized string to internal filter values to avoid a lot of re-write
                    string selectedFilter = task.Result == AppResources.AchievementFilterUnlocked ? "Unlocked" :
                                            task.Result == AppResources.AchievementFilterLocked ? "Locked" : "All";

                    if (selectedFilter != currentFilter)
                    {
                        currentFilter = selectedFilter;
                        FilterAchievements(selectedFilter);
                    }
                }
            });
        }

        // Filter achievements based on the selected filter
        private void FilterAchievements(string filter)
        {
            FilteredAchievements.Clear();

            if (filter == "Unlocked")
            {
                foreach (var achievement in Achievements.Where(a => a.ProgressState == "Achieved"))
                {
                    FilteredAchievements.Add(achievement);
                }
            }
            else if (filter == "Locked")
            {
                foreach (var achievement in Achievements.Where(a => a.ProgressState != "Achieved"))
                {
                    FilteredAchievements.Add(achievement);
                }
            }
            else
            {
                foreach (var achievement in Achievements)
                {
                    FilteredAchievements.Add(achievement);
                }
            }

            this.Dispatcher.Dispatch(() =>
            {
                NoAchievementsLabel.IsVisible = FilteredAchievements.Count == 0;
            });
        }

        private async void UnlockAchievement(Achievement achievement)
        {
            if (achievement.ProgressState == "Achieved")
            {
                await CallToActionHelper.ShowPopup(CallToActionControl, $"{AppResources.AlreadyUnlocked}", $"{achievement.Name} {AppResources.AlreadyUnlockedMessage}.", "erroric50.png", "RedError");
                return;
            }

            // Check if the achievement is event-based (legacy code before i disabled the swipe for event-based games)
            if (achievement.Requirements != null && achievement.Requirements.Count > 0)
            {
                bool hasNonZeroIdRequirement = false;
                foreach (var requirement in achievement.Requirements)
                {
                    if (requirement.Id != "00000000-0000-0000-0000-000000000000")
                    {
                        hasNonZeroIdRequirement = true;
                        break;
                    }
                }

                if (hasNonZeroIdRequirement)
                {
                    await CallToActionHelper.ShowPopup(CallToActionControl, $"{AppResources.UnsupportedAchievement}", $"{achievement.Name} {AppResources.UnsupportedAchievementMessage}.", "erroric50.png", "RedError");

                    return;
                }
            }

            try
            {
                bool confirmUnlock = await CallToActionHelper.ShowDialog(CallToActionControl, $"{AppResources.AchievementConfirmUnlock}", $"{AppResources.AchievementConfirmUnlockMessage} {achievement.Name}?", $"{AppResources.Yes}", $"{AppResources.No}", "infoic50.png", "Primary");

                if (confirmUnlock)
                {
                    // achievement unlock request body
                    var requestbody = new
                    {
                        action = "progressUpdate",
                        serviceConfigId = achievement.ServiceConfigId,
                        titleId = achievement.TitleId,
                        userId = UserPage.Xuid,
                        achievements = new[]
                        {
                            new
                            {
                                id = achievement.Id,
                                percentComplete = 100
                            }
                        }
                    };

                    var jsonRequest = JObject.FromObject(requestbody);
                    var bodyconverted = new StringContent(jsonRequest.ToString(), Encoding.UTF8, "application/json");

                    _client.DefaultRequestHeaders.Clear();
                    _client.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
                    _client.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
                    _client.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
                    _client.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-US");  //todo: use settings lang
                    _client.DefaultRequestHeaders.Add(HeaderNames.Authorization, XAUTHService.AuthToken);
                    _client.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Achievements);
                    _client.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
                    _client.DefaultRequestHeaders.Add("User-Agent", "XboxServicesAPI/2021.10.20211005.0 c");
                    if (SettingsService.IsSignatureEnabled)
                    {
                        _client.DefaultRequestHeaders.Add(HeaderNames.Signature, SettingsService.GetCurrentSignature());
                    }

                    HttpResponseMessage response = await _client.PostAsync($"https://{Hosts.Achievements}/users/xuid({UserPage.Xuid})/achievements/{achievement.ServiceConfigId}/update", bodyconverted);
                    response.EnsureSuccessStatusCode();

                    // Update achievement after successful unlock
                    achievement.ProgressState = "Achieved";
                    Console.WriteLine($"Achievement unlocked: {achievement.Name}");
                    await CallToActionHelper.ShowPopup(CallToActionControl, $"{AppResources.AchievementUnlocked}", $"{achievement.Name} {AppResources.AchievementUnlockedMessage}!", "successic50.png", "Primary");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request error: {ex.Message}");
                await DisplayAlert("Error", $"HTTP request error: {ex.Message}", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unlocking achievement: {ex.Message}");
            }
        }

        private async void OnBulkUnlockClicked(object sender, EventArgs e)
        {
            try  // this shit still in beta dont enable
            {
                // Show input dialog for achievement IDs
                string result = await DisplayPromptAsync(
                    "Bulk Unlock Achievements",
                    "Enter achievement IDs separated by commas (e.g., 4,15,20):",
                    "OK",
                    "Cancel",
                    placeholder: "4,15,20");

                if (string.IsNullOrWhiteSpace(result))
                    return;

                // Parse the input IDs
                var inputIds = result.Split(',')
                    .Select(id => id.Trim())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToList();

                if (inputIds.Count == 0)
                {
                    await DisplayAlert("Error", "No valid IDs entered.", "OK");
                    return;
                }

                // Find matching achievements that aren't already unlocked
                var achievementsToUnlock = new List<Achievement>();
                var notFoundIds = new List<string>();
                var alreadyUnlockedIds = new List<string>();

                foreach (var inputId in inputIds)
                {
                    var achievement = Achievements.FirstOrDefault(a => a.Id.Equals(inputId, StringComparison.OrdinalIgnoreCase));

                    if (achievement == null)
                    {
                        notFoundIds.Add(inputId);
                    }
                    else if (achievement.ProgressState == "Achieved")
                    {
                        alreadyUnlockedIds.Add(inputId);
                    }
                    else
                    {
                        // Check if achievement is event-based (unsupported)
                        bool isEventBased = achievement.Requirements != null &&
                            achievement.Requirements.Any(req => req.Id != "00000000-0000-0000-0000-000000000000");

                        if (isEventBased)
                        {
                            await DisplayAlert("Error", $"Achievement {inputId} ({achievement.Name}) is event-based and cannot be unlocked.", "OK");
                            return;
                        }

                        achievementsToUnlock.Add(achievement);
                    }
                }

                if (notFoundIds.Count > 0)
                {
                    await DisplayAlert("Warning", $"Achievement IDs not found: {string.Join(", ", notFoundIds)}", "OK");
                }

                if (alreadyUnlockedIds.Count > 0)
                {
                    await DisplayAlert("Warning", $"Already unlocked: {string.Join(", ", alreadyUnlockedIds)}", "OK");
                }

                if (achievementsToUnlock.Count == 0)
                {
                    await DisplayAlert("Info", "No achievements to unlock.", "OK");
                    return;
                }

                // Show confirmation
                string achievementNames = string.Join("\n", achievementsToUnlock.Select(a => $"• {a.Name} (ID: {a.Id})"));
                bool confirm = await DisplayAlert(
                    "Confirm Bulk Unlock",
                    $"Unlock {achievementsToUnlock.Count} achievement(s)?\n\n{achievementNames}",
                    "Yes",
                    "No");

                if (!confirm)
                    return;

                // Show loading
                AchievementsIndicator.IsRunning = true;
                AchievementsIndicator.IsVisible = true;

                // Perform bulk unlock
                await BulkUnlockAchievements(achievementsToUnlock);

                // Refresh the page  causing UI bug? investigate u idiot
                if (Achievements.Count > 0)
                {
                    var firstAchievement = Achievements.First();
                    var gameItem = new GameItem
                    {
                        TitleId = firstAchievement.TitleId
                    };
                    LoadAchievements(gameItem);
                }

                await DisplayAlert("Success", $"Successfully unlocked {achievementsToUnlock.Count} achievement(s)!", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in bulk unlock: {ex.Message}");
                await DisplayAlert("Error", $"Failed to unlock achievements: {ex.Message}", "OK");
            }
            finally
            {
                AchievementsIndicator.IsRunning = false;
                AchievementsIndicator.IsVisible = false;
            }
        }

        private async Task BulkUnlockAchievements(List<Achievement> achievements)
        {
            if (achievements.Count == 0) return;

            var firstAchievement = achievements.First();

            var requestBody = new
            {
                action = "progressUpdate",
                serviceConfigId = firstAchievement.ServiceConfigId,
                titleId = firstAchievement.TitleId,
                userId = UserPage.Xuid,
                achievements = achievements.Select(a => new
                {
                    id = a.Id,
                    percentComplete = 100
                }).ToArray()
            };

            var jsonRequest = JObject.FromObject(requestBody);
            var bodyContent = new StringContent(jsonRequest.ToString(), Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
            _client.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
            _client.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
            _client.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-US");  //todo: use settings lang
            _client.DefaultRequestHeaders.Add(HeaderNames.Authorization, XAUTHService.AuthToken);
            _client.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Achievements);
            _client.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
            _client.DefaultRequestHeaders.Add("User-Agent", "XboxServicesAPI/2021.10.20211005.0 c");

            if (SettingsService.IsSignatureEnabled)
            {
                _client.DefaultRequestHeaders.Add(HeaderNames.Signature, SettingsService.GetCurrentSignature());
            }

            string url = $"https://{Hosts.Achievements}/users/xuid({UserPage.Xuid})/achievements/{firstAchievement.ServiceConfigId}/update";
            HttpResponseMessage response = await _client.PostAsync(url, bodyContent);
            response.EnsureSuccessStatusCode();

            foreach (var achievement in achievements)
            {
                achievement.ProgressState = "Achieved";
                achievement.TimeUnlocked = DateTime.Now.ToString();
            }

            Console.WriteLine($"Bulk unlocked {achievements.Count} achievements");
        }

        private void OnAchievementTapped(object sender, EventArgs e) // for the noobies that dont know how to use swipe lol
        {
            if (sender is Element element)
            {
                var swipeView = element.Parent as SwipeView;
                if (swipeView != null &&
                    swipeView.Parent is ViewCell viewCell &&
                    viewCell.BindingContext is Achievement achievement)
                {
                    if (achievement.ProgressState != "Achieved")
                    {
                        swipeView.Open(OpenSwipeItem.RightItems);
                    }
                }
            }
        }

        public class Achievement : INotifyPropertyChanged
        {
            private string _progressState = string.Empty;
            private string _timeUnlocked = string.Empty;
            private bool _isSwipeEnabled = true;

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
                        UpdateSwipeEnabled();

                        if (_progressState == "Achieved")
                        {
                            TimeUnlocked = DateTime.Now.ToString();
                        }
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
                        OnPropertyChanged(nameof(TimeUnlocked));
                    }
                }
            }

            public bool IsSwipeEnabled
            {
                get => _isSwipeEnabled;
                private set
                {
                    if (_isSwipeEnabled != value)
                    {
                        _isSwipeEnabled = value;
                        OnPropertyChanged(nameof(IsSwipeEnabled));
                    }
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

            public void UpdateSwipeEnabled(bool isEventBasedGame = false)
            {
                IsSwipeEnabled = !isEventBasedGame && ProgressState != "Achieved";
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