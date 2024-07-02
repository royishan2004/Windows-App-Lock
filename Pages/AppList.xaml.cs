using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Management.Deployment;
using Microsoft.Win32;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;


namespace Windows_App_Lock
{
    public sealed partial class AppList : Page
    {
        public List<AppInfo> Apps { get; set; } = new List<AppInfo>();

        public AppList()
        {
            this.InitializeComponent();
            LoadAppsFromSettings();
            appListBox.ItemsSource = Apps;
        }
        public Visibility EmptyListVisibility => Apps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        private async void AddApp_Click(object sender, RoutedEventArgs e)
        {
            var installedApps = await Task.Run(() => GetInstalledApplications());
            // Filter out apps already in the main app list
            var filteredApps = installedApps.Where(app => !Apps.Any(a => a.Name == app.Name)).ToList();
            appListView.ItemsSource = filteredApps;

            await addAppDialog.ShowAsync();
        }

        private void OKButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var selectedApps = appListView.SelectedItems.Cast<AppInfo>().ToList();

            if (selectedApps.Any())
            {
                // Add the selected apps to the main app list
                foreach (var app in selectedApps)
                {
                    Apps.Add(app);
                }
                appListBox.ItemsSource = null;
                appListBox.ItemsSource = Apps;
                UpdateEmptyListVisibility();

                // Save the updated Apps list
                SaveAppsToSettings();
            }
            
            // Close the dialog
            addAppDialog.Hide();
        }

        private void ClosePopupClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Simply close the dialog
            addAppDialog.Hide();
        }

        private void DeleteApp_Click(object sender, RoutedEventArgs e)
        {
            AppInfo selectedApp = (sender as FrameworkElement)?.DataContext as AppInfo;

            if (selectedApp != null)
            {
                Apps.Remove(selectedApp);
                appListBox.ItemsSource = null;
                appListBox.ItemsSource = Apps;
                UpdateEmptyListVisibility();

                SaveAppsToSettings();
            }
        }

        private List<AppInfo> GetInstalledApplications()
        {
            var installedApps = new List<AppInfo>();

            // Get UWP apps from PackageManager
            PackageManager packageManager = new PackageManager();
            var packages = packageManager.FindPackagesForUserWithPackageTypes("", PackageTypes.Main);

            foreach (var package in packages)
            {
                try
                {
                    var displayName = package.DisplayName;
                    var logo = package.Logo?.AbsoluteUri;

                    if (!string.IsNullOrEmpty(displayName))
                    {
                        installedApps.Add(new AppInfo
                        {
                            Name = displayName,
                            IconPath = logo
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to retrieve package info: {ex.Message}");
                }
            }

            // Get Win32 apps from the registry
            string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
            {
                if (key != null)
                {
                    foreach (string subkey_name in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                        {
                            if (subkey != null)
                            {
                                var displayName = subkey.GetValue("DisplayName") as string;
                                var iconPath = subkey.GetValue("DisplayIcon") as string;

                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    installedApps.Add(new AppInfo
                                    {
                                        Name = displayName,
                                        IconPath = iconPath
                                    });
                                }
                            }
                        }
                    }
                }
            }

            // Sort installedApps by Name
            installedApps = installedApps.OrderBy(app => app.Name).ToList();

            return installedApps;
        }
        private void UpdateEmptyListVisibility()
        {
            EmptyList.Visibility = EmptyListVisibility;
        }

        private void LoadAppsFromSettings()
        {
            // Access the local settings container
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            // Check if the AppList key exists
            if (localSettings.Values.ContainsKey("AppList"))
            {
                // Retrieve serializedApps from local settings
                var serializedApps = localSettings.Values["AppList"].ToString();

                // Deserialize serializedApps back into Apps list
                Apps = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AppInfo>>(serializedApps);

                // Refresh the appListBox with the loaded Apps list
                appListBox.ItemsSource = Apps;
            }
        }

        private void SaveAppsToSettings()
        {
            // Access the local settings container
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            // Convert Apps list to a format that can be saved (e.g., JSON)
            var serializedApps = Newtonsoft.Json.JsonConvert.SerializeObject(Apps);

            // Save serializedApps to local settings
            localSettings.Values["AppList"] = serializedApps;
        }

    }

    public class AppInfo
    {
        public string Name { get; set; }
        public string IconPath { get; set; }
    }
}
