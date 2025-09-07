namespace XAUMobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            ThemeService.ApplySavedTheme();

            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = AppTheme.Dark; //force dark mode - disable light mode, dont follow system theme
            }

            var savedLanguage = SettingsService.SelectedLanguage;

            if (SettingsPage.SupportedLanguages.TryGetValue(savedLanguage, out var culture))
            {
                LocalizationResourceService.Instance.SetCulture(culture);
            }

            MainPage = new WelcomePage();
        }
    }
}