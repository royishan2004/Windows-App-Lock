using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Management.Deployment;
using Microsoft.Win32;

namespace Windows_App_Lock
{
    public sealed partial class AppList : Page
    {
        public List<AppInfo> Apps { get; set; } = new List<AppInfo>();

        public AppList()
        {
            this.InitializeComponent();
            appListBox.ItemsSource = Apps;
        }

        private async void AddApp_Click(object sender, RoutedEventArgs e)
        {
            var installedApps = await Task.Run(() => GetInstalledApplications());
            appListView.ItemsSource = installedApps;

            await addAppDialog.ShowAsync();
        }

        private void OKButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var selectedApp = (AppInfo)appListView.SelectedItem;

            if (selectedApp != null)
            {
                // Add the selected app to the main app list
                Apps.Add(selectedApp);
                appListBox.ItemsSource = null;
                appListBox.ItemsSource = Apps;
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
    }

    public class AppInfo
    {
        public string Name { get; set; }
        public string IconPath { get; set; }
    }
}
