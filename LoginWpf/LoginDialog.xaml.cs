using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Facebook;
using System.Dynamic;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace LoginWpf
{
    public enum LogIn
    {
        none,
        fb,
        google
    }
    /// <summary>
    /// LoginDialog.xaml 的互動邏輯
    /// </summary>
    public partial class LoginDialog : Window
    {
        #region FB-related
        private const string fbAppId = "1835738026681907";    // from https://developers.facebook.com/apps/
        /// <summary>
        /// Extended permissions is a comma separated list of permissions to ask the user.
        /// </summary>
        /// <remarks>
        /// For extensive list of available extended permissions refer to 
        /// https://developers.facebook.com/docs/reference/api/permissions/
        /// </remarks>
        private const string extendedPermissions = "public_profile,user_birthday,user_location";
        private readonly Uri loginUrl;
        public FacebookOAuthResult FacebookOAuthResult { get; private set; }
        protected FacebookClient fb = new FacebookClient();
        #endregion

        #region Google
        // client configuration
        public const string clientID = "62727377895-josmdsg2q4utsj30pukg80dep8n4imul.apps.googleusercontent.com";
        public const string clientSecret = "tuyyeeOmRYOxDoQo9v-h1ygQ";
        const string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        const string tokenEndpoint = "https://www.googleapis.com/oauth2/v4/token";
        const string userInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";
        #endregion

        private LogIn loginWay;
        private HttpListener http = new HttpListener();
        public string State { get; private set; }
        public string Code_verifier { get; private set; }
        public string Code_challenge { get; private set; }
        public string Code { get; private set; }
        public string RedirectURI { get; private set; }
        public LoginDialog(LogIn way)
        {
            switch (way)
            {
                case LogIn.fb:
                    loginWay = LogIn.fb;
                    loginUrl = GenerateFbLoginUrl(fbAppId,extendedPermissions);
                    break;
                case LogIn.google:
                    loginWay = LogIn.google;
                    // Creates a redirect URI using an available port on the loopback address.
                    RedirectURI = string.Format("http://{0}:{1}/", "localhost", GetRandomUnusedPort());
                    loginUrl = GenerateGoogleLoginUrl();
                    break;
            }
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loginBrowser.Navigate(loginUrl.AbsoluteUri);
        }

        private Uri GenerateFbLoginUrl(string appId, string extendedPermissions)
        {
            dynamic parameters = new ExpandoObject();
            parameters.client_id = appId;
            parameters.redirect_uri = "https://www.facebook.com/connect/login_success.html";

            // The requested response: an access token (token), an authorization code (code), or both (code token).
            parameters.response_type = "token";

            // list of additional display modes can be found at http://developers.facebook.com/docs/reference/dialogs/#display
            parameters.display = "popup";

            // add the 'scope' parameter only if we have extendedPermissions.
            if (!string.IsNullOrWhiteSpace(extendedPermissions))
                parameters.scope = extendedPermissions;

            // when the Form is loaded navigate to the login url.
            return fb.GetLoginUrl(parameters);
        }

        private const string scope = "https://www.googleapis.com/auth/plus.login";
        private Uri GenerateGoogleLoginUrl()
        {
            // Generates state and PKCE values.
            State = randomDataBase64url(32);
            Code_verifier = randomDataBase64url(32);
            Code_challenge = base64urlencodeNoPadding(sha256(Code_verifier));
            const string code_challenge_method = "S256";
            // Creates the OAuth 2.0 authorization request.
            string authorizationRequest = string.Format("{0}?response_type=code&scope=openid%20{1}&redirect_uri={2}&client_id={3}&state={4}&code_challenge={5}&code_challenge_method={6}",
                authorizationEndpoint,
                scope,
                System.Uri.EscapeDataString(RedirectURI),
                clientID,
                State,
                Code_challenge,
                code_challenge_method);
            return new Uri(authorizationRequest);
        }
        private string randomDataBase64url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return base64urlencodeNoPadding(bytes);
        }
        private string base64urlencodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }
        private byte[] sha256(string inputStirng)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(inputStirng);
            SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(bytes);
        }
        // ref http://stackoverflow.com/a/3978040
        public static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private async void loginBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            // whenever the browser navigates to a new url, try parsing the url.
            // the url may be the result of OAuth 2.0 authentication.
            switch (loginWay)
            {
                case LogIn.fb:
                    FacebookOAuthResult oauthResult;
                    if (fb.TryParseOAuthCallbackUrl(e.Uri, out oauthResult))
                    {
                        // The url is the result of OAuth 2.0 authentication
                        FacebookOAuthResult = oauthResult;
                        DialogResult = FacebookOAuthResult.IsSuccess;
                    }
                    else
                        FacebookOAuthResult = null;
                    break;
                case LogIn.google:
                    if (e.Uri.AbsolutePath == "/o/oauth2/v2/auth" && !http.IsListening)        // 並非在登入的頁面才會開起 listening
                    {
                        http.Prefixes.Add(RedirectURI);
                        http.Start();
                    }
                    if (!http.IsListening)
                        return;
                    try
                    {
                        var context = await http.GetContextAsync();
                        // Sends an HTTP response to the browser.
                        var response = context.Response;
                        string responseString = string.Format("<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Please return to the app.</body></html>");
                        var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = buffer.Length;
                        var responseOutput = response.OutputStream;
                        Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
                        {
                            responseOutput.Close();
                            http.Stop();
                            Console.WriteLine("HTTP server stopped.");
                        });

                        // Checks for errors.
                        if (context.Request.QueryString.Get("error") != null)
                        {
                            this.Close();
                            MessageBox.Show(String.Format("OAuth authorization error: {0}.", context.Request.QueryString.Get("error")));
                            return;
                        }
                        if (context.Request.QueryString.Get("code") == null
                            || context.Request.QueryString.Get("state") == null)
                        {
                            MessageBox.Show("Malformed authorization response. " + context.Request.QueryString);
                            return;
                        }

                        // extracts the code
                        Code = context.Request.QueryString.Get("code");
                        var incoming_state = context.Request.QueryString.Get("state");
                        loginWay = LogIn.none;
                        this.Close();
                    }
                    catch (Exception ee)
                    {

                    }
                    
                    
                    break;
            }
            
        }
    }
}
