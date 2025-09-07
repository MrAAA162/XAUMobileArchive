namespace XAUMobile
{
    public static class ThemeService
    {
        public static List<ColorOption> GetColorOptions()
        {
            return new List<ColorOption>
            {
                new ColorOption { Name = "Default", HexValue = "#5D9632" },
                new ColorOption { Name = "Purple", HexValue = "#6200EE" },
                new ColorOption { Name = "Indigo", HexValue = "#4B0082" },
                new ColorOption { Name = "Teal", HexValue = "#008080" },
                new ColorOption { Name = "Dark Red", HexValue = "#8B0000" },
                new ColorOption { Name = "Red", HexValue = "#FF0000" },
                new ColorOption { Name = "Pink", HexValue = "#D80D76" },
                new ColorOption { Name = "Dark Blue", HexValue = "#003366" },
                new ColorOption { Name = "Blue", HexValue = "#0000FF" }
            };
        }

        public static void ApplyPrimaryColor(string hexColor)
        {
            if (Application.Current?.Resources != null)
            {
                // Apply the primary color
                if (Application.Current.Resources.ContainsKey("Primary"))
                {
                    Application.Current.Resources["Primary"] = Color.FromArgb(hexColor);
                }
                else
                {
                    Application.Current.Resources.Add("Primary", Color.FromArgb(hexColor));
                }
            }

            RefreshUI();
        }

        public static void ApplyTheme(bool isNightMode)
        {
            if (Application.Current?.Resources != null)
            {
                // Update PageBg color based on light or dark mode
                var pageBgColor = isNightMode ? "#000000" : "#1f1f1f"; // black for night mode mode, off-black for dark mode
                var secondaryColor = isNightMode ? "#DFD8F7" : "#DFD8F7"; // Black for light mode, custom color for dark mode
                var cardBgColor = isNightMode ? "#151515" : "#282828"; // cards bg for games, achievements, and more

                if (Application.Current.Resources.ContainsKey("PageBg"))
                {
                    Application.Current.Resources["PageBg"] = Color.FromArgb(pageBgColor);
                }
                else
                {
                    Application.Current.Resources.Add("PageBg", Color.FromArgb(pageBgColor));
                }

                if (Application.Current.Resources.ContainsKey("Secondary"))
                {
                    Application.Current.Resources["Secondary"] = Color.FromArgb(secondaryColor);
                }
                else
                {
                    Application.Current.Resources.Add("Secondary", Color.FromArgb(secondaryColor));
                }

                if (Application.Current.Resources.ContainsKey("CardBg"))
                {
                    Application.Current.Resources["CardBg"] = Color.FromArgb(cardBgColor);
                }
                else
                {
                    Application.Current.Resources.Add("CardBg", Color.FromArgb(cardBgColor));
                }
            }

            RefreshUI();
        }

        public static void ApplySavedTheme()
        {
            string savedColor = SettingsService.PrimaryColor;
            ApplyPrimaryColor(savedColor);

            bool isNightMode = SettingsService.IsNightMode;
            ApplyTheme(isNightMode);
        }

        public static ColorOption? GetSavedColorOption()
        {
            var savedColorHex = SettingsService.PrimaryColor;
            return GetColorOptions().FirstOrDefault(c => c.HexValue == savedColorHex);
        }

        public static void HandlePrimaryColorSelection(object selectedItem)
        {
            var selectedColor = selectedItem as ColorOption;
            if (selectedColor != null)
            {
                SettingsService.PrimaryColor = selectedColor.HexValue;

                ApplyPrimaryColor(selectedColor.HexValue);
            }
        }

        public static string GetActiveThemeName()
        {
            string savedColorHex = SettingsService.PrimaryColor;

            if (savedColorHex == SettingsService.DefaultPrimaryColor)
            {
                return "Default";
            }

            var savedColorOption = GetColorOptions().FirstOrDefault(c => c.HexValue == savedColorHex);

            return savedColorOption != null ? savedColorOption.Name : "Unknown";
        }

        private static void RefreshUI()
        {
            // Refresh all active windows
            if (Application.Current?.Windows != null)
            {
                foreach (var window in Application.Current.Windows)
                {
                    var mainPage = window.Page;
                    if (mainPage != null)
                    {
                        // Reapply the main page to force the UI to refresh
                        mainPage.Dispatcher.Dispatch(() =>
                        {
                            window.Page = mainPage;
                        });
                    }
                }
            }
        }

        public class ColorOption
        {
            public string Name { get; set; }
            public string HexValue { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}