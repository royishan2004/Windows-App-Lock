using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Windows.Storage;
using Windows_App_Lock.Components;
using Windows_App_Lock.Helpers;

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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        public enum ThreadAccess : int
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

                        bool isAuthenticated = await AuthenticationHelper.AuthenticateWithWindowsHelloAsync();
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

        private static void LogAuthentication(string appName, string status)
        {
            // Load existing logs
            List<AuthenticationLog> existingLogs = LoadLogsFromLocalSettings();

            // Create new log entry
            AuthenticationLog newLog = new AuthenticationLog
            {
                AppName = appName,
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Status = status
            };

            // Append new log entry to existing logs
            existingLogs.Add(newLog);

            // Save the updated log list back to local settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["AuthenticationLogs"] = Newtonsoft.Json.JsonConvert.SerializeObject(existingLogs);

            // Also update the in-memory list
            authenticationLogs = existingLogs;
        }


        public static List<AuthenticationLog> LoadLogsFromLocalSettings()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("AuthenticationLogs"))
            {
                string logsJson = (string)localSettings.Values["AuthenticationLogs"];
                if (!string.IsNullOrEmpty(logsJson))
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<List<AuthenticationLog>>(logsJson);
                }
            }
            return new List<AuthenticationLog>();
        }
    }
}
