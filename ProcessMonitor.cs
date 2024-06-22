using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using System.Timers;

namespace Windows_App_Lock
{

    public class ForegroundAppDetector {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public static string GetForegroundProcessName()
        {
            IntPtr hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out uint processId);

            Process proc = Process.GetProcessById((int)processId);
            return proc.ProcessName;
        }
    }
    public class ProcessMonitor
    {
        private const string AppCredentialKey = "AppLockerCredential";
        private static Timer _timer;
        private static string targetProcessName = "Discord";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ResumeThread(IntPtr hThread);

        public async Task BackGroundStart()
        {
            _timer = new Timer(1000); // 1000 ms = 1 second
            _timer.Elapsed += CheckForegroundApp;
            _timer.Start();
            
        }

        private static async void CheckForegroundApp(object state, ElapsedEventArgs e)
        {
            string foregroundProcess = ForegroundAppDetector.GetForegroundProcessName();
            Process[] processes = Process.GetProcessesByName(foregroundProcess);


            if (foregroundProcess.Equals(targetProcessName, StringComparison.OrdinalIgnoreCase))
            {
                _timer.Stop();
                Console.WriteLine($"{targetProcessName} is in the foreground.");
                if (processes.Length > 0)
                {
                    Process foreground = processes[0];
                    ProcessMonitor monitor = new ProcessMonitor();
                    monitor.SuspendProcess(foreground);

                    bool isAuthenticated = await monitor.AuthenticateWithWindowsHelloAsync();
                    if (isAuthenticated)
                    {
                        monitor.ResumeProcess(foreground);
                        // Handle success message if needed
                    }
                    else
                    {
                        foreground.Kill(); //end process
                    }
                    await WaitForProcessToLeaveForeground(foreground);
                }
            }
        }

        private static async Task WaitForProcessToLeaveForeground(Process process)
        {
            while (true)
            {
                string foregroundProcess = ForegroundAppDetector.GetForegroundProcessName();
                if (!foregroundProcess.Equals(targetProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    _timer.Start(); // Resume the timer after the target app is not in the foreground
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