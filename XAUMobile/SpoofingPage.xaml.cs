using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Windows.Input;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;

namespace XAUMobile
{
    public partial class SpoofingPage : ContentPage, INotifyPropertyChanged
    {
        private readonly HttpClient _spoofingclient = ApiManagerService.Instance.GetSpoofingClient();
        private string _newSpoofingID = "";
        private string _currentSpoofingID = "";
        private bool _spoofingUpdate = false;
        private dynamic _gameInfoResponse = new JObject();
        private dynamic _gameStatsResponse = new JObject();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private ICommand _startSpoofingCommand;

        private List<string> _userAgents = new List<string>
    {
        "None",
        "XboxLm-Console/25398.4478.amd64fre.xb_flt_2405zn.240501-1900",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:108.0) Gecko/20100101 Firefox/108.0",
        "WindowsGameBarPresenceWriter/10.0.10011.16384",
    };

        private string _selectedUserAgent = String.Empty;

        public SpoofingPage()
        {
            InitializeComponent();

            BindingContext = this;
            _startSpoofingCommand = new Command(StartSpoofingCommandExecute);
            SelectedUserAgent = _userAgents.First(); // Default to the first user-agent (None)
        }

        public List<string> UserAgents => _userAgents;

        public string SelectedUserAgent
        {
            get => _selectedUserAgent;
            set
            {
                _selectedUserAgent = value;
                OnPropertyChanged(nameof(SelectedUserAgent));
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private async Task GetGameInfoAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_newSpoofingID))
                {
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.EnterValidTitleId}.", "erroric50.png", "RedError");
                    return;
                }

                SetupClientHeaders();
                var gameInfoResponse = await PostAsync($"https://{Hosts.TitleHub}/users/xuid({UserPage.Xuid})/titles/batch/decoration/GamePass,Achievement,Stats",
                    $"{{\"pfns\":null,\"titleIds\":[\"{_newSpoofingID}\"]}}");

                var gameStatsResponse = await PostAsync($"https://{Hosts.UserStats}/batch",
                    $"{{\"arrangebyfield\":\"xuid\",\"xuids\":[\"{UserPage.Xuid}\"],\"stats\":[{{\"name\":\"MinutesPlayed\",\"titleId\":\"{_newSpoofingID}\"}}]}}");

                if (gameInfoResponse.IsSuccessStatusCode)
                {
                    ProcessGameInfoResponse(await gameInfoResponse.Content.ReadAsStringAsync(), await gameStatsResponse.Content.ReadAsStringAsync());
                    _currentSpoofingID = _newSpoofingID;
                    await StartSpoofingAsync();
                }
                else
                {
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.FailedFetchTitleId}.", "erroric50.png", "RedError");

                    ResetUI();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetGameInfoAsync: {ex.Message}");
            }
        }

        private void SetupClientHeaders()
        {
            _spoofingclient.DefaultRequestHeaders.Clear();
            _spoofingclient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
            _spoofingclient.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
            _spoofingclient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
            _spoofingclient.DefaultRequestHeaders.Add(HeaderNames.Authorization, XAUTHService.AuthToken);
            _spoofingclient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-US");  //todo: use settings lang
        }

        private async Task<HttpResponseMessage> PostAsync(string url, string content)
        {
            return await _spoofingclient.PostAsync(url, new StringContent(content, Encoding.UTF8, "application/json"));
        }

        private void ProcessGameInfoResponse(string gameInfoResponseContent, string gameStatsResponseContent)
        {
            _gameInfoResponse = JObject.Parse(gameInfoResponseContent);
            _gameStatsResponse = JObject.Parse(gameStatsResponseContent);

            GameImage = _gameInfoResponse.titles[0].displayImage.ToString();
            GameName = $"{AppResources.Name}: {_gameInfoResponse.titles[0].name}";
            GameTitleID = $"{AppResources.TitleID}: {_gameInfoResponse.titles[0].titleId}";
            GamePFN = $"PFN: {_gameInfoResponse.titles[0].pfn}";
            GameType = $"{AppResources.Type}: {_gameInfoResponse.titles[0].type}";
            GameGamepass = $"Gamepass: {_gameInfoResponse.titles[0].gamePass.isGamePass}";
            GameDevices = $"{AppResources.Device}: {string.Join(", ", _gameInfoResponse.titles[0].devices)}";
            GameGamerscore = $"{AppResources.Gamerscore}: {_gameInfoResponse.titles[0].achievement.currentGamerscore}/{_gameInfoResponse.titles[0].achievement.totalGamerscore}";
            SpoofingButtonText = $"{AppResources.StopSpoofing}";

            UpdateGameTime();
            OnPropertyChanged(nameof(GameName), nameof(GameImage), nameof(GameTitleID), nameof(GamePFN), nameof(GameType), nameof(GameGamepass),
                nameof(GameDevices), nameof(GameGamerscore), nameof(SpoofingButtonText), nameof(GameTime));
        }

        private void UpdateGameTime()
        {
            try
            {
                var timePlayed = TimeSpan.FromMinutes(Convert.ToDouble(_gameStatsResponse.statlistscollection[0].stats[0].value));
                var formattedTime = $"{timePlayed.Days} {AppResources.Days}, {timePlayed.Hours} {AppResources.Hours} {AppResources.And} {timePlayed.Minutes} {AppResources.Minutes}";
                GameTime = $"{AppResources.TimePlayed}: " + formattedTime;
            }
            catch
            {
                GameTime = $"{AppResources.TimePlayed}: {AppResources.Unknown}";
            }
        }

        private async void StartSpoofingCommandExecute()
        {
            if (SpoofingButtonText == $"{AppResources.StartSpoofing}")
            {
                await GetGameInfoAsync();
            }
            else
            {
                ResetUI();
                _cancellationTokenSource.Cancel();
                await _spoofingclient.DeleteAsync($"https://{Hosts.PresenceHeartBeat}/users/xuid({UserPage.Xuid})/devices/current");
                return;
            }
        }

        private async Task StartSpoofingAsync()
        {
            SetupSpoofingClientHeaders();
            _spoofingUpdate = true;
            _cancellationTokenSource = new CancellationTokenSource();

            Stopwatch visualStopwatch = new Stopwatch();
            visualStopwatch.Start();

            var requestBodyJson = CreateSpoofingRequestBody(_currentSpoofingID);
            var requestBodyContent = requestBodyJson.ToString();

            await SendHeartbeatAsync(new StringContent(requestBodyContent, Encoding.UTF8, "application/json"));

            try
            {
                Task heartbeatTask = SendHeartbeatPeriodicallyAsync(requestBodyContent, _cancellationTokenSource.Token);

                while (_spoofingUpdate)
                {
                    TimeSpan elapsedTime = visualStopwatch.Elapsed;
                    TimerText = elapsedTime.ToString(@"hh\:mm\:ss");

                    this.Dispatcher.Dispatch(() =>
                    {
                        OnPropertyChanged(nameof(TimerText));
                    });

                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }

                _cancellationTokenSource.Cancel();
                await heartbeatTask;
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Spoofing stopped.");
            }
        }

        private async Task SendHeartbeatPeriodicallyAsync(string requestBodyContent, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine("Sending heartbeat...");
                        var requestBody = new StringContent(requestBodyContent, Encoding.UTF8, "application/json");
                        await SendHeartbeatAsync(requestBody);
                    }
                }
                catch (TaskCanceledException)
                {
                    break; 
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error sending heartbeat: {ex.Message}");
                }
            }
        }

        private void SetupSpoofingClientHeaders()
        {
            _spoofingclient.DefaultRequestHeaders.Clear();
            _spoofingclient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
            _spoofingclient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
            _spoofingclient.DefaultRequestHeaders.Add(HeaderNames.Authorization, XAUTHService.AuthToken);
            if (SelectedUserAgent != "None") // Only add the User-Agent header if it's not "None"
            {
                _spoofingclient.DefaultRequestHeaders.Add("User-Agent", SelectedUserAgent);
            }
        }

        private JObject CreateSpoofingRequestBody(string spoofingID)
        {
            return new JObject
            {
                ["titles"] = new JArray
                {
                    new JObject
                    {
                        ["expiration"] = 600,
                        ["id"] = spoofingID,
                        ["state"] = "active",
                        ["sandbox"] = "RETAIL"
                    }
                }
            };
        }

        private async Task SendHeartbeatAsync(StringContent requestBody)
        {
            try
            {
                var endpoint = $"https://{Hosts.PresenceHeartBeat}/users/xuid({UserPage.Xuid})/devices/current";
                var response = await _spoofingclient.PostAsync(endpoint, requestBody);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Failed to send heartbeat: {response.StatusCode}");
                    ResetUI();
                    _cancellationTokenSource.Cancel();
                    await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.FailedToSpoofGame}. {AppResources.FailedToSpoofGameMessage}. Error code: {(int)response.StatusCode} - {response.StatusCode}", "erroric50.png", "RedError");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in SendHeartbeatAsync: {ex.Message}");
            }
        }

        private void ResetUI()
        {
            GameName = $"{AppResources.Name}: ";
            GameTitleID = $"{AppResources.TitleID}: ";
            GameImage = "logo1.png";
            GamePFN = "PFN: ";
            GameType = $"{AppResources.Type}: ";
            GameGamepass = "Gamepass: ";
            GameDevices = $"{AppResources.Device}: ";
            GameGamerscore = $"{AppResources.Gamerscore}: ?/?";
            GameTime = $"{AppResources.TimePlayed} ";
            TimerText = "00:00:00";
            SpoofingButtonText = $"{AppResources.StartSpoofing}";

            OnPropertyChanged(nameof(GameName), nameof(GameImage), nameof(GameTitleID), nameof(GamePFN), nameof(GameType), nameof(GameGamepass),
                nameof(GameDevices), nameof(GameGamerscore), nameof(SpoofingButtonText), nameof(GameTime), nameof(TimerText));
        }

        public string NewSpoofingID
        {
            get => _newSpoofingID;
            set
            {
                _newSpoofingID = value;
                OnPropertyChanged(nameof(NewSpoofingID));
            }
        }

        public ICommand StartSpoofingCommand => _startSpoofingCommand ??= new Command(StartSpoofingCommandExecute);

        public string GameName { get; private set; } = $"{AppResources.Name}: ";
        public string GameTitleID { get; private set; } = $"{AppResources.TitleID}: ";
        public string GamePFN { get; private set; } = "PFN: ";
        public string GameType { get; private set; } = $"{AppResources.Type}: ";
        public string GameGamepass { get; private set; } = "Gamepass: ";
        public string GameDevices { get; private set; } = $"{AppResources.Device}: ";
        public string GameGamerscore { get; private set; } = $"{AppResources.Gamerscore}: ?/?";
        public string GameImage { get; private set; } = "logo1.png";
        public string GameTime { get; private set; } = $"{AppResources.TimePlayed}: ";
        public string SpoofingButtonText { get; private set; } = $"{AppResources.StartSpoofing}";
        public string TimerText { get; private set; } = "00:00:00";
    }
}