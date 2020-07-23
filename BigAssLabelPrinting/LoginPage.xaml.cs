using System.Windows;
using System.Windows.Controls;

namespace BigAssLabelPrinting
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private static User user;

        public LoginPage()
        {
            InitializeComponent();
            loginButton.IsDefault = true;
            usernameTextbox.Focus();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
            user = new User(usernameTextbox.Text, passwordTextbox.Password);
            if (user.VerifyUser())
            {
                MainWindow.CurrentUser = user;
                MainWindow.UserVerified();
                MainWindow.MainInfoTextBlock.Text = "";
            }
            else
            {
                //infoTextbox.Text = "Authentication failed.";
            }
        }
    }
}
