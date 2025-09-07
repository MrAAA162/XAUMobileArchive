using Newtonsoft.Json.Linq;

namespace XAUMobile
{
    public class AnnouncementsService
    {
        private readonly string _filePath;
        private readonly HttpClient _client;

        public AnnouncementsService()
        {
            _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "announcements.json");
            _client = ApiManagerService.Instance.GetXAUApiClient();
        }

        public async Task<JObject?> GetCachedAnnouncementsAsync()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string cachedJson = await File.ReadAllTextAsync(_filePath);
                    JObject cachedData = JObject.Parse(cachedJson);
                    Console.WriteLine("Using saved announcements data from file.");

                    UpdateAnnouncementSettings(cachedData);
                    return cachedData;
                }
                else
                {
                    Console.WriteLine("No cached announcements available.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching cached data: {ex.Message}");
                throw;
            }
        }

        public async Task<JObject> FetchAndSaveAnnouncementsFromApiAsync()
        {
            try
            {
                string currentVersion = UpdateService.GetAppVersion()?.Trim() ?? "unknown";
                string currentLanguage = SettingsService.SelectedLanguage ?? "unknown";

                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, HeaderValues.UserAgentMeowMeow);
                _client.DefaultRequestHeaders.Add(HeaderNames.XAUVersion, currentVersion);
                _client.DefaultRequestHeaders.Add(HeaderNames.XAULanguage, currentLanguage);
                _client.DefaultRequestHeaders.Add(HeaderNames.XAU, HeaderValues.XAU);
                HttpResponseMessage response = await _client.GetAsync($"https://{Hosts.XAUApi}/api/announcements");
                Console.WriteLine("Fetching announcements from API");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    UpdateAnnouncementSettings(data);

                    await File.WriteAllTextAsync(_filePath, json);
                    Console.WriteLine("Announcements data saved to file.");

                    return data;
                }
                else
                {
                    throw new Exception("Failed to load announcements from API.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }

        private void UpdateAnnouncementSettings(JObject data)
        {
            var latestId = data["announcements"]?["latest"]?["id"]?.ToString();
            if (!string.IsNullOrEmpty(latestId))
            {
                if (SettingsService.AnnouncementId != latestId)
                {
                    SettingsService.HasSeenAnnouncement = false;
                    SettingsService.AnnouncementId = latestId;
                    UpdateAnnouncementIcon();
                }
            }
        }

        private void UpdateAnnouncementIcon()
        {
            // Access the AppShell and update the announcement icon
            if (Application.Current.MainPage is AppShell appShell)
            {
                appShell.UpdateAnnouncementsIndicator();
            }
        }
    }
}