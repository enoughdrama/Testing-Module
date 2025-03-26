using System.Windows;
using System.Threading.Tasks;
using TestingApp.Models;
using TestingApp.Services;
using System.Windows.Controls;

namespace TestingApp.Windows
{
    public partial class AuthWindow : Window
    {
        public User AuthenticatedUser { get; private set; }
        private bool _isAuthenticating = false;

        public AuthWindow()
        {
            InitializeComponent();
            TxtFullName.Focus();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Prevent multiple authentication attempts
            if (_isAuthenticating) return;

            TxtError.Visibility = Visibility.Collapsed;

            var fullName = TxtFullName.Text;
            var uniqueKey = TxtUniqueKey.Password;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TxtError.Text = "Please enter your full name.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            if (string.IsNullOrWhiteSpace(uniqueKey))
            {
                TxtError.Text = "Please enter your unique key.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            // Get reference to the login button
            var loginButton = sender as Button;
            if (loginButton != null)
            {
                loginButton.IsEnabled = false;
                loginButton.Content = "Logging in...";
            }

            _isAuthenticating = true;

            try
            {
                // Call API for authentication (using singleton)
                AuthenticatedUser = await ApiService.Instance.Authenticate(fullName, uniqueKey);

                if (AuthenticatedUser != null)
                {
                    var testingWindow = new TestingWindow(AuthenticatedUser);

                    // Set owner to ensure proper window management
                    testingWindow.Owner = this;

                    // Show the testing window
                    testingWindow.Show();

                    // Hide (not close) this window to maintain state
                    this.Hide();
                }
                else
                {
                    TxtError.Text = "Invalid unique key. Please try again.";
                    TxtError.Visibility = Visibility.Visible;
                    TxtUniqueKey.Password = string.Empty;
                    TxtUniqueKey.Focus();
                }
            }
            catch (System.Exception ex)
            {
                if (!this.IsLoaded) return; // Don't show errors if window is closed/closing

                TxtError.Text = ex.Message;
                TxtError.Visibility = Visibility.Visible;
                TxtUniqueKey.Password = string.Empty;
                TxtUniqueKey.Focus();
            }
            finally
            {
                _isAuthenticating = false;

                // Restore button state if window is still open
                if (this.IsLoaded && loginButton != null)
                {
                    loginButton.IsEnabled = true;
                    loginButton.Content = "Start Assessment";
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}