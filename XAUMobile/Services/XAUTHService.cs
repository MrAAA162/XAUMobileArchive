namespace XAUMobile
{
    public static class XAUTHService
    {
        private const string XAuthTokenKey = "XAuthToken";

        public static string AuthToken
        {
            get => Preferences.Get(XAuthTokenKey, string.Empty);
            set => Preferences.Set(XAuthTokenKey, value);
        }
    }
}

// to do: figure out xauth login with spoofing support. Add more options for Azure read-only auth, and integrate XAU PC developer API xauth import