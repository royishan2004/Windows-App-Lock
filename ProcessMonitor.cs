using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using System.Timers;

namespace Windows_App_Lock
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
        private static List<string> targetProcessNames = new List<string> { "WhatsApp Beta" }; // Add your target apps here
        private static HashSet<string> authenticatedProcesses = new HashSet<string>();
        private static Dictionary<string, bool> processVisibility = new Dictionary<string, bool>();
        private static Dictionary<string, bool> authenticationInProgress = new Dictionary<string, bool>();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ResumeThread(IntPtr hThread);

        public async Task StartMonitoringAsync()
        {
            _timer = new Timer(1000); // 1000 ms = 1 second
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

                        bool isAuthenticated = await monitor.AuthenticateWithWindowsHelloAsync();
                        if (isAuthenticated)
                        {
                            monitor.ResumeProcess(foreground);
                            authenticatedProcesses.Add(foregroundProcess);
                            await WaitForProcessToBeClosed(foregroundProcess);
                        }
                        else
                        {
                            foreground.Kill(); // End process
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
                await Task.Delay(1000); // Check every second
            }
        }

        private async Task<bool> AuthenticateWithWindowsHelloAsync()
        {
            try
            {
                var result = await KeyCredentialManager.RequestCreateAsync(AppCredentialKey, KeyCredentialCreationOption.ReplaceExisting);
                if (result.Status == KeyCredentialStatus.Success)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Handle authentication errors
                Console.WriteLine($"Authentication error: {ex.Message}");
            }
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
    }
}
