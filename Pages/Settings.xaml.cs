using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows_App_Lock.Services;
using Windows_App_Lock.Helpers;

namespace Windows_App_Lock
{
    public sealed partial class Settings : Page
    {
        private const string LockAppKey = "LockAppEnabled";
        private const string ThemeKey = "AppTheme";
        private const string NotificationsKey = "NotificationsEnabled";

        public Settings()
        {
            this.InitializeComponent();
            Loaded += Settings_Loaded;
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSelfLockState();
            LoadThemeSetting();
            LoadNotificationsSetting();
            if (!LockAppToggleSwitch.IsOn)
            {
                TurnOffWarning.IsOpen = true;
            }
        }

        private async void TestCheck(object sender, RoutedEventArgs e)
        {
            var processMonitor = new ProcessMonitor();
            await processMonitor.StartMonitoringAsync();
        }

        private async void CheckWindowsHello(object sender, RoutedEventArgs e)
        {
            bool isWindowsHelloEnabled = await CheckWindowsHelloEnabledAsync();

            if (isWindowsHelloEnabled)
            {
                InfoBarSuccess.IsOpen = true;
                InfoBarFailure.IsOpen = false;
            }
            else
            {
                InfoBarSuccess.IsOpen = false;
                InfoBarFailure.IsOpen = true;
            }
        }

        private async Task<bool> CheckWindowsHelloEnabledAsync()
        {
            try
            {
                var keyCredentialManager = await KeyCredentialManager.IsSupportedAsync();
                return keyCredentialManager;
            }
            catch (Exception ex)
            {
                // Handle exceptions if necessary
                return false;
            }
        }

        private async void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTheme = ((ComboBoxItem)((ComboBox)sender).SelectedItem).Content.ToString();
            SetAppTheme(selectedTheme);
            await Task.Delay(500);
            SaveThemeSetting(selectedTheme);
        }

        private void SetAppTheme(string theme)
        {
            if (App.m_window?.Content is FrameworkElement rootElement)
            {
                if (theme == "Light")
                {
                    rootElement.RequestedTheme = ElementTheme.Light;
                }
                else if (theme == "Dark")
                {
                    rootElement.RequestedTheme = ElementTheme.Dark;
                }
                else
                {
                    rootElement.RequestedTheme = ElementTheme.Default;
                }
            }
        }

        private void LoadThemeSetting()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(ThemeKey))
            {
                string theme = localSettings.Values[ThemeKey].ToString();
                SetAppTheme(theme);

                foreach (ComboBoxItem item in ThemeComboBox.Items)
                {
                    if (item.Content.ToString() == theme)
                    {
                        ThemeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                ThemeComboBox.SelectedIndex = 2; // Default to Default theme
            }
        }

        private void SaveThemeSetting(string theme)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[ThemeKey] = theme;
        }

        private async void LockAppToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (LockAppToggleSwitch.IsOn)
            {
                TurnOffWarning.IsOpen = false;
            }
            if (!LockAppToggleSwitch.IsOn)
            {
                TurnOffWarning.IsOpen = true;
                // Authenticate using Windows Hello when toggling off
                bool isAuthenticated = await AuthenticationHelper.AuthenticateWithWindowsHelloAsync();
                if (!isAuthenticated)
                {
                    // If authentication fails, turn on the toggle switch again
                    LockAppToggleSwitch.IsOn = true;
                    TurnOffFailure.IsOpen = true;
                    TurnOffWarning.IsOpen = false;
                    return;
                }
            }
            SaveSelfLockState();
        }

        
        private void LoadSelfLockState()
        {
            // Load the state from local settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(LockAppKey))
            {
                LockAppToggleSwitch.IsOn = (bool)localSettings.Values[LockAppKey];
            }
            else
            {
                // Default value if not set
                LockAppToggleSwitch.IsOn = true;
                SaveSelfLockState();
            }
        }

        private void SaveSelfLockState()
        {
            // Save the state to local settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[LockAppKey] = LockAppToggleSwitch.IsOn;
        }

        private void LoadNotificationsSetting()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(NotificationsKey))
            {
                NotificationsToggleSwitch.IsOn = (bool)localSettings.Values[NotificationsKey];
            }
            else
            {
                NotificationsToggleSwitch.IsOn = true; // Default value
                SaveNotificationsSetting();
            }
        }

        private void SaveNotificationsSetting()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[NotificationsKey] = NotificationsToggleSwitch.IsOn;
        }

        private void NotificationsToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SaveNotificationsSetting();
        }
    }
}