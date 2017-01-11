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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_fbLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginDialog logInDialog = new LoginDialog(LogIn.fb);
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
                GetProfileFromFb(logInDialog.FacebookOAuthResult.AccessToken);
            }
            else
            {
                // for some reason we failed to get the access token.
                // most likely the user clicked don't allow.
                MessageBox.Show(logInDialog.FacebookOAuthResult.ErrorDescription);
            }
        }

        private void btn_goolgeLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginDialog logInDialog = new LoginDialog(LogIn.google);
            logInDialog.ShowDialog();
        }

        private void GetProfileFromFb(string accessKey)
        {
            try
            {
                var fb = new FacebookClient(accessKey);

                // FacebookClient's Get/Post/Delete methods only supports JSON response results.
                // For non json results, you will need to use different mechanism,

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
                        // error occurred
                        Application.Current.Dispatcher.Invoke(()=>
                            { MessageBox.Show(e.Error.Message); }
                        );
                    }
                    else
                    {
                        // the request was completed successfully

                        // now we can either cast it to IDictionary<string, object> or IList<object>
                        // depending on the type.
                        // For this example, we know that it is IDictionary<string,object>.
                        var result = (IDictionary<string, object>)e.GetResultData();

                        var id = (string)result["id"];
                        var name = (string)result["name"];
                        var firstName = (string)result["first_name"];
                        var lastName = (string)result["last_name"];
                        var gender = (string)result["gender"];
                        var dob = (string)result["birthday"];

                        // since this is an async callback, make sure to be on the right thread
                        // when working with the UI.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            tb_Id.Text = id;
                            tb_name.Text = name;
                            tb_gender.Text = gender;
                            tb_dob.Text = dob;

                            // load profilePhoto
                            // available picture types: square (50x50), small (50xvariable height), large (about 200x variable height) (all size in pixels)
                            // for more info visit http://developers.facebook.com/docs/reference/api
                            string profilePictureUrl = string.Format("https://graph.facebook.com/{0}/picture?type={1}", id, "square");
                            BitmapImage image = new BitmapImage();
                            image.BeginInit();
                            image.UriSource = new Uri(profilePictureUrl);
                            image.EndInit();
                            profilePhoto.Source = image;
                        }); 
                    }
                };

                // additional parameters can be passed and 
                // must be assignable from IDictionary<string, object> or anonymous object
                var parameters = new Dictionary<string, object>();
                parameters["fields"] = "id,name,first_name,last_name,gender,birthday";

                //fb.GetAsync("me", parameters);
                fb.GetTaskAsync("me", parameters);
            }
            catch (FacebookApiException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
