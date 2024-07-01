using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using System.Timers;
using Windows.Storage;
using Windows_App_Lock.Components;

namespace Windows_App_Lock.Services
{
    public class ForegroundAppDetector
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        public static string GetForegroundProcessName()
        {
            IntPtr hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out uint processId);

            Process proc = Process.GetProcessById((int)processId);
            return proc.ProcessName;
        }

        public static bool IsWindowVisible(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                IntPtr hwnd = processes[0].MainWindowHandle;
                return IsWindowVisible(hwnd);
            }
            return false;
        }
    }

    public class ProcessMonitor
    {
        private const string AppCredentialKey = "AppLockerCredential";
        private const string NotificationsKey = "NotificationsEnabled";
        private static Timer _timer;
        private static List<string> targetProcessNames = new List<string> { "Discord", "opera", "Notepad", "LenovoVantage" };
        private static HashSet<string> authenticatedProcesses = new HashSet<string>();
        private static Dictionary<string, bool> processVisibility = new Dictionary<string, bool>();
        private static Dictionary<string, bool> authenticationInProgress = new Dictionary<string, bool>();
        private static List<AuthenticationLog> authenticationLogs = new List<AuthenticationLog>();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ResumeThread(IntPtr hThread);

        public async Task StartMonitoringAsync()
        {
            _timer = new Timer(500);
            _timer.Elapsed += CheckForegroundApp;
            _timer.Start();

            foreach (var processName in targetProcessNames)
            {
                processVisibility[processName] = ForegroundAppDetector.IsWindowVisible(processName);
                authenticationInProgress[processName] = false;
            }
        }

        private static async void CheckForegroundApp(object sender, ElapsedEventArgs e)
        {
            string foregroundProcess = ForegroundAppDetector.GetForegroundProcessName();
            Process[] processes = Process.GetProcessesByName(foregroundProcess);

            if (targetProcessNames.Contains(foregroundProcess, StringComparer.OrdinalIgnoreCase) && !authenticatedProcesses.Contains(foregroundProcess))
            {
                bool isVisible = ForegroundAppDetector.IsWindowVisible(foregroundProcess);

                if (isVisible && !authenticationInProgress[foregroundProcess])
                {
                    Console.WriteLine($"{foregroundProcess} window is visible and in the foreground.");
                    if (processes.Length > 0)
                    {
                        authenticationInProgress[foregroundProcess] = true;

                        Process foreground = processes[0];
                        ProcessMonitor monitor = new ProcessMonitor();
                        monitor.SuspendProcess(foreground);

                        bool isAuthenticated = await monitor.AuthenticateWithWindowsHelloAsync(foregroundProcess);
                        if (isAuthenticated)
                        {
                            monitor.ResumeProcess(foreground);
                            LogAuthentication(foregroundProcess, "Unlocked");
                            authenticatedProcesses.Add(foregroundProcess);
                            await WaitForProcessToBeClosed(foregroundProcess);
                        }
                        else
                        {
                            LogAuthentication(foregroundProcess, "Auth Failed");
                            foreground.Kill();
                        }

                        authenticationInProgress[foregroundProcess] = false;
                    }
                }
            }

            foreach (var processName in targetProcessNames)
            {
                bool isVisible = ForegroundAppDetector.IsWindowVisible(processName);
                if (processVisibility[processName] && !isVisible)
                {
                    processVisibility[processName] = false;
                    authenticatedProcesses.Remove(processName);
                }
                else if (!processVisibility[processName] && isVisible)
                {
                    processVisibility[processName] = true;
                }
            }
        }

        private static async Task WaitForProcessToBeClosed(string processName)
        {
            while (true)
            {
                bool isVisible = ForegroundAppDetector.IsWindowVisible(processName);
                if (!isVisible)
                {
                    authenticatedProcesses.Remove(processName);
                    break;
                }
                await Task.Delay(1000);
            }
        }

        private async Task<bool> AuthenticateWithWindowsHelloAsync(string processName)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            bool areNotificationsEnabled = localSettings.Values.ContainsKey(NotificationsKey) && (bool)localSettings.Values[NotificationsKey];

            //string successImage = "ms-appx:///Assets/icons8-correct-100.png"; 
            //string failureImage = "ms-appx:///Assets/icons8-cancel-100.png";

            try
            {
                var result = await KeyCredentialManager.RequestCreateAsync(AppCredentialKey, KeyCredentialCreationOption.ReplaceExisting);
                if (result.Status == KeyCredentialStatus.Success)
                {
                    if (areNotificationsEnabled) { NotificationHelper.ShowToastNotification("Authentication Success", $"You have successfully authenticated {processName}."); }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication error: {ex.Message}");
            }
            if (areNotificationsEnabled) { NotificationHelper.ShowToastNotification("Authentication Failed", $"Failed to authenticate {processName}. Please try again."); }
            return false;
        }

        private void SuspendProcess(Process process)
        {
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }
                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }

        private void ResumeProcess(Process process)
        {
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }
                ResumeThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        private enum ThreadAccess : int
        {
            TERMINATE = 0x0001,
            SUSPEND_RESUME = 0x0002,
            GET_CONTEXT = 0x0008,
            SET_CONTEXT = 0x0010,
            SET_INFORMATION = 0x0020,
            QUERY_INFORMATION = 0x0040,
            SET_THREAD_TOKEN = 0x0080,
            IMPERSONATE = 0x0100,
            DIRECT_IMPERSONATION = 0x0200
        }

        private static void LogAuthentication(string processName, string status)
        {
            try
            {
                var logEntry = new AuthenticationLog
                {
                    AppName = processName,
                    Date = DateTime.Now.ToShortDateString(),
                    Time = DateTime.Now.ToLongTimeString(),
                    Status = status // Log the status
                };
                authenticationLogs.Add(logEntry);
                SaveLogsToLocalSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log authentication: {ex.Message}");
            }
        }


        private static void SaveLogsToLocalSettings()
        {
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                List<string> logEntries = new List<string>();

                // Load existing logs if any
                if (localSettings.Values.ContainsKey("AuthenticationLogs"))
                {
                    string existingLogs = localSettings.Values["AuthenticationLogs"] as string;
                    logEntries.AddRange(existingLogs.Split(';'));
                }

                // Append new logs
                foreach (var log in authenticationLogs)
                {
                    string logEntry = $"{log.AppName},{log.Date},{log.Time},{log.Status}"; // Added Status
                    logEntries.Add(logEntry);
                }

                // Save all logs
                localSettings.Values["AuthenticationLogs"] = string.Join(";", logEntries);
                Console.WriteLine($"Logs saved to local settings.");

                authenticationLogs.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save logs: {ex.Message}");
            }
        }

        public static List<AuthenticationLog> LoadLogsFromLocalSettings()
        {
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                List<AuthenticationLog> logs = new List<AuthenticationLog>();

                if (localSettings.Values.ContainsKey("AuthenticationLogs"))
                {
                    string logsData = localSettings.Values["AuthenticationLogs"] as string;
                    string[] logEntries = logsData.Split(';');

                    foreach (string logEntry in logEntries)
                    {
                        if (!string.IsNullOrEmpty(logEntry))
                        {
                            string[] logParts = logEntry.Split(',');
                            if (logParts.Length == 4)
                            {
                                logs.Add(new AuthenticationLog
                                {
                                    AppName = logParts[0],
                                    Date = logParts[1],
                                    Time = logParts[2],
                                    Status = logParts[3] // Added Status
                                });
                            }
                        }
                    }
                }

                return logs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load logs: {ex.Message}");
                return new List<AuthenticationLog>();
            }
        }
    }
}
