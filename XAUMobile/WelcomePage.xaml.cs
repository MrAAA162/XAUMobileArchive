namespace XAUMobile
{
    public partial class WelcomePage : ContentPage
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        private void OnAcceptButtonClicked(object sender, EventArgs e)
        {
            if (Application.Current != null)
            {
                // Set the MainPage to AppShell after the user accepts the terms
                Application.Current.MainPage = new AppShell();
            }
        }
    }
}