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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Windows_App_Lock
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {

        private const string LockAppKey = "LockAppEnabled";
        public Settings()
        {
            this.InitializeComponent();
            LoadToggleSwitchState();
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

        private async void LockAppToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (LockAppToggleSwitch.IsOn)
            {
                // Authenticate using Windows Hello
                bool isAuthenticated = await AuthenticateWithWindowsHelloAsync();
                if (!isAuthenticated)
                {
                    // If authentication fails, turn off the toggle switch
                    LockAppToggleSwitch.IsOn = false;
                    InfoBarFailure.IsOpen = true;
                    InfoBarFailure.Message = "Failed to authenticate with Windows Hello.";
                }
            }
            SaveToggleSwitchState();
        }

        private async Task<bool> AuthenticateWithWindowsHelloAsync()
        {
            try
            {
                // Request a biometric verification
                KeyCredentialRetrievalResult result = await KeyCredentialManager.RequestCreateAsync("WindowsHelloSampleCredential", KeyCredentialCreationOption.ReplaceExisting);

                if (result.Status == KeyCredentialStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void LoadToggleSwitchState()
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
                SaveToggleSwitchState();
            }
        }

        private void SaveToggleSwitchState()
        {
            // Save the state to local settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[LockAppKey] = LockAppToggleSwitch.IsOn;
        }

    }

}

