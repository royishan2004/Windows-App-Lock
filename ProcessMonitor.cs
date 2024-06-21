using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace Windows_App_Lock
{
    public class ProcessMonitor
    {
        private const string AppCredentialKey = "AppLockerCredential";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ResumeThread(IntPtr hThread);

        public async Task MonitorProcessesAsync()
        {
            string[] monitoredApps = { "WhatsApp"}; // Add other process names as needed

            foreach (var processName in monitoredApps)
            {
                var process = Process.GetProcessesByName(processName).FirstOrDefault();
                if (process != null)
                {
                    SuspendProcess(process);
                    bool isAuthenticated = await AuthenticateWithWindowsHelloAsync();
                    if (isAuthenticated)
                    {
                        ResumeProcess(process);
                        // Handle success message if needed
                    }
                    else
                    {
                        process.Kill(); //end process
                    }
                }
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
