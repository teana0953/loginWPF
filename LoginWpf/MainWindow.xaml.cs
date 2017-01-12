using Facebook;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public MainWindow()
        {
            InitializeComponent();
            patient = new Patient();
            setting = Setting.LoadSettingConfig("setting.config")??new Setting();
            DataContext = patient;
            switch (setting.logInWay)
            {
                case LogIn.fb:
                    GetProfileFromFb(setting.FbAccessToken);
                    break;
                case LogIn.google:
                    break;
            }
        }

        private void btn_fbLogin_Click(object sender, RoutedEventArgs e)
        {
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
                        var result = (IDictionary<string, object>)e.GetResultData();
                        patient.Id = (string)result["id"];
                        patient.Name = (string)result["name"];
                        patient.Gender = (string)result["gender"];
                        patient.Dob = (string)result["birthday"];
                        patient.ImagePath = string.Format("https://graph.facebook.com/{0}/picture?type={1}", patient.Id, "square");
                        
                    }
                };

                var parameters = new Dictionary<string, object>();
                parameters["fields"] = "id,name,first_name,last_name,gender,birthday";

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
        private void btn_goolgeLogin_Click(object sender, RoutedEventArgs e)
        {
            currentLoginWay = LogIn.google;
            LoginDialog logInDialog = new LoginDialog(currentLoginWay);
            logInDialog.ShowDialog();
        }

        private void btn_logout_Click(object sender, RoutedEventArgs e)
        {
            switch (currentLoginWay)
            {
                case LogIn.fb:
                    logoutFb();
                    break;
                case LogIn.google:
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
                    setting.FbAccessToken = string.Empty;
                }   
                //Close();
            };
            webBrowser.Navigate(logoutUrl.AbsoluteUri);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            setting.logInWay = currentLoginWay;
            Setting.SaveSettingConfig("setting",setting);
        }
    }
}
