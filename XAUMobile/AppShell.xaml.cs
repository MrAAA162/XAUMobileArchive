namespace XAUMobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // register routes to be used for navigation
            Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
            Routing.RegisterRoute(nameof(AchievementsPage), typeof(AchievementsPage));
            Routing.RegisterRoute(nameof(AnnouncementsPage), typeof(AnnouncementsPage));
            Routing.RegisterRoute(nameof(GamesPage), typeof(GamesPage));
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(SearchGamertagPage), typeof(SearchGamertagPage));
            Routing.RegisterRoute(nameof(SearchGamesPage), typeof(SearchGamesPage));
            Routing.RegisterRoute(nameof(SearchProductPage), typeof(SearchProductPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(SpoofingPage), typeof(SpoofingPage));
            Routing.RegisterRoute(nameof(StatsPage), typeof(StatsPage));
            Routing.RegisterRoute(nameof(UserPage), typeof(UserPage));
            Routing.RegisterRoute(nameof(WelcomePage), typeof(WelcomePage));
        }

        public void SetToolbarItemsVisible(bool isVisible)
        {
            var settingsItem = this.FindByName<ToolbarItem>("SettingsItem");
            var aboutAppItem = this.FindByName<ToolbarItem>("AboutAppItem");
            var upgradeItem = this.FindByName<ToolbarItem>("UpgradeItem");
            var announcementsItem = this.FindByName<ToolbarItem>("AnnouncementsItem");

            var toolbarItems = new[] { settingsItem, aboutAppItem, upgradeItem, announcementsItem };

            foreach (var item in toolbarItems)
            {
                if (item != null)
                {
                    if (isVisible)
                    {
                        this.ToolbarItems.Add(item);
                    }
                    else
                    {
                        this.ToolbarItems.Remove(item);
                    }
                }
            }
        }

        public void UpdateAnnouncementsIndicator()
        {
            if (!SettingsService.HasSeenAnnouncement)
            {
                AnnouncementsItem.IconImageSource = "notired.svg";
            }
            else
            {
                AnnouncementsItem.IconImageSource = "noti.svg";
            }
        }

        private async void OnAnnouncementsItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("AnnouncementsPage");
        }

        private async void OnSettingsItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("SettingsPage");
        }

        private async void OnAboutItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("AboutPage");
        }

        private async void OnStatsItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("StatsPage");
        }

        private async void OnHelpItemClicked(object sender, EventArgs e)
        {
            await Launcher.OpenAsync($"https://{Hosts.XAUDiscord}");
        }
    }
}