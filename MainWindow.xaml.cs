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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Windows_App_Lock
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            nvSample.SelectionChanged += nvSample_SelectionChanged; ;
            NavigateToPage("Home");
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
        private void NavigateToPage(string pageTag)
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

                case "AcitvityLogs":
                    contentFrame.BackStack.Clear();
                    contentFrame.Navigate(typeof(ActivityLogs));

                    break;

                case "About":
                    contentFrame.BackStack.Clear();
                    contentFrame.Navigate(typeof(About));

                    break;

                default:
                    break;
            } 
        } 


    }
}
