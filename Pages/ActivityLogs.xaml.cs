using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Microsoft.UI.Dispatching;
using CommunityToolkit.WinUI.UI.Controls;
using Windows_App_Lock.Components;
using Windows_App_Lock.Services;

namespace Windows_App_Lock
{
    public sealed partial class ActivityLogs : Page
    {
        public ObservableCollection<AuthenticationLog> Logs { get; set; } = new ObservableCollection<AuthenticationLog>();
        private DispatcherTimer _updateTimer;

        public ActivityLogs()
        {
            this.InitializeComponent();
            InitializeTimer();
            LoadLogs(); // Initial load of logs

            LogsDataGrid.LoadingRow += DataGrid_LoadingRow;
        }

        private void InitializeTimer()
        {
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(0.5); // Update interval
            _updateTimer.Tick += UpdateLogs;
            _updateTimer.Start();
        }

        private void UpdateLogs(object sender, object e)
        {
            // Load logs on the UI thread using DispatcherQueue
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                LoadLogs();
            });
        }

        private void LoadLogs()
        {
            try
            {
                var newLogs = ProcessMonitor.LoadLogsFromLocalSettings()
                                            .OrderByDescending(log => DateTime.Parse($"{log.Date} {log.Time}"))
                                            .ToList();

                // Update the existing collection
                Logs.Clear();
                foreach (var log in newLogs)
                {
                    Logs.Add(log);
                }

                // Debug statement
                Console.WriteLine($"Displayed {Logs.Count} log(s) in the DataGrid.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load logs: {ex.Message}");
            }
        }

        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["AuthenticationLogs"] = string.Empty;
                Logs.Clear();

                Console.WriteLine("Logs cleared.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clear logs: {ex.Message}");
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var log = e.Row.DataContext as AuthenticationLog;
            if (log != null)
            {
                if (log.Status == "Auth Failed")
                {
                    e.Row.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                }
            }
        }
    }
}
