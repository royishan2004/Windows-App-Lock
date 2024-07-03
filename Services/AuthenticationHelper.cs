using System;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows_App_Lock.Components;
using Windows_App_Lock.Helpers;

namespace Windows_App_Lock.Helpers
{
    public static class AuthenticationHelper
    {
        private const string NotificationsKey = "NotificationsEnabled";
        private const string AppCredentialKey = "AppLockerCredential";

        public static async Task<bool> AuthenticateWithWindowsHelloAsync()
        {
            try
            {
                KeyCredentialRetrievalResult result = await KeyCredentialManager.RequestCreateAsync(AppCredentialKey, KeyCredentialCreationOption.ReplaceExisting);

                if (result.Status == KeyCredentialStatus.Success)
                {
                    ShowNotification("Authentication Success", "You have successfully authenticated.");
                    return true;
                }
            }
            catch (Exception)
            {
                // Handle the exception if necessary
            }

            ShowNotification("Authentication Failed", "Failed to authenticate. Please try again.");
            return false;
        }

        private static void ShowNotification(string title, string message)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            bool areNotificationsEnabled = localSettings.Values.ContainsKey(NotificationsKey) && (bool)localSettings.Values[NotificationsKey];

            if (areNotificationsEnabled)
            {
                NotificationHelper.ShowToastNotification(title, message);
            }
        }
    }
}
