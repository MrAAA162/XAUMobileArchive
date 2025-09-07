namespace XAUMobile
{
    public static class SettingsService
    {
        // TO DO: MOVE SETTINGS TO SQLITE SERVICE INSTEAD OF USING PREFERENCES. THIS IS MUCH BETTER FOR FUTURE PROOFING
        // SQLITE CAN HANDLE LARGER DATA. PREFERENCES CANT!
        public static string DefSignature => DefaultSignature;

        private const string DefaultSignature = "RGFtbklHb3R0YU1ha2VUaGlzU3RyaW5nU3VwZXJMb25nSHVoLkRvbnRFdmVuS25vd1doYXRTaG91bGRCZUhlcmVEcmFmZlN0cmluZw==";
        private const string SignatureKey = "UserSignature";
        private const string SignatureEnabledKey = "SignatureEnabled";
        private const string PrivacyEnabledKey = "PrivacyEnabled";

        public const string PrimaryColorKey = "PrimaryColor";
        public const string DefaultPrimaryColor = "#5D9632";
        private const string NightModeKey = "NightMode";

        private const string LastUpdateCheckDateKey = "LastUpdateCheckDate";
        private const string AnnouncementIdKey = "AnnouncementId";
        private const string HasSeenAnnouncementKey = "HasSeenAnnouncement";

        private const string LanguageKey = "SelectedLanguage";
        private const string DefaultLanguage = "en-US";

        public static string SelectedLanguage
        {
            get => Preferences.Get(LanguageKey, DefaultLanguage);
            set => Preferences.Set(LanguageKey, value);
        }

        public static string PrimaryColor
        {
            get => Preferences.Get(PrimaryColorKey, DefaultPrimaryColor);
            set => Preferences.Set(PrimaryColorKey, value);
        }

        public static bool IsNightMode
        {
            get => Preferences.Get(NightModeKey, false);
            set => Preferences.Set(NightModeKey, value);
        }

        public static bool IsSignatureEnabled
        {
            get => Preferences.Get(SignatureEnabledKey, true);
            set => Preferences.Set(SignatureEnabledKey, value);
        }

        public static string UserSignature
        {
            get => Preferences.Get(SignatureKey, DefaultSignature);
            set => Preferences.Set(SignatureKey, value);
        }

        public static string GetCurrentSignature()
        {
            return IsSignatureEnabled ? UserSignature : DefaultSignature;
        }

        public static bool IsPrivacyEnabled
        {
            get => Preferences.Get(PrivacyEnabledKey, false);
            set => Preferences.Set(PrivacyEnabledKey, value);
        }


        public static DateTime LastUpdateCheckDate
        {
            get
            {
                string storedDate = Preferences.Get(LastUpdateCheckDateKey, string.Empty);
                return string.IsNullOrEmpty(storedDate) ? DateTime.MinValue : DateTime.Parse(storedDate);
            }
            set
            {
                Preferences.Set(LastUpdateCheckDateKey, value.ToString());
            }
        }

        public static string AnnouncementId
        {
            get => Preferences.Get(AnnouncementIdKey, string.Empty);
            set => Preferences.Set(AnnouncementIdKey, value);
        }

        public static bool HasSeenAnnouncement
        {
            get => Preferences.Get(HasSeenAnnouncementKey, true);
            set => Preferences.Set(HasSeenAnnouncementKey, value);
        }
    }
}