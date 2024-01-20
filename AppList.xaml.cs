using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Numerics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Windows_App_Lock
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AppList : Page
    {
        public List<AppInfo> Apps { get; set; } = new List<AppInfo>();



        public AppList()
        {
            this.InitializeComponent();
            appListBox.ItemsSource = Apps;
        }
        private void ClosePopupClicked(object sender, RoutedEventArgs e)
        {
            addAppPopup.IsOpen = false;

        }
        private void AddApp_Click(object sender, RoutedEventArgs e)
        {
            // Implement the logic to add an app to the list
            // For demonstration purposes, let's add a sample app

            /*
            Apps.Add(new AppInfo { Name = "Sample App", Path = "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\Common7\\IDE\\devenv.exe",
              IconPath = "/Assets/StoreLogo.png"});
            appListBox.ItemsSource = null;
            appListBox.ItemsSource = Apps;
            */

            double horizontalOffset = 215;
            double verticalOffset = -75;


            addAppPopup.HorizontalOffset = horizontalOffset;
            addAppPopup.VerticalOffset = verticalOffset;


            addAppPopup.IsOpen = true;
            addAppPopup.Translation += new Vector3(0, 0, 32);

        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            addAppPopup.IsOpen = false;
        }
        private void DeleteApp_Click(object sender, RoutedEventArgs e)
        {
            // Get the DataContext of the sender (which is the specific AppInfo item in the list)
            AppInfo selectedApp = (sender as FrameworkElement)?.DataContext as AppInfo;

            if (selectedApp != null)
            {
                // Remove the selected app from the list
                Apps.Remove(selectedApp);
                appListBox.ItemsSource = null;
                appListBox.ItemsSource = Apps;
            }
        }
    }
    public class AppInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string IconPath { get; set; }
    }
}