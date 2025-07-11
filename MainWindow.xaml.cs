using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using xiaochao.Util;
using Path = System.IO.Path;

namespace xiaochao
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string LastFrontWindowTitle = "";
        private string LastFrontWindowProcess = "";

        // (新) 字典和設定檔路徑
        private Dictionary<string, string> _sheetNameToExePathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly string _iconCacheFilePath; // 設定檔路徑


        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region 固定变量定义
        const int HOTKEY_id = 6666;
        #endregion

        #region 属性定义
        private Structofdata _data = Structofdata.InitInstance();
        public Structofdata Data
        {
            get => _data;
            set
            {
                _data = value;
                OnPropertyChanged(nameof(Data));
            }
        }

        // (新) 用於 ComboBox 的屬性
        private List<string> _availableCheatSheets = new List<string>();
        public List<string> AvailableCheatSheets
        {
            get => _availableCheatSheets;
            set
            {
                _availableCheatSheets = value;
                OnPropertyChanged(nameof(AvailableCheatSheets));
            }
        }


        // (新) 用於綁定應用程式圖示的屬性
        private ImageSource _frontAppIcon;
        public ImageSource FrontAppIcon
        {
            get => _frontAppIcon;
            set
            {
                _frontAppIcon = value;
                OnPropertyChanged(nameof(FrontAppIcon));
            }
        }

        // (新) 輔助方法：將 System.Drawing.Icon 轉換為 WPF 的 ImageSource
        private static ImageSource IconToImageSource(Icon icon)
        {
            if (icon == null) return null;

            // 使用 using 確保 icon handle 被正確釋放
            using (var bmp = icon.ToBitmap())
            {
                var hBitmap = bmp.GetHbitmap();
                try
                {
                    return Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    // 釋放 GDI 物件
                    NativeMethods.DeleteObject(hBitmap);
                }
            }
        }

        private void LoadIconCache()
        {
            try
            {
                if (File.Exists(_iconCacheFilePath))
                {
                    _sheetNameToExePathCache.Clear();
                    foreach (string line in File.ReadLines(_iconCacheFilePath))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // 尋找第一個空格，將行分割成 key 和 value
                        int firstSpaceIndex = line.IndexOf(' ');
                        if (firstSpaceIndex > 0)
                        {
                            string key = line.Substring(0, firstSpaceIndex);
                            string value = line.Substring(firstSpaceIndex + 1).Trim();

                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                            {
                                _sheetNameToExePathCache[key] = value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading icon cache: {ex.Message}");
            }
        }

        /// <summary>
        /// 將當前的圖示快取儲存到 icon_cache.md
        /// </summary>
        private void SaveIconCache()
        {
            try
            {
                string configDir = Path.GetDirectoryName(_iconCacheFilePath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                // 將字典轉換為 "key value" 格式的行
                var lines = _sheetNameToExePathCache.Select(kvp => $"{kvp.Key} {kvp.Value}");

                // 使用 WriteAllLines 覆蓋寫入，因為我們總是用一個更完整的字典來更新
                File.WriteAllLines(_iconCacheFilePath, lines);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving icon cache: {ex.Message}");
            }
        }
        #endregion


        private string _selectedCheatSheet;
        public string SelectedCheatSheet
        {
            get => _selectedCheatSheet;
            set
            {
                if (_selectedCheatSheet != value)
                {
                    _selectedCheatSheet = value;
                    OnPropertyChanged(nameof(SelectedCheatSheet));
                }
            }
        }
        // (新) 結束

        private static string BaseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static string AppDir = Path.Combine(BaseDir, "XiaoChao.exe");
        private readonly string _sub_directory = Path.Combine(BaseDir, "data");
        private readonly string _local_directory = Path.Combine(BaseDir, "local");
        private static WindowHooker _hooker;
        public ConfigManager ConfigManagerInstance { get; set; } = ConfigManager.GetInstance();
        public int Normal_data_height { get; set; } = 27;
        public int Bigtitle_data_height { get; set; } = 30;
        public int Column_count { get; set; } = 4; // 修改欄位
        public int Window_Height { get; set; } = 750; //修改高度
        public int Window_Width { get; set; } = 1200;
        public int Column_Width { get; set; }
        public int Colum_Item_Width { get; set; }
        public string Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();
       

        #region 构造函数
        public MainWindow()
        {
            Column_count = ConfigManagerInstance.Column_count;
            Column_Width = (Window_Width - 40) / Column_count;
            Colum_Item_Width = Column_Width - 20;
            Window_Height = ConfigManagerInstance.Window_Height;
            Window_Width = ConfigManagerInstance.Window_Width;
            // (新) 初始化設定檔路徑
            _iconCacheFilePath = Path.Combine(BaseDir, "config", "icon_cache.md");


            InitializeComponent();


            this.SizeChanged += MainWindow_SizeChanged;


            Window_Height = ConfigManagerInstance.Window_Height;
            this.Height = Window_Height; // MainWindow.xaml 中設定 Height
            this.Width = Window_Width; // MainWindow.xaml 中設定 Width


            // (新) 將 DataContext 設為自身，以便綁定新屬性
            this.DataContext = this;

            InitHwnd();
            Hide();
            Check_Directory_Exist();
            LoadIconCache();
            StartUp(ConfigManagerInstance.Start_Up);

            // (修改) 首次啟動時不需載入資料，在 Switchwindow 時才載入
            string[] args = Environment.GetCommandLineArgs();
            if (!args.Contains("--autostart"))
            {
                Show();
                // (修改) 首次顯示時需要更新資料
                UpdateDataAndUI();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Register();
        }
        #endregion

        protected override void OnClosed(EventArgs e)
        {
            NativeMethods.UnregisterHotKey(WindowsManager.GethWnd(), HOTKEY_id);
            base.OnClosed(e);
        }

        /// <summary>
        /// (新) 核心邏輯：根據選擇的 Sheet 名稱載入資料
        /// </summary>
        /// <param name="sheetName">要載入的設定檔名稱 (無副檔名)</param>
        private void LoadDataForSheet(string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName)) return;

            string bigtitle = this.LastFrontWindowTitle;
            string fullExePath = this.LastFrontWindowProcess;

            string exeNameOnly = Path.GetFileNameWithoutExtension(fullExePath);
            string targetFile = FindSheetFilePath(sheetName);

            // 尋找對應的檔案路徑
            string[] data_files = Directory.GetFiles(_sub_directory, $"{sheetName}.*");
            string[] local_files = Directory.GetFiles(_local_directory, $"{sheetName}.*");
            // string targetFile = (local_files.Length > 0 ? local_files[0] : (data_files.Length > 0 ? data_files[0] : null));

            var tempkeyValueAssembles = new List<KeyValueAssemble>();

            if (targetFile != null && File.Exists(targetFile))
            {
                // 循環遍歷每個文件的每一行
                foreach (string line in File.ReadLines(targetFile))
                {
                    string line_clean = line.Trim();
                    if (line_clean.Length > 0)
                    {
                        if (line_clean[0] == '#')
                        {
                            tempkeyValueAssembles.Add(new KeyValueAssemble());
                            tempkeyValueAssembles.Last().SmallTitle = line_clean.Substring(1).Trim();
                            tempkeyValueAssembles.Last().Height += Bigtitle_data_height;
                        }
                        else
                        {
                            if (tempkeyValueAssembles.Count == 0) tempkeyValueAssembles.Add(new KeyValueAssemble());
                            string[] key_value = line_clean.Split(' ');
                            if (key_value.Length < 2) continue;
                            KeyValue keyValue = new KeyValue();
                            keyValue.Height = Normal_data_height;
                            keyValue.Key = new KeyList(key_value[0]);
                            keyValue.Value = key_value[1].Replace('+', ' ');
                            if (key_value.Length >= 3)
                            {
                                keyValue.Url = key_value[2];
                                tempkeyValueAssembles.Last().Ftype = FunctionType.ContainURl;
                            }
                            tempkeyValueAssembles.Last().KeyValues.Add(keyValue);
                            tempkeyValueAssembles.Last().Height += Normal_data_height;
                        }
                    }
                }
            }

            KeyValueAssembleList[] keyValueAssemblesArray = new KeyValueAssembleList[Column_count];
            for (int i = 0; i < Column_count; i++)
            {
                keyValueAssemblesArray[i] = new KeyValueAssembleList();
            }

            foreach (KeyValueAssemble keyValueAssemble in tempkeyValueAssembles)
            {
                switch (keyValueAssemble.Ftype)
                {
                    case FunctionType.Normal:
                        int min_length_index = 0;
                        for (int i = 1; i < Column_count - 1; i++)
                        {
                            if (keyValueAssemblesArray[i].height < keyValueAssemblesArray[min_length_index].height)
                            {
                                min_length_index = i;
                            }
                        }
                        keyValueAssemblesArray[min_length_index].KeyValueAssemblesListInstance.Add(keyValueAssemble);
                        keyValueAssemblesArray[min_length_index].height += keyValueAssemble.Height;
                        break;
                    case FunctionType.ContainURl:
                        keyValueAssemblesArray[keyValueAssemblesArray.Length - 1].KeyValueAssemblesListInstance.Add(keyValueAssemble);
                        keyValueAssemblesArray[keyValueAssemblesArray.Length - 1].height += keyValueAssemble.Height;
                        break;
                }
            }
            // 使用新資料更新 Data 屬性，觸發 UI 更新
            Data = Structofdata.InitInstance(keyValueAssemblesArray, bigtitle, exeNameOnly);
        }

        /// <summary>
        /// (修改) 更新資料和UI，包括下拉選單和內容
        /// </summary>
        private void UpdateDataAndUI()
        {
            // 1. 取得所有可用的 Cheat Sheets
            var allFiles = Directory.GetFiles(_sub_directory, "*.*").Concat(Directory.GetFiles(_local_directory, "*.*"));
            var sheetNames = allFiles.Select(f => Path.GetFileNameWithoutExtension(f))
                                     .Where(n => !string.IsNullOrEmpty(n))
                                     .Distinct(StringComparer.OrdinalIgnoreCase) // 忽略大小寫來去重
                                     .OrderBy(n => n)
                                     .ToList();

            if (!sheetNames.Contains("全局", StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(Path.Combine(_sub_directory, "全局.md")) || File.Exists(Path.Combine(_local_directory, "全局.md")))
                {
                    sheetNames.Insert(0, "全局");
                }
            }
            AvailableCheatSheets = sheetNames;

            // 2. 決定預設要顯示哪一個
            string bigtitle = WindowsManager.GetFrontWindowText();
            string bigtitle_exe = WindowsManager.GetWindowProcessName();
            LastFrontWindowTitle = bigtitle;
            LastFrontWindowProcess = bigtitle_exe;

            // (新) 根據 bigtitle_exe 提取並設定應用程式圖示
            try
            {
                if (!string.IsNullOrEmpty(bigtitle_exe) && File.Exists(bigtitle_exe))
                {
                    using (Icon appIcon = System.Drawing.Icon.ExtractAssociatedIcon(bigtitle_exe))
                    {
                        FrontAppIcon = IconToImageSource(appIcon);
                    }

                    string sheetKey = Path.GetFileNameWithoutExtension(bigtitle_exe);

                    if (!string.IsNullOrEmpty(sheetKey) && !_sheetNameToExePathCache.ContainsKey(sheetKey))
                    {
                        _sheetNameToExePathCache[sheetKey] = bigtitle_exe;
                        SaveIconCache();
                    }
                }
                else
                {
                    FrontAppIcon = null;
                }
            }
            catch (Exception) { FrontAppIcon = null; }

            string selected = "全局"; // 默認為全局
            string exeNameOnly = string.IsNullOrEmpty(bigtitle_exe) ? "" : Path.GetFileNameWithoutExtension(bigtitle_exe);

            if (AvailableCheatSheets.Any())
            {
                string bigtitleLower = bigtitle.ToLower();

                var match = AvailableCheatSheets.FirstOrDefault(s =>
                {
                    if (string.IsNullOrEmpty(s)) return false;
                    // 比較1: 視窗標題是否包含 sheet 名稱 (不區分大小寫)
                    if (bigtitleLower.Contains(s.ToLower())) return true;
                    // 比較2: exe 檔名是否等於 sheet 名稱 (不區分大小寫)
                    if (!string.IsNullOrEmpty(exeNameOnly) && exeNameOnly.Equals(s, StringComparison.OrdinalIgnoreCase)) return true;
                    return false;
                });

                if (!string.IsNullOrEmpty(match))
                {
                    selected = match;
                }
            }

            // (*** 這裡是新的核心邏輯 ***)
            // 處理新應用程式的情況：如果找不到任何匹配的設定檔，
            // 且前景視窗是一個有效的應用程式（不是我們自己或空），
            // 就將該應用程式名稱作為預設選中項。
            if (selected.Equals("全局", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(exeNameOnly) &&
                !exeNameOnly.Equals("xiaochao", StringComparison.OrdinalIgnoreCase))
            {
                // 將新應用程式名稱設定為選中項
                selected = exeNameOnly;

                // 如果這個新名稱不在下拉選單列表中，動態加入它
                if (!AvailableCheatSheets.Contains(selected, StringComparer.OrdinalIgnoreCase))
                {
                    var tempList = new List<string>(AvailableCheatSheets);
                    tempList.Add(selected);
                    // 重新排序並更新UI
                    AvailableCheatSheets = tempList.OrderBy(s => s).ToList();
                }
            }

            // 3. 設定選中的項目並載入資料
            // 直接設定後端欄位，避免觸發 SelectionChanged 事件造成重複載入
            _selectedCheatSheet = selected;
            OnPropertyChanged(nameof(SelectedCheatSheet)); // 手動通知UI更新選中項
            LoadDataForSheet(selected);
        }

        /// <summary>
        /// 切换窗口显示状态
        /// </summary>
        private void Switchwindow()
        {
            Debug.WriteLine($"[XiaoChao] >>> Switchwindow() method triggered! Current visibility: {this.IsVisible}");
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Screen screen = Screen.FromPoint(System.Windows.Forms.Cursor.Position);
                var scaleRatio = Math.Max(VisualTreeHelper.GetDpi(this).DpiScaleX, VisualTreeHelper.GetDpi(this).DpiScaleY);

                this.Left = (screen.WorkingArea.Left + (screen.WorkingArea.Width - this.Width * scaleRatio) / 2) / scaleRatio;
                this.Top = (screen.WorkingArea.Top + (screen.WorkingArea.Height - this.Height * scaleRatio) / 2) / scaleRatio;

                // (修改) 呼叫新的更新方法
                UpdateDataAndUI();

                Show();
                Activate();
                Focus();
            }
        }

        // (新) ComboBox 選擇變更事件
        private void CheatSheetSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is string selectedSheet)
            {
                // 1. 載入對應的快捷鍵資料 (原有邏輯)
                LoadDataForSheet(selectedSheet);

                // 2. (新) 更新對應的應用程式圖示
                UpdateIconForSheet(selectedSheet);
            }
        }

        #region Unchanged Methods

        /// <summary>
        /// (*** 新增輔助方法 ***)
        /// 根據 sheet 名稱在 data 和 local 資料夾中尋找對應的檔案路徑。
        /// </summary>
        /// <param name="sheetName">設定檔的名稱 (不含副檔名)</param>
        /// <returns>完整的檔案路徑，如果找不到則返回 null。</returns>
        private string FindSheetFilePath(string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                return null;
            }

            // 優先搜尋 local 資料夾 (使用者自訂)
            var localFiles = Directory.GetFiles(_local_directory, $"{sheetName}.*");
            if (localFiles.Length > 0)
            {
                return localFiles[0];
            }

            // 若 local 找不到，再搜尋 data 資料夾 (預設)
            var dataFiles = Directory.GetFiles(_sub_directory, $"{sheetName}.*");
            if (dataFiles.Length > 0)
            {
                return dataFiles[0];
            }

            // 都找不到
            return null;
        }




        /// <summary>
        /// 注册快捷键, 需要window的handle
        /// </summary>
        private void Register()
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            string shortcut = ConfigManagerInstance.Shortcut.ToLower().Trim();

            // 定義哪些鍵是給「雙擊」功能使用的
            var doublePressKeys = new[] { "ctrl", "lcontrol", "rcontrol", "alt", "lalt", "ralt", "shift", "lshift", "rshift" };

            if (doublePressKeys.Contains(shortcut))
            {
                // --- 情況一：註冊雙擊快捷鍵 ---
                PInvoke.User32.VirtualKey keyToHook;
                switch (shortcut)
                {
                    case "ctrl":
                    case "lcontrol":
                        keyToHook = PInvoke.User32.VirtualKey.VK_LCONTROL;
                        break;
                    case "rcontrol":
                        keyToHook = PInvoke.User32.VirtualKey.VK_RCONTROL;
                        break;
                    case "alt":
                    case "lalt":
                        keyToHook = PInvoke.User32.VirtualKey.VK_LMENU;
                        break;
                    case "ralt":
                        keyToHook = PInvoke.User32.VirtualKey.VK_RMENU;
                        break;
                    case "shift":
                    case "lshift":
                        keyToHook = PInvoke.User32.VirtualKey.VK_LSHIFT;
                        break;
                    case "rshift":
                        keyToHook = PInvoke.User32.VirtualKey.VK_RSHIFT;
                        break;
                    default:
                        // 理論上不會執行到這裡
                        System.Windows.MessageBox.Show($"不支援的雙擊快捷鍵: {shortcut}");
                        return;
                }

                _hooker = new WindowHooker();
                _hooker._c_v_key = keyToHook;
                _hooker.Key_activate = Switchwindow;
            }
            else
            {
                // --- 情況二：註冊標準全局熱鍵 (例如 F2, Alt+S, Ctrl+F3) ---
                try
                {
                    var converter = new KeyGestureConverter();
                    var gesture = (KeyGesture)converter.ConvertFromString(shortcut.Replace("+", "-")); // KeyGestureConverter 用 '-' 分隔

                    var modifiers = (uint)gesture.Modifiers;
                    var key = (uint)KeyInterop.VirtualKeyFromKey(gesture.Key);

                    bool success = NativeMethods.RegisterHotKey(hWnd, HOTKEY_id, modifiers, key);
                    if (!success)
                    {
                        // 嘗試註銷後重新註冊，以防萬一
                        NativeMethods.UnregisterHotKey(hWnd, HOTKEY_id);
                        success = NativeMethods.RegisterHotKey(hWnd, HOTKEY_id, modifiers, key);
                    }

                    if (!success)
                    {
                        System.Windows.MessageBox.Show($"快速鍵 '{shortcut}' 註冊失敗，可能已被其他程式占用。請重新設定。");
                        Close();
                        return;
                    }

                    // 為標準熱鍵添加消息回調
                    HwndSource _source = HwndSource.FromHwnd(hWnd);
                    _source.AddHook(HotKeyHook);

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"無法解析快速鍵 '{shortcut}'。請檢查格式（例如：Alt+S, F2, Ctrl+Shift+A）。\n錯誤訊息: {ex.Message}");
                    Close();
                }
            }
        }


        private IntPtr HotKeyHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY)
            {
                Debug.WriteLine($"[XiaoChao] >>> HotKeyHook received a WM_HOTKEY message! wParam: {wParam}");
            }

            if (wParam == (IntPtr)HOTKEY_id && msg == WM_HOTKEY)
            {
                Switchwindow();
                handled = true;
            }
            return IntPtr.Zero;
        }








        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            //if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "设置.md")))
            //{
            //    System.Diagnostics.Process.Start("Explorer.exe", Path.Combine(BaseDir, "设置.md"));
            //}
            //else
            //{
            //    System.Windows.MessageBox.Show("请自行创建 {设置.md} 文件");
            //    System.Diagnostics.Process.Start("Explorer.exe", Directory.GetCurrentDirectory());
            //}
            //Hide();
            SettingWindow sw = new SettingWindow();
            sw.Owner = this;  // 設定父視窗，確保 Modal 效果
            sw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            sw.ShowDialog();  // 模態方式打開






        }







        private void Check_Directory_Exist()
        {
            if (!Directory.Exists(_sub_directory))
            {
                Directory.CreateDirectory(_sub_directory);
            }
            if (!Directory.Exists(_local_directory))
            {
                Directory.CreateDirectory(_local_directory);
            }
        }
        private void Data_Click(object sender, RoutedEventArgs e)
        {
            string sheetToEdit = SelectedCheatSheet;

            if (string.IsNullOrEmpty(sheetToEdit))
            {
                System.Windows.MessageBox.Show("請先從下拉選單中選擇一個要編輯的資料檔案。");
                return;
            }

            string filePath = FindSheetFilePath(sheetToEdit);

            // 如果檔案不存在，則在 local 資料夾中為其建立一個新檔案
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.Combine(_local_directory, $"{sheetToEdit}.md");
                try
                {
                    // 寫入一些預設內容，引導使用者編輯
                    string defaultContent = $"# {sheetToEdit} 的快捷鍵設定\n\n# 新增的第一個分類\nCtrl+N 新功能\n";
                    File.WriteAllText(filePath, defaultContent);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"無法建立新的設定檔 '{filePath}'。\n錯誤: {ex.Message}");
                    return;
                }
            }

            // 使用記事本開啟檔案
            try
            {
                // 為檔案路徑加上引號，以處理包含空格的路徑
                Process.Start("notepad.exe", $"\"{filePath}\"");
                Hide(); // 開啟後隱藏主視窗
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"無法開啟檔案 '{filePath}'。\n錯誤: {ex.Message}");
            }
        }




        public void InitHwnd()
        {
            var helper = new WindowInteropHelper(this);
            helper.EnsureHandle();
        }
        private void StartUp(bool start)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            string appName = curAssembly.GetName().Name;
            if (start)
            {
                key.SetValue(appName, $"\"{AppDir}\" --autostart");
            }
            else
            {
                if (key.GetValue(appName) != null)
                    key.DeleteValue(appName);
            }
        }
        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Url_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Controls.Button bt = (System.Windows.Controls.Button)sender;
                var dataContext = (KeyValue)bt.DataContext;
                string url = dataContext.Url;
                if (url != "")
                {
                    System.Diagnostics.Process.Start(url);
                    Hide();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("链接有问题，请重新编辑链接");
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Column_Width = ((int)this.ActualWidth - 40) / Column_count;
            Colum_Item_Width = Column_Width - 20;

            // 如果你有手動產生 UI，要在這裡重新排版
            // 或重設某些容器的寬度、Padding 等
        }


        private void Main_Deactivated(object sender, EventArgs e)
        {
            Hide();
        }
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            string frontApp = LastFrontWindowProcess;
            if (string.IsNullOrEmpty(frontApp)) return;
            string appName = Path.GetFileNameWithoutExtension(frontApp);
            string ahkPath = Path.Combine(BaseDir, "data", "tooltip.ahk");
            if (File.Exists(ahkPath))
            {
                System.Diagnostics.Process.Start(ahkPath, $"\"{appName}\"");
            }
            else
            {
                System.Windows.MessageBox.Show("找不到 tooltip.ahk");
            }
            Hide();
        }
        #endregion

        private void UpdateIconForSheet(string sheetName)
        {
            // 對 "全局" 或其他特殊名稱，可以設置一個預設圖示或清除圖示
            if (string.IsNullOrEmpty(sheetName) || sheetName.Equals("全局", StringComparison.OrdinalIgnoreCase))
            {
                // 可以考慮顯示 XiaoChao 自己的圖示作為預設
                // FrontAppIcon = new BitmapImage(new Uri("pack://application:,,,/YourIcon.ico"));
                FrontAppIcon = null; // 或者直接清除
                return;
            }

            // 嘗試從緩存中獲取路徑
            if (_sheetNameToExePathCache.TryGetValue(sheetName, out string exePath) && File.Exists(exePath))
            {
                try
                {
                    using (Icon appIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath))
                    {
                        FrontAppIcon = IconToImageSource(appIcon);
                    }
                }
                catch { FrontAppIcon = null; }
            }
            else
            {
                // 如果緩存中沒有，可以考慮一個備用策略，例如：
                // 1. 查找與 sheetName 同名的 exe 檔案 (如果用戶有良好習慣)
                // string potentialPath = Path.Combine(_local_directory, sheetName + ".exe");
                // 2. 或者，暫時清除圖示
                FrontAppIcon = null;
            }
        }






    }



}