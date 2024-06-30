using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Windows_App_Lock
{
    public partial class App : Application
    {
        public static Window m_window;
        private const string ThemeKey = "AppTheme";
        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
            LoadAppTheme();
        }

        private void LoadAppTheme()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(ThemeKey))
            {
                string theme = localSettings.Values[ThemeKey].ToString();
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
        }
        public static void SaveAppTheme(ElementTheme theme)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[ThemeKey] = theme.ToString();
            if (m_window.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
        }

    }
}
