using Newtonsoft.Json.Linq;

namespace XAUMobile
{
    public static class UpdateService
    {
        private static readonly HttpClient _client = ApiManagerService.Instance.GetXAUApiClient();

        public static async Task<(bool IsUpdateAvailable, string LatestVersion, string DownloadLink, string Changelog)> CheckForUpdatesAsync()
        {
            try
            {
                string currentVersion = GetAppVersion()?.Trim() ?? "unknown";
                string currentLanguage = SettingsService.SelectedLanguage ?? "unknown";

                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, HeaderValues.UserAgentMeowMeow);
                _client.DefaultRequestHeaders.Add(HeaderNames.XAUVersion, currentVersion);
                _client.DefaultRequestHeaders.Add(HeaderNames.XAULanguage, currentLanguage);
                _client.DefaultRequestHeaders.Add(HeaderNames.XAU, HeaderValues.XAU);

                var response = await _client.GetStringAsync($"https://{Hosts.XAUApi}/api/status");
                Console.WriteLine("Received response from update server.");

                var statusData = JObject.Parse(response);
                var latestVersion = statusData["updates"]?["version"]?.ToString()?.Trim();
                var downloadLink = statusData["updates"]?["downloadLink"]?.ToString();
                var changelog = statusData["updates"]?["changelog"]?.ToString();

                Console.WriteLine($"Current version: {currentVersion}, Latest version: {latestVersion}");

                bool isUpdateAvailable = !string.IsNullOrEmpty(latestVersion) && latestVersion != currentVersion;

                return (isUpdateAvailable, latestVersion ?? string.Empty, downloadLink ?? string.Empty, changelog ?? string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for updates: {ex.Message}");
                return (false, string.Empty, string.Empty, string.Empty);
            }
        }

        public static async Task DownloadAndInstallApkAsync(string apkUrl)
        {
            try
            {
                SettingsService.LastUpdateCheckDate = DateTime.MinValue;
                Console.WriteLine($"Downloading APK from: {apkUrl}");
                var apkData = await _client.GetByteArrayAsync(apkUrl);
                var filePath = Path.Combine(FileSystem.AppDataDirectory, "new_xaumobile.apk");
                File.WriteAllBytes(filePath, apkData);

                Console.WriteLine($"APK downloaded and saved to: {filePath}. Initiating installation.");
                InstallApk(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading APK: {ex.Message}");
                throw;
            }
        }

        private static void InstallApk(string filePath)
        {
#if ANDROID
            var apkFile = new Java.IO.File(filePath);
            var apkUri = AndroidX.Core.Content.FileProvider.GetUriForFile(Android.App.Application.Context, "com.xaumobile.xaumobile.fileprovider", apkFile);
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            intent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
            intent.SetFlags(Android.Content.ActivityFlags.GrantReadUriPermission | Android.Content.ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
#else
            Console.WriteLine("APK installation is only supported on Android.");
#endif
        }

        public static string GetAppVersion()
        {
#if ANDROID
            try
            {
                var context = Android.App.Application.Context;
                var packageManager = context.PackageManager;
                var packageName = context.PackageName;

                if (packageManager == null || packageName == null)
                {
                    return "unknown";
                }

                var packageInfo = packageManager.GetPackageInfo(packageName, 0);
                return packageInfo?.VersionName ?? "unknown";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving app version: {ex.Message}");
                return "unknown";
            }
#else
            return "unknown";
#endif
        }
    }
}