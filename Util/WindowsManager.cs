// --- START OF WindowsManager.cs (MODIFIED) ---
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// 我將命名空間改為 xiaochao.Util，這是一個好習慣。
// 如果您想保持原樣，可以改回 namespace xiaochao
namespace xiaochao.Util
{
    // 將類別設為 public static，因為裡面的方法都是靜態的
    public static class WindowsManager
    {
        /// <summary>
        /// 获取当前前台窗口的标题
        /// </summary>
        public static string GetFrontWindowText()
        {
            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            return GetTitle(hWnd);
        }

        /// <summary>
        /// 获取当前前台窗口的句柄 (Handle)
        /// </summary>
        public static IntPtr GethWnd()
        {
            return NativeMethods.GetForegroundWindow();
        }

        /// <summary>
        /// 获取当前前台窗口归属进程的完整路径
        /// </summary>
        /// <returns>例如 "C:\Windows\System32\notepad.exe"</returns>
        public static string GetWindowProcessName()
        {
            try
            {
                IntPtr hWnd = NativeMethods.GetForegroundWindow();
                if (hWnd == IntPtr.Zero) return null;

                NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
                if (processId == 0) return null;

                // 嘗試使用新方法獲取完整路徑，這在處理 UWP 應用或高權限應用時更可靠
                // PROCESS_QUERY_LIMITED_INFORMATION (0x1000)
                IntPtr hProcess = NativeMethods.OpenProcess(0x1000, false, processId);
                if (hProcess != IntPtr.Zero)
                {
                    try
                    {
                        int capacity = 2048;
                        StringBuilder sb = new StringBuilder(capacity);
                        if (NativeMethods.QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
                        {
                            return sb.ToString();
                        }
                    }
                    finally
                    {
                        NativeMethods.CloseHandle(hProcess);
                    }
                }

                // 如果新方法失敗，回退到使用 System.Diagnostics.Process 的傳統方法
                // 這對大多數標準桌面應用有效
                Process p = Process.GetProcessById((int)processId);
                return p?.MainModule?.FileName;
            }
            catch (Exception)
            {
                // 捕獲所有可能的異常（例如權限不足無法訪問 MainModule）
                return null;
            }
        }

        /// <summary>
        /// 将对应名称的窗口放置在前台
        /// </summary>
        public static bool Set_window_frontByName(string windowName)
        {
            IntPtr hWnd = NativeMethods.FindWindow(null, windowName);
            return NativeMethods.SetForegroundWindow(hWnd);
        }

        /// <summary>
        /// 根据句柄获取窗口标题
        /// </summary>
        private static string GetTitle(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return "";

            int nChars = NativeMethods.GetWindowTextLength(handle);
            if (nChars == 0) return "";

            StringBuilder Buff = new StringBuilder(nChars + 1);
            if (NativeMethods.GetWindowText(handle, Buff, Buff.Capacity) > 0)
            {
                return Buff.ToString();
            }
            return "";
        }
    }
}
// --- END OF WindowsManager.cs (MODIFIED) ---