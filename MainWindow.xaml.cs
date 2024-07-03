using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows_App_Lock.Helpers;

namespace Windows_App_Lock
{
    public sealed partial class MainWindow : Window
    {
        private const string LockAppKey = "LockAppEnabled";

        public MainWindow()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null);
            nvSample.SelectionChanged += nvSample_SelectionChanged;
            NavigateToPage("Home");

            // Check if app lock is enabled and authenticate
            CheckAppLockOnStartupAsync();
        }

        private void nvSample_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem settingsItem && settingsItem.Tag.Equals("Settings"))
            {
                contentFrame.Navigate(typeof(Settings));
            }
            else
            {
                // Handle regular item click
                string selectedItemTag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
                NavigateToPage(selectedItemTag);
            }
        }

        public void NavigateToPage(string pageTag)
        {
            switch (pageTag)
            {
                case "Home":
                    contentFrame.BackStack.Clear();
                    contentFrame.Navigate(typeof(Home));
                    break;

                case "AppList":
                    contentFrame.BackStack.Clear();
                    contentFrame.Navigate(typeof(AppList));
                    break;

                case "ActivityLogs":
                    contentFrame.BackStack.Clear();
                    contentFrame.Navigate(typeof(ActivityLogs));
                    break;

                case "Help":
                    contentFrame.BackStack.Clear();
                    contentFrame.Navigate(typeof(Help));
                    break;

                case "About":
                    contentFrame.BackStack.Clear();
                    contentFrame.Navigate(typeof(About));
                    break;

                default:
                    break;
            }
        }

        private async void CheckAppLockOnStartupAsync()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            bool isLockEnabled = localSettings.Values.ContainsKey(LockAppKey) && (bool)localSettings.Values[LockAppKey];

            if (isLockEnabled)
            {
                bool isAuthenticated = await AuthenticationHelper.AuthenticateWithWindowsHelloAsync();
                if (!isAuthenticated)
                {
                    blurGrid.Visibility = Visibility.Visible;
                    Application.Current.Exit();
                }
                else
                {
                    blurGrid.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                blurGrid.Visibility = Visibility.Collapsed;
            }
        }
    }
}
