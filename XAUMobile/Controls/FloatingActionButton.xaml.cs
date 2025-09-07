namespace XAUMobile.Controls
{
    public partial class FloatingActionButton : ContentView
    {
        private bool isExpanded = false;
        private double _offsetX;
        private double _offsetY;

        public FloatingActionButton()
        {
            InitializeComponent();

            // Load saved position (centered by default if no saved values)
            var savedX = Preferences.Get("FabX", 0.5); // Horizontal position
            var savedY = Preferences.Get("FabY", 0.5); // Vertical position
            AbsoluteLayout.SetLayoutBounds(this, new Rect(savedX, savedY, WidthRequest, HeightRequest));

            // Attach touch events to the main FAB button
            var panGestureRecognizer = new PanGestureRecognizer();
            panGestureRecognizer.PanUpdated += OnPanUpdated;
            MainFabButton.GestureRecognizers.Add(panGestureRecognizer);
        }

        private async void OnFabClicked(object sender, EventArgs e)
        {
            isExpanded = !isExpanded;

            if (isExpanded)
            {
                // Show buttons and animate in parallel
                SettingsButton.IsVisible = true;
                AboutUsButton.IsVisible = true;

                var settingsAnimation = SettingsButton.TranslateTo(0, -60, 200, Easing.CubicOut);
                var aboutUsAnimation = AboutUsButton.TranslateTo(0, -120, 200, Easing.CubicOut);

                // Run animations in parallel
                await Task.WhenAll(settingsAnimation, aboutUsAnimation);
            }
            else
            {
                // Animate hiding the buttons in parallel
                var settingsAnimation = SettingsButton.TranslateTo(0, 0, 200, Easing.CubicIn);
                var aboutUsAnimation = AboutUsButton.TranslateTo(0, 0, 200, Easing.CubicIn);

                await Task.WhenAll(settingsAnimation, aboutUsAnimation);

                // Hide buttons after animation
                SettingsButton.IsVisible = false;
                AboutUsButton.IsVisible = false;
            }
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            Console.WriteLine("Settings Button clicked!");
            // Shell.Current.GoToAsync("//SettingsPage");
        }

        private void OnAboutUsClicked(object sender, EventArgs e)
        {
            Console.WriteLine("About Us Button clicked!");
            // Shell.Current.GoToAsync("//AboutUsPage");
        }

        private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            // Handle dragging of the FloatingActionButton
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    // Store the initial position
                    _offsetX = e.TotalX;
                    _offsetY = e.TotalY;
                    break;

                case GestureStatus.Running:
                    // Update the position of the FAB
                    var layoutBounds = AbsoluteLayout.GetLayoutBounds(this);
                    double newX = layoutBounds.X + e.TotalX;
                    double newY = layoutBounds.Y + e.TotalY;

                    // Ensure the FAB stays within the screen boundaries
                    var parentWidth = (this.Parent as VisualElement)?.Width ?? Application.Current.MainPage.Width;
                    var parentHeight = (this.Parent as VisualElement)?.Height ?? Application.Current.MainPage.Height;
                    var buttonWidth = this.Width;
                    var buttonHeight = this.Height;

                    // 10% margins for the top and bottom
                    double topMargin = parentHeight * 0.1;
                    double bottomMargin = parentHeight * 0.9 - buttonHeight;

                    // Clamp the new X and Y values to keep the FAB on-screen and within 20% margin bounds
                    newX = Math.Max(0, Math.Min(newX, parentWidth - buttonWidth));
                    newY = Math.Max(topMargin, Math.Min(newY, bottomMargin));

                    // Update position
                    AbsoluteLayout.SetLayoutBounds(this, new Rect(newX, newY, layoutBounds.Width, layoutBounds.Height));
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    // Save the position when dragging is done
                    layoutBounds = AbsoluteLayout.GetLayoutBounds(this);
                    Preferences.Set("FabX", layoutBounds.X);
                    Preferences.Set("FabY", layoutBounds.Y);
                    break;
            }
        }
    }
}