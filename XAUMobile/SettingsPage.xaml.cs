using System.Globalization;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;

namespace XAUMobile
{
    public partial class SettingsPage : ContentPage
    {

        public static Dictionary<string, CultureInfo> SupportedLanguages { get; } = new()
    {
        { "English", new CultureInfo("en-US") },
        { "Brazilian Portuguese", new CultureInfo("pt-BR") },
        { "German", new CultureInfo("de-DE") },
        { "Polish", new CultureInfo("pl-PL") },
        { "Portuguese", new CultureInfo("pt-PT") },
        { "Spanish", new CultureInfo("es-ES") }
    };

        public SettingsPage()
        {
            InitializeComponent();

            // Populate language options
            LanguagePicker.ItemsSource = new List<string>(SupportedLanguages.Keys);
            var savedLanguage = SettingsService.SelectedLanguage;
            LanguagePicker.SelectedItem = savedLanguage ?? "English";

            // theme and color settings
            ThemeSwitch.IsToggled = SettingsService.IsNightMode;
            PrimaryColorPicker.ItemsSource = ThemeService.GetColorOptions();
            PrimaryColorPicker.SelectedItem = ThemeService.GetSavedColorOption();
            PrimaryColorPicker.SelectedIndexChanged += (sender, e) =>
            {
                if (PrimaryColorPicker.SelectedItem != null)
                {
                    ThemeService.HandlePrimaryColorSelection(PrimaryColorPicker.SelectedItem);
                    UpdateActiveThemeLabel();
                }
            };
            ThemeService.ApplySavedTheme();
            UpdateActiveThemeLabel();

            // version label
            VersionLabel.Text = $"XAUMobile {AppResources.Version} {UpdateService.GetAppVersion()}";

            // signature settings
            SignatureSwitch.IsToggled = SettingsService.IsSignatureEnabled;
            SignatureEntry.Text = SettingsService.UserSignature;

            SignatureSwitch.Toggled += OnSignatureSwitchToggled;
            SignatureEntry.TextChanged += OnSignatureEntryTextChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ((AppShell)Shell.Current).SetToolbarItemsVisible(false);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ((AppShell)Shell.Current).SetToolbarItemsVisible(true);
        }

        private void OnLanguageSelected(object sender, EventArgs e)
        {
            var selectedLanguage = LanguagePicker.SelectedItem?.ToString();

            if (selectedLanguage != null && SupportedLanguages.TryGetValue(selectedLanguage, out var culture))
            {
                LocalizationResourceService.Instance.SetCulture(culture);

                SettingsService.SelectedLanguage = selectedLanguage;
            }

            UpdateActiveThemeLabel(); // update selected theme label hack for now

            VersionLabel.Text = $"XAUMobile {AppResources.Version} {UpdateService.GetAppVersion()}";
        }

        private void UpdateActiveThemeLabel()
        {
            ActiveThemeLabel.Text = $"{AppResources.SelectedColor}: {ThemeService.GetActiveThemeName()}";
        }

        private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            bool isNightMode = e.Value;

            ThemeService.ApplyTheme(isNightMode);

            SettingsService.IsNightMode = isNightMode;
        }

        private async void OnCheckForUpdatesClicked(object sender, EventArgs e)
        {
            CheckForUpdatesButton.IsEnabled = false;

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var (isUpdateAvailable, latestVersion, downloadLink, changelog) = await UpdateService.CheckForUpdatesAsync();

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            if (isUpdateAvailable)
            {
                bool shouldUpdate = await PromptForUpdate(latestVersion, downloadLink, changelog);
                if (shouldUpdate)
                {
                    UpdateStatusLabel.IsVisible = true;
                    await CallToActionHelper.ShowPopup(CallToActionControl, $"{AppResources.Downloading}...", $"{AppResources.DownloadingPrompt}", "updateic50.png", "Primary");

                    await UpdateService.DownloadAndInstallApkAsync(downloadLink);
                }
            }
            else
            {
                await CallToActionHelper.ShowPopup(CallToActionControl, $"{AppResources.UpdateCheck}", $"XAUMobile {AppResources.UpdateCheckAlreadyUpToDate}", "successic50.png", "Primary");
            }

            await Task.Delay(TimeSpan.FromSeconds(120));
            CheckForUpdatesButton.IsEnabled = true;
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

        private void OnSignatureSwitchToggled(object? sender, ToggledEventArgs e)
        {
            SettingsService.IsSignatureEnabled = e.Value;
        }

        private void OnSignatureEntryTextChanged(object? sender, TextChangedEventArgs e)
        {
            SettingsService.UserSignature = e.NewTextValue;
        }

        private void OnResetSignatureClicked(object sender, EventArgs e)
        {
            SettingsService.UserSignature = SettingsService.DefSignature;
            SignatureEntry.Text = SettingsService.UserSignature;
        }
    }
}