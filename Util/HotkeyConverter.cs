using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

// 將命名空間改回 xiaochao
namespace xiaochao
{
    internal class HotkeyConverter
    {
        /// <summary>
        /// 将字符串的快捷键转化为系统的快捷键
        /// </summary>
        public static Hotkey Convert_N(string hotkey)
        {
            Key keys = Key.None;
            ModifierKeys keys2 = ModifierKeys.None;
            string hotkeyLower = hotkey.ToLower(); // 转换为小写以便比较

            hotkey = hotkey.Replace(" ", "").Replace(",", "").Replace("+", "");

            if (hotkeyLower.Contains("ctrl"))
            {
                keys2 |= ModifierKeys.Control;
                hotkey = Regex.Replace(hotkey, "Ctrl", "", RegexOptions.IgnoreCase);
            }
            if (hotkeyLower.Contains("shift"))
            {
                keys2 |= ModifierKeys.Shift;
                hotkey = Regex.Replace(hotkey, "Shift", "", RegexOptions.IgnoreCase);
            }
            if (hotkeyLower.Contains("alt"))
            {
                keys2 |= ModifierKeys.Alt;
                hotkey = Regex.Replace(hotkey, "Alt", "", RegexOptions.IgnoreCase);
            }
            if (hotkeyLower.Contains("win"))
            {
                keys2 |= ModifierKeys.Windows;
                hotkey = Regex.Replace(hotkey, "win", "", RegexOptions.IgnoreCase);
            }

            if (Enum.TryParse<Key>(hotkey, true, out var parsedKey))
            {
                keys = parsedKey;
            }

            return new Hotkey(keys, keys2);
        }

        /// <summary>
        /// 做一层抽象，对双击的快捷键和正常的快捷键注册采用两种在注册模式
        /// </summary>
        //public static bool Convert(string hotkey, out Key _key, out Hotkey _hotkey)
        //{
        //    hotkey = hotkey.Replace(",", " ").Replace("+", " ");
        //    string hotkeyLower = hotkey.ToLower(); // 转换为小写以便比较

        //    var a = hotkey.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //    if (a.Length >= 2 && a[0].Equals(a[1], StringComparison.OrdinalIgnoreCase))
        //    {
        //        // 默认注册ctrl
        //        _key = Key.LeftCtrl;
        //        if (hotkeyLower.Contains("ctrl")) _key = Key.LeftCtrl;
        //        if (hotkeyLower.Contains("shift")) _key = Key.LeftShift;
        //        if (hotkeyLower.Contains("alt")) _key = Key.LeftAlt;
        //        if (hotkeyLower.Contains("win")) _key = Key.LWin;

        //        _hotkey = null;
        //        return true;
        //    }
        //    else
        //    {
        //        _key = Key.None;
        //        _hotkey = Convert_N(hotkey);
        //        return false;
        //    }
        //}

        /// <summary>
        /// (*** 核心修改 ***)
        /// 判斷快捷鍵類型，並返回對應的註冊方式
        /// </summary>
        /// <returns>
        /// true: 應使用鍵盤鉤子 (WindowHooker) - 適用於雙擊或單一功能鍵
        /// false: 應使用 RegisterHotKey - 適用於帶修飾鍵的組合鍵
        /// </returns>
        public static bool Convert(string hotkey, out Key _key, out Hotkey _hotkey)
        {
            _key = Key.None;
            _hotkey = null;
            if (string.IsNullOrWhiteSpace(hotkey)) return false;

            hotkey = hotkey.Replace(",", " ").Replace("+", " ");
            string[] parts = hotkey.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // --- 判斷 1: 雙擊修飾鍵 (例如 "Ctrl Ctrl") ---
            if (parts.Length >= 2 && parts[0].Equals(parts[1], StringComparison.OrdinalIgnoreCase))
            {
                string keyPartLower = parts[0].ToLower();
                if (keyPartLower.Contains("ctrl")) _key = Key.LeftCtrl;
                else if (keyPartLower.Contains("shift")) _key = Key.LeftShift;
                else if (keyPartLower.Contains("alt")) _key = Key.LeftAlt;
                else if (keyPartLower.Contains("win")) _key = Key.LWin;
                else _key = Key.None; // 無效的雙擊

                return true; // 使用 WindowHooker
            }

            // --- 判斷 2: 單一按鍵 (例如 "F2", "F3") ---
            if (parts.Length == 1)
            {
                // 嘗試將單一按鍵字串轉換為 Key 列舉
                if (Enum.TryParse<Key>(parts[0], true, out var singleKey))
                {
                    // 檢查是否為 F1-F24 功能鍵
                    if (singleKey >= Key.F1 && singleKey <= Key.F24)
                    {
                        _key = singleKey;
                        return true; // 使用 WindowHooker
                    }
                }
            }

            // --- 預設情況: 帶修飾鍵的組合鍵 (例如 "Ctrl+Alt+P") ---
            // 如果以上條件都不滿足，則認為是標準組合熱鍵
            _hotkey = Convert_N(hotkey);
            // 額外檢查：如果解析後沒有修飾鍵，這是不合法的，但為了避免崩潰，我們也讓它走 false
            if (_hotkey.Modifiers == ModifierKeys.None)
            {
                // 理論上不應該走到這裡，因為單一按鍵在上面已經被攔截了
                // 但作為保護，我們不讓它註冊
                return false;
            }

            return false; // 使用 RegisterHotKey
        }





    }

    //快捷键的组合类
    internal class Hotkey
    {
        public Key KeyCode { get; set; }
        public ModifierKeys Modifiers { get; set; }

        public Hotkey(Key keycode, ModifierKeys modifiers)
        {
            KeyCode = keycode;
            Modifiers = modifiers;
        }
    }

    // 官方提供的注册快捷键的方法
    public static class NativeMethods
    {

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}