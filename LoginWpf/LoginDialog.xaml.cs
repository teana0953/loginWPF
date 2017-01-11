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

namespace LoginWpf
{
    public enum LogIn
    {
        fb,
        google
    }
    /// <summary>
    /// LoginDialog.xaml 的互動邏輯
    /// </summary>
    public partial class LoginDialog : Window
    {
        private const string appId = "1835738026681907";    // from https://developers.facebook.com/apps/
        /// <summary>
        /// Extended permissions is a comma separated list of permissions to ask the user.
        /// </summary>
        /// <remarks>
        /// For extensive list of available extended permissions refer to 
        /// https://developers.facebook.com/docs/reference/api/permissions/
        /// </remarks>
        private const string extendedPermissions = "public_profile,user_birthday";
        private readonly Uri loginUrl;
        public FacebookOAuthResult FacebookOAuthResult { get; private set; }
        protected FacebookClient fb = new FacebookClient();
        public LoginDialog(LogIn way)
        {
            switch (way)
            {
                case LogIn.fb:
                    loginUrl = GenerateLoginUrl(appId,extendedPermissions);
                    break;
                case LogIn.google:
                    break;
            }
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loginBrowser.Navigate(loginUrl.AbsoluteUri);
        }

        private Uri GenerateLoginUrl(string appId, string extendedPermissions)
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

        private void loginBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            // whenever the browser navigates to a new url, try parsing the url.
            // the url may be the result of OAuth 2.0 authentication.

            FacebookOAuthResult oauthResult;
            if (fb.TryParseOAuthCallbackUrl(e.Uri, out oauthResult))
            {
                // The url is the result of OAuth 2.0 authentication
                FacebookOAuthResult = oauthResult;
                DialogResult = FacebookOAuthResult.IsSuccess;
            }
            else
            {
                // The url is NOT the result of OAuth 2.0 authentication.
                FacebookOAuthResult = null;
            }
        }
    }
}
