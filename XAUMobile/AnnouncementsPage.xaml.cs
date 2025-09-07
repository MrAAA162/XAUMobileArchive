using Newtonsoft.Json.Linq;

namespace XAUMobile
{
    public partial class AnnouncementsPage : ContentPage
    {
        private readonly AnnouncementsService _announcementsService;

        public AnnouncementsPage()
        {
            InitializeComponent();
            _announcementsService = new AnnouncementsService();
            LoadAnnouncements();
        }

        private async void LoadAnnouncements()
        {
            try
            {
                JObject? announcementsData = await _announcementsService.GetCachedAnnouncementsAsync();

                if (announcementsData != null)
                {
                    DisplayAnnouncements(announcementsData);
                }
                else
                {
                    announcementsData = await _announcementsService.FetchAndSaveAnnouncementsFromApiAsync();

                    if (announcementsData != null)
                    {
                        DisplayAnnouncements(announcementsData);
                    }
                    else
                    {
                        await DisplayAlert("Info", "No announcements available.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private void DisplayAnnouncements(JObject data)
        {
            var latest = data["announcements"]?["latest"];
            if (latest != null)
            {
                LatestTitleLabel.Text = latest["Title"]?.ToString() ?? "No Title";
                LatestBodyLabel.Text = latest["Body"]?.ToString() ?? "No Content";
            }
            else
            {
                LatestTitleLabel.Text = "No Latest Announcement";
                LatestBodyLabel.Text = "No Content Available";
            }

            var previous = data["announcements"]?["previous"];
            if (previous != null)
            {
                PreviousAnnouncementsStack.Children.Clear();

                foreach (var announcement in previous)
                {
                    var cardFrame = new Frame
                    {
                        BackgroundColor = Colors.Transparent,
                        BorderColor = (Color)Application.Current.Resources["Primary"],
                        CornerRadius = 0,
                        HasShadow = true,
                        Padding = new Thickness(10),
                        Margin = new Thickness(0, 5)
                    };

                    var cardContent = new VerticalStackLayout
                    {
                        Spacing = 5
                    };

                    var titleLabel = new Label
                    {
                        Text = announcement["Title"]?.ToString() ?? "No Title",
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = (Color)Application.Current.Resources["Primary"],
                        HorizontalOptions = LayoutOptions.Center
                    };

                    var bodyLabel = new Label
                    {
                        Text = announcement["Body"]?.ToString() ?? "No Content",
                        FontSize = 12,
                        TextColor = (Color)Application.Current.Resources["Secondary"]
                    };

                    cardContent.Children.Add(titleLabel);
                    cardContent.Children.Add(bodyLabel);
                    cardFrame.Content = cardContent;

                    PreviousAnnouncementsStack.Children.Add(cardFrame);
                }
            }
            else
            {
                DisplayAlert("Info", "No previous announcements available.", "OK");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!SettingsService.HasSeenAnnouncement)
            {
                SettingsService.HasSeenAnnouncement = true;

                if (Application.Current.MainPage is AppShell appShell)
                {
                    appShell.UpdateAnnouncementsIndicator();
                }
            }

            ((AppShell)Shell.Current).SetToolbarItemsVisible(false);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ((AppShell)Shell.Current).SetToolbarItemsVisible(true);
        }
    }
}