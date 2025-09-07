using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using XAUMobile.Controls;
using XAUMobile.Resources.Languages;

namespace XAUMobile
{
    public partial class SearchProductPage : ContentPage
    {
        private readonly HttpClient _client = ApiManagerService.Instance.GetXboxApiClient();

        public SearchProductPage()
        {
            InitializeComponent();
        }

        private async void OnGetTitleIdClicked(object sender, EventArgs e)
        {
            ProductSearchButton.IsEnabled = false;

            string input = linkEntry.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.SearchProductEnterValidProductId}.", "erroric50.png", "RedError");

                ProductSearchButton.IsEnabled = true;
                return;
            }

            string productId = ExtractProductId(input);
            if (string.IsNullOrWhiteSpace(productId))
            {
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.SearchProductEnterValidProductId}.", "erroric50.png", "RedError");

                ProductSearchButton.IsEnabled = true;
                return;
            }

            var titleIdItems = await GetXboxTitleIdsAsync(productId);
            if (titleIdItems == null || !titleIdItems.Any())
            {
                await CallToActionHelper.ShowMessage(CallToActionControl, $"{AppResources.Error}", $"{AppResources.SearchProductError}.", "erroric50.png", "RedError");

                ProductSearchButton.IsEnabled = true;
                return;
            }

            // Debug log for checking the data
            foreach (var item in titleIdItems)
            {
                Console.WriteLine($"Title: {item.Name}, XboxTitleId: {item.XboxTitleId}");
            }



            titleIdListView.ItemsSource = titleIdItems;
            titleIdListView.IsVisible = true;
            CopyInstructionLabel.IsVisible = true;

            await Task.Delay(4000);
            ProductSearchButton.IsEnabled = true;
        }

        private string ExtractProductId(string input)
        {
            if (Regex.IsMatch(input, @"^[a-zA-Z0-9]{10,}$"))
            {
                return input;
            }

            var regex = new Regex(@"\/([a-zA-Z0-9]{10,})(\/|$)");
            var match = regex.Match(input);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private async Task<List<TitleIdItem>> GetXboxTitleIdsAsync(string productId)
        {
            var url = $"https://{Hosts.GamepassCatalog}/products?market=US&language=en-US&hydration=PCHome";
            var jsonContent = JsonSerializer.Serialize(new { Products = new[] { productId } });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Clear();

            var response = await _client.PostAsync(url, content);
            Console.WriteLine($"Response status code: {response.StatusCode}");

            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"API call failed. Status: {response.StatusCode}, Response: {responseJson}");
                return new List<TitleIdItem>();
            }

            Console.WriteLine("Response received");
            Console.WriteLine($"JSON Response: {responseJson}");

            try
            {
                var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("Products", out var products))
                {
                    var titleIdItems = new List<TitleIdItem>();
                    var productObjects = products.EnumerateObject().ToList();

                    // Check if the first product array has ChildXboxTitleIds
                    if (productObjects.Count > 0)
                    {
                        var firstProductValue = productObjects[0].Value;

                        if (firstProductValue.TryGetProperty("ChildXboxTitleIds", out var childTitleIds) &&
                            childTitleIds.ValueKind == JsonValueKind.Array)
                        {
                            // Skip the first product array
                            productObjects = productObjects.Skip(1).ToList();
                        }
                    }

                    foreach (var product in productObjects)
                    {
                        var productValue = product.Value;

                        var productTitle = productValue.GetProperty("ProductTitle").GetString();

                        if (productValue.TryGetProperty("ChildXboxTitleIds", out var childTitleIds) &&
                            childTitleIds.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var titleId in childTitleIds.EnumerateArray())
                            {
                                titleIdItems.Add(new TitleIdItem
                                {
                                    Name = productTitle,
                                    XboxTitleId = titleId.GetString()
                                });
                            }
                        }
                        else if (productValue.TryGetProperty("XboxTitleId", out var singleTitleId) && singleTitleId.ValueKind == JsonValueKind.String)
                        {
                            titleIdItems.Add(new TitleIdItem
                            {
                                Name = productTitle,
                                XboxTitleId = singleTitleId.GetString()
                            });
                        }
                    }

                    Console.WriteLine($"Title IDs: {titleIdItems.Count} found");
                    return titleIdItems;
                }

                Console.WriteLine("Failed to parse JSON response");
                return new List<TitleIdItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return new List<TitleIdItem>();
            }
        }



        private async void OnTitleTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is TitleIdItem item)
            {
                await Clipboard.SetTextAsync(item.XboxTitleId);
                await CallToActionHelper.ShowPopup(CallToActionControl, $"{AppResources.Copied}", $"{AppResources.SearchTabGame}: {item.Name}\nXbox {AppResources.TitleID}: {item.XboxTitleId} {AppResources.CopiedToClipboardMessage}.", "successic50.png", "Primary");
            }
        }
    }

    public class TitleIdItem
    {
        public string? Name { get; set; }
        public string? XboxTitleId { get; set; }
    }
}