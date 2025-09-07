namespace XAUMobile
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
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

        private async void OnDiscordTapped(object sender, EventArgs e)
        {
            await Launcher.OpenAsync($"https://{Hosts.XAUDiscord}");
        }

        private async void OnGithubTapped(object sender, EventArgs e)
        {
            await Launcher.OpenAsync($"https://{Hosts.XAUMobileGithubRelease}");
        }
    }
}