using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace xiaochao
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        public ConfigManager Config { get; set; } = ConfigManager.GetInstance();
        public SettingWindow()
        {
            InitializeComponent();
            //config = ConfigManager.GetInstance();
            this.DataContext = this;
        }

        private void ThemeSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selected = (ThemeSelector.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();

            if (selected == "番茄")
            {
                Config.Font_color = "#212121";
                Config.Decoration_color = "#f98981";
                Config.Background_opacity = 0.9;
                Config.Background_color = "#fff3e8";

            }
            else if (selected == "白色")
            {
                Config.Font_color = "#212121";
                Config.Decoration_color = "#ecf0f1";
                Config.Background_opacity = 0.9;
                Config.Background_color = "#ffffff";
            }
            else if (selected == "黑色")
            {
                Config.Font_color = "#ffffff";
                Config.Decoration_color = "#1e1e1e";
                Config.Background_opacity = 1;
                Config.Background_color = "#525252";
            }
            else if (selected == "灰色")
            {
                Config.Font_color = "#000000";
                Config.Decoration_color = "#ecf0f1";
                Config.Background_opacity = 1;
                Config.Background_color = "#e5e5e5";
            }
            else if (selected == "自訂")
            {

            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            // ✅ 在這裡呼叫儲存設定邏輯（寫入设置.md）
            // 可由 ConfigManager 實作 SaveConfig()
            MessageBox.Show("已套用佈景主題（實際儲存邏輯待實作）");
            Config.SaveConfig();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }







    }
}
