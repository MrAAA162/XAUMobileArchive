using System.Diagnostics;
using System.Text.RegularExpressions;
using XAUMobile.Resources.Languages;

namespace XAUMobile
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private void InitializeWebView()
        {
            webView.Source = "https://www.xbox.com/en-US/auth/msa?action=logIn&returnUrl=https%3A%2F%2Fwww.xbox.com%2Fen-US%2Fplay%2Fuser";
        }

        private void OnInjectScriptClicked(object sender, EventArgs e)
        {
            // Inject script on button press
            _ = InjectJavaScriptAsync();
        }

        private async void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
        {
            var regex = new Regex(@"xbox\.com/[^/]+/play/");
            if (regex.IsMatch(e.Url))
            {
                // Auto inject script on navigation (lil buggy so i added a button for now)
                await InjectJavaScriptAsync();
            }
        }

        private async Task InjectJavaScriptAsync()
        // credits to stelemanuele77
        {
            var script = @"
            (function() {
                try {
                    console.log('Injecting script...');
                    var tokenData = (c=>(cookies=>{
                        console.log('Parsing cookies...');
                        for(var i=0;i<cookies.length;i++){
                            var cookie=cookies[i].trim();
                            console.log('Checking cookie: ' + cookie);
                            if(cookie.indexOf(c)===0){
                                var cookieValue=decodeURIComponent(cookie.substring(c.length+1));
                                console.log('Cookie value: ' + cookieValue);
                                try{
                                    var jsonValue=JSON.parse(cookieValue);
                                    if(jsonValue.identityType==='XToken'&&jsonValue.tokenData)
                                        return jsonValue.tokenData
                                } catch(e){ console.error('Error parsing cookie value', e); return '' }
                            }
                        }
                        return ''
                    })(document.cookie.split(';')))('XBXXtkhttp%3A%2F%2Fxboxlive.com');
                    if (tokenData && tokenData.token && tokenData.userHash) {
                        var tokenString = 'XBL3.0 x=' + tokenData.userHash + ';' + tokenData.token;
                        console.log('Token found:', tokenString);
                        return tokenString;
                    } else {
                        console.log('Token not found or cookie issue');
                        return '';
                    }
                } catch (e) {
                    console.error('Script injection error', e);
                    return '';
                }
            })();";

            try
            {
                var result = await webView.EvaluateJavaScriptAsync(script);
                Debug.WriteLine("JavaScript execution result: " + result);

                if (!string.IsNullOrEmpty(result))
                {
                    XAUTHService.AuthToken = result;
                    Debug.WriteLine("AuthToken saved successfully: " + result);
                    await DisplayAlert($"{AppResources.Success}", $"{AppResources.LoginPageSuccessMessage}", $"{AppResources.OK}");
                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    Debug.WriteLine("Failed to retrieve AuthToken.");
                    await DisplayAlert($"{AppResources.Error}", $"{AppResources.LoginPageFailMessage}", $"{AppResources.OK}");

                    //await Shell.Current.GoToAsync("//MainPage");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("JavaScript injection failed: " + ex.Message);
                await DisplayAlert("Error", "Javascript injection failed. Try again. Contact Discord support for assistance.", "OK");
                await Shell.Current.GoToAsync("//MainPage");
            }
        }
    }
}