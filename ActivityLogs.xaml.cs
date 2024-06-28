using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using System.Timers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

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

        private void ClearLogsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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
    }
}
