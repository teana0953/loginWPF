using Facebook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LoginWpf
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public Patient patient;
        public Setting setting;
        private LogIn currentLoginWay;
        private const string googleAk = "googleAk";
        private string googleAccessToken = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
            patient = new Patient();
            setting = Setting.LoadSettingConfig("setting.config") ?? new Setting();
            DataContext = patient;
            switch (setting.logInWay)
            {
                case LogIn.fb:
                    GetProfileFromFb(setting.FbAccessToken);
                    break;
                case LogIn.google:
                    GetGoogleProfileByPreviousAccessToken();
                    break;
            }
        }

        private void btn_fbLogin_Click(object sender, RoutedEventArgs e)
        {
            ClearProfile();
            currentLoginWay = LogIn.fb;
            // load previous accesstoken
            if (!string.IsNullOrWhiteSpace(patient.Id))
                return;

            LoginDialog logInDialog = new LoginDialog(currentLoginWay);
            logInDialog.ShowDialog();
            // Take log in action
            if (logInDialog.FacebookOAuthResult == null)
            {
                // the user closed the FacebookLoginDialog, so do nothing.
                MessageBox.Show("Cancelled!");
                return;
            }
            // Even though facebookOAuthResult is not null, it could had been an 
            // OAuth 2.0 error, so make sure to check IsSuccess property always.
            if (logInDialog.FacebookOAuthResult.IsSuccess)
            {
                // since our respone_type in FacebookLoginDialog was token,
                // we got the access_token
                // The user now has successfully granted permission to our app.
                setting.FbAccessToken = logInDialog.FacebookOAuthResult.AccessToken;
                GetProfileFromFb(logInDialog.FacebookOAuthResult.AccessToken);
            }
            else
            {
                MessageBox.Show(logInDialog.FacebookOAuthResult.ErrorDescription);
            }
        }


        private async void GetProfileFromFb(string accessKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accessKey))
                    return;
                var fb = new FacebookClient(accessKey);

                // load other profile info
                fb.GetCompleted += (o, e) =>
                {
                    // incase you support cancellation, make sure to check
                    // e.Cancelled property first even before checking (e.Error != null).
                    if (e.Cancelled)
                    {
                        // for this example, we can ignore as we don't allow this
                        // example to be cancelled.
                    }
                    else if (e.Error != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(e.Error.Message);
                            setting.FbAccessToken = string.Empty;       // accesstoken init
                        });
                    }
                    else
                    {
                        try
                        {
                            var result = (IDictionary<string, object>)e.GetResultData();
                            patient.Id = (string)result["id"];
                            patient.Name = (string)result["name"];
                            patient.Gender = (string)result["gender"];
                            patient.Dob = (string)result["birthday"];
                            patient.ImagePath = string.Format("https://graph.facebook.com/{0}/picture?type={1}", patient.Id, "square");

                            // location
                            var locationDict = (IDictionary<string, object>)result["location"];
                            patient.Country = (string)locationDict["name"];
                        }
                        catch (Exception ee)
                        {
                            MessageBox.Show(ee.Message);
                        }

                    }
                };

                var parameters = new Dictionary<string, object>();
                parameters["fields"] = "id,name,first_name,last_name,gender,birthday,location";

                await fb.GetTaskAsync("me", parameters);
            }
            catch (FacebookApiException ex)
            {
                //MessageBox.Show(ex.Message);
            }
        }
        private void ClearProfile()
        {
            patient = new Patient();
            DataContext = patient;
        }

        private async Task<bool> GetGoogleProfileByPreviousAccessToken()
        {
            try
            {
                using (StreamReader file = new StreamReader(googleAk))
                {

                    string text = file.ReadToEnd();
                    // converts to dictionary
                    Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
                    googleAccessToken = tokenEndpointDecoded["access_token"];
                    await userinfoCall(googleAccessToken);
                    return true;
                }
            }
            catch (Exception ee)
            {
                return false;
            }
        }

        private async void btn_goolgeLogin_Click(object sender, RoutedEventArgs e)
        {
            ClearProfile();
            currentLoginWay = LogIn.google;
            if (await GetGoogleProfileByPreviousAccessToken())      // 先前已有 access token
                return;

            LoginDialog logInDialog = new LoginDialog(currentLoginWay);
            logInDialog.ShowDialog();
            if (logInDialog.Code == null)
                return;
            await performCodeExchange(logInDialog.Code, logInDialog.Code_verifier, logInDialog.RedirectURI);
        }
        async Task performCodeExchange(string code, string code_verifier, string redirectURI)
        {
            MessageBox.Show("Exchanging code for tokens...");

            await Task.Run(async () =>
            {
                // builds the  request
                string tokenRequestURI = "https://www.googleapis.com/oauth2/v4/token";
                string tokenRequestBody = string.Format("code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&client_secret={4}&scope=&grant_type=authorization_code",
                    code,
                    System.Uri.EscapeDataString(redirectURI),
                    LoginDialog.clientID,
                    code_verifier,
                    LoginDialog.clientSecret
                    );

                // sends the request
                HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(tokenRequestURI);
                tokenRequest.Method = "POST";
                tokenRequest.ContentType = "application/x-www-form-urlencoded";
                tokenRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                byte[] _byteVersion = Encoding.ASCII.GetBytes(tokenRequestBody);
                tokenRequest.ContentLength = _byteVersion.Length;
                Stream stream = tokenRequest.GetRequestStream();
                await stream.WriteAsync(_byteVersion, 0, _byteVersion.Length);
                stream.Close();

                try
                {
                    // gets the response
                    WebResponse tokenResponse = await tokenRequest.GetResponseAsync();
                    using (StreamWriter file = new StreamWriter(googleAk))
                    using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                    {
                        // reads response body
                        string responseText = await reader.ReadToEndAsync();
                        MessageBox.Show(responseText);
                        file.Write(responseText);

                        // converts to dictionary
                        Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);
                        googleAccessToken = tokenEndpointDecoded["access_token"];
                        await userinfoCall(googleAccessToken);
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        var response = ex.Response as HttpWebResponse;
                        if (response != null)
                        {
                            MessageBox.Show("HTTP: " + response.StatusCode);
                            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                            {
                                // reads response body
                                string responseText = await reader.ReadToEndAsync();
                                MessageBox.Show(responseText);
                            }
                        }

                    }
                }
            });

        }
        async Task userinfoCall(string access_token)
        {
            MessageBox.Show("Making API Call to Userinfo...");

            // builds the  request
            //string userinfoRequestURI = "https://www.googleapis.com/oauth2/v3/userinfo";
            string userinfoRequestURI = "https://www.googleapis.com/plus/v1/people/me";

            // sends the request
            HttpWebRequest userinfoRequest = (HttpWebRequest)WebRequest.Create(userinfoRequestURI);
            userinfoRequest.Method = "GET";
            userinfoRequest.Headers.Add(string.Format("Authorization: Bearer {0}", access_token));
            userinfoRequest.ContentType = "application/x-www-form-urlencoded";
            userinfoRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

            // gets the response
            WebResponse userinfoResponse = await userinfoRequest.GetResponseAsync();
            using (StreamReader userinfoResponseReader = new StreamReader(userinfoResponse.GetResponseStream()))
            {
                // reads response body
                string userinfoResponseText = await userinfoResponseReader.ReadToEndAsync();

                try
                {
                    // converts to dictionary
                    Dictionary<string, object> userInfoDecoded = JsonConvert.DeserializeObject<Dictionary<string, object>>(userinfoResponseText);
                    object country, dob, gender, id, name;
                    userInfoDecoded.TryGetValue("language", out country);
                    userInfoDecoded.TryGetValue("birthday", out dob);
                    userInfoDecoded.TryGetValue("gender", out gender);
                    userInfoDecoded.TryGetValue("id", out id);
                    userInfoDecoded.TryGetValue("displayName", out name);

                    patient.Country = Convert.ToString(country) ?? string.Empty;
                    patient.Dob = Convert.ToString(dob)??string.Empty;
                    patient.Gender = Convert.ToString(gender) ?? string.Empty;
                    patient.Id = Convert.ToString(id) ?? string.Empty;
                    patient.Name = Convert.ToString(name)??string.Empty;
                    Dictionary<string, object> imageDecoded = JsonConvert.DeserializeObject<Dictionary<string, object>>(Convert.ToString(userInfoDecoded["image"]));
                    patient.ImagePath = Convert.ToString(imageDecoded["url"]);
                    
                }
                catch (Exception ee)
                {
                }

                MessageBox.Show(userinfoResponseText);
            }
        }

        private void btn_logout_Click(object sender, RoutedEventArgs e)
        {
            switch (currentLoginWay)
            {
                case LogIn.fb:
                    logoutFb();
                    break;
                case LogIn.google:
                    logoutGoogle();
                    break;
            }
        }

        private void logoutFb()
        {
            var fb = new FacebookClient();
            var logoutUrl = fb.GetLogoutUrl(new
            {
                next = "https://www.facebook.com/connect/login_success.html",
                access_token = setting.FbAccessToken
            });
            var webBrowser = new WebBrowser();
            webBrowser.Navigated += (o, args) =>
            {
                if (args.Uri.AbsoluteUri == "https://www.facebook.com/connect/login_success.html")
                {
                    ClearProfile();
                    setting.FbAccessToken = null;
                }
                //Close();
            };
            webBrowser.Navigate(logoutUrl.AbsoluteUri);
        }

        private void logoutGoogle()
        {
            try
            {
                if (File.Exists(googleAk))
                    File.Delete(googleAk);
                ClearProfile();
            }
            catch (Exception)
            {

            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            setting.logInWay = currentLoginWay;
            Setting.SaveSettingConfig("setting", setting);
        }
    }
}
