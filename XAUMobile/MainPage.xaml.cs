using Newtonsoft.Json.Linq;
using System.Diagnostics;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;

namespace XAUMobile

{
    public partial class MainPage : ContentPage
    {
        private bool hasCheckedForUpdate = false;

        public MainPage()
        {
            InitializeComponent();

            PrivacyToggle.IsToggled = SettingsService.IsPrivacyEnabled;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            AuthTokenEntry.Text = XAUTHService.AuthToken;

            if (!hasCheckedForUpdate)
            {
                if (ShouldCheckForUpdate())
                {
                    Console.WriteLine("Checking for updates...");
                    _ = OnCheckForUpdate();
                }
                else
                {
                    Console.WriteLine("Skipping update check because it hasn't been 24 hours since the last check.");
                }
                hasCheckedForUpdate = true;
            }
        }

        private bool ShouldCheckForUpdate()
        {
            // Get the last update check date
            DateTime lastCheckDate = SettingsService.LastUpdateCheckDate;
            DateTime currentDate = DateTime.Now;

            // Check if more than a day has passed since the last check
            return (currentDate - lastCheckDate).TotalDays >= 1;
        }

        private async Task OnCheckForUpdate()
        {
            // check for announcements also
            var announcementsService = new AnnouncementsService();
            try
            {
                JObject announcements = await announcementsService.FetchAndSaveAnnouncementsFromApiAsync();
                Console.WriteLine("Fetched announcements: " + announcements.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching announcements: {ex.Message}");
            }

            // update stuff here
            SettingsService.LastUpdateCheckDate = DateTime.Now;

            var (isUpdateAvailable, latestVersion, downloadLink, changelog) = await UpdateService.CheckForUpdatesAsync();

            if (isUpdateAvailable)
            {
                bool shouldUpdate = await PromptForUpdate(latestVersion, downloadLink, changelog);
                if (shouldUpdate)
                {
                    await CallToActionHelper.ShowPopup(CallToActionControl, $"{AppResources.Downloading}...", $"{AppResources.DownloadingPrompt}", "updateic50.png", "Primary");
                    await UpdateService.DownloadAndInstallApkAsync(downloadLink);
                }
            }
            else
            {
                // if app is already up to date, continue.. no need to show anything. enable for debugging only
                //await CallToActionHelper.ShowPopup(CallToActionControl, $"{AppResources.UpdateCheck}", $"{AppResources.UpdateCheckAlreadyUpToDate}", "successic50.png", "Primary");
            }

            await Task.Delay(TimeSpan.FromSeconds(60));
        }

        private async Task<bool> PromptForUpdate(string latestVersion, string downloadLink, string changelog)
        {
            string updateMessage = string.Format(AppResources.UpdateMessage, latestVersion, changelog).Replace("\\n", Environment.NewLine);

            return await CallToActionHelper.ShowDialog(CallToActionControl,
                $"{AppResources.UpdateAvailable}",
                updateMessage,
                $"{AppResources.Update}",
                $"{AppResources.Later}",
                "updateic50.png", "Primary"
            );
        }

        private async void ForceCheckForUpdateBtn(object sender, EventArgs e)
        {
            await OnCheckForUpdate();
        }

        private void AuthTokenEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            XAUTHService.AuthToken = e.NewTextValue;
            Debug.WriteLine("Auth Token Changed: " + XAUTHService.AuthToken);
        }

        private void PrivacyToggle_Toggled(object? sender, ToggledEventArgs e)
        {
            SettingsService.IsPrivacyEnabled = e.Value;
        }

        private async void EnterApp(object sender, EventArgs e)
        {
            string authToken = XAUTHService.AuthToken;

            if (!string.IsNullOrEmpty(authToken))
            {
                // Token exists, navigate to the UserPage
                await Shell.Current.GoToAsync("//UserPage");
            }
            else
            {
                // No token saved, show an error message (e.g., using DisplayAlert)
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.XauthNotSet}", "erroric50.png", "RedError");
            }
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string loginMessage = AppResources.MainPagePromptForLoginPage.Replace("\\n", Environment.NewLine);

            bool isContinue = await CallToActionHelper.ShowDialog(CallToActionControl, $"{AppResources.XboxLogin}", loginMessage, $"{AppResources.Continue}", $"{AppResources.Cancel}", "infoic50.png", "Primary");

            if (isContinue)
            {
                await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
        }
    }
}