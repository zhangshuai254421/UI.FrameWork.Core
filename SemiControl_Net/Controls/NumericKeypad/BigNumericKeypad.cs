using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SemiControl.Controls
{
    /// <summary>
    /// 大型数字键盘：模板内置数字/方向/退格等按键，点击后经 Win32 <see cref="keybd_event"/>
    /// 注入系统级键盘事件（等效真实按键），大小写随 Shift/大写锁定切换；按 Enter 时引发 <see cref="EnterKeyPressed"/>。
    /// </summary>
    public class BigNumericKeypad : Control
    {
        /// <summary>键名 → Win32 虚拟键码映射（Tag 值即此处的键名）。</summary>
        private static Dictionary<string, byte> keycode = new Dictionary<string, byte>()
        {
            {"BackSpace", 8 },
            {"Tab", 9 },
            {"Ctrl", 17 },
            {"Alt", 18 },
            {"Shift", 16 },
            {"CapsLock", 20 },
            {"Space", 32 },
            {"LeftArrow", 37 },
            {"UpArrow", 38 },
            {"RightArrow", 39 },
            {"DwArrow", 40 },
            {"Del", 46 },
            {"0", 48 },
            {"1", 49 },
            {"2", 50 },
            {"3", 51 },
            {"4", 52 },
            {"5", 53 },
            {"6", 54 },
            {"7", 55 },
            {"8", 56 },
            {"9", 57 },
            {"A", 65 },
            {"B", 66 },
            {"C", 67 },
            {"D", 68 },
            {"E", 69 },
            {"F", 70 },
            {"G", 71 },
            {"H", 72 },
            {"I", 73 },
            {"J", 74 },
            {"K", 75 },
            {"L", 76 },
            {"M", 77 },
            {"N", 78 },
            {"O", 79 },
            {"P", 80 },
            {"Q", 81 },
            {"R", 82 },
            {"S", 83 },
            {"T", 84 },
            {"U", 85 },
            {"V", 86 },
            {"W", 87 },
            {"X", 88 },
            {"Y", 89 },
            {"Z", 90 },
            {"Enter", 13 },
            {";", 186 },
            {",", 188 },
            {"=", 187 },
            {"-", 189 },
            {".", 190 },
            {"/", 191 },
            {"`", 192 },
            {"[", 219 },
            {"\\", 220 },
            {"]", 221 },
            {"'", 222 },

        };


        /// <summary>EnterKeyPressed 路由事件（冒泡）。</summary>
        public static readonly RoutedEvent EnterKeyPressedEvent = EventManager.RegisterRoutedEvent("EnterKeyPressed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(BigNumericKeypad));

        /// <summary>按下 Enter 键时引发。</summary>
        public event RoutedEventHandler EnterKeyPressed
        {
            add { AddHandler(EnterKeyPressedEvent, value); }
            remove { RemoveHandler(EnterKeyPressedEvent, value); }
        }

        /// <summary>
        /// Win32 keybd_event：向系统注入一次键盘事件（0=按下，2=抬起），实现等效真实按键输入。
        /// </summary>
        /// <param name="bVK">虚拟键码，见 <see cref="keycode"/>。</param>
        /// <param name="bScan">硬件扫描码（此处固定传 0）。</param>
        /// <param name="dwFlags">标志位：0 按下 / KEYEVENTF_KEYUP(2) 抬起。</param>
        /// <param name="dwExtraInfo">附加信息（此处固定传 0）。</param>
        [DllImport("User32.dll")]
        public static extern void keybd_event(byte bVK, byte bScan, Int32 dwFlags, int dwExtraInfo);

        private Grid _grid;
        private ToggleButton _shift;

        /// <summary>文本内容（预留属性：当前版本按键经 Win32 直接注入系统键盘流，不经过此属性）。</summary>
        public string TextContent { get; set; }

        public override void OnApplyTemplate()
        {
            _grid = (Grid)GetTemplateChild("G_Grid");
            _shift = (ToggleButton)GetTemplateChild("T_Shift");

            _grid.Loaded -= NumericKeypadLoad;
            _grid.Loaded += NumericKeypadLoad;

            GetGridChild(_grid);
        }

        private void NumericKeypadLoad(object sender, RoutedEventArgs e)
        {
            // 按系统大写锁定状态初始化 Shift 键与按键大小写显示。
            _shift.IsChecked = Console.CapsLock;
            PlusShift(_shift, _grid);
        }

        private void GetGridChild(Panel panel)
        {
            if (panel == null)
                return;
            foreach (var button in panel.Children)
            {
                if (button is Button)
                {
                    ((Button)button).Click -= Bt_Click;
                    ((Button)button).Click += Bt_Click;
                }
                if (button is ToggleButton)
                {
                    ((ToggleButton)button).Click -= ToggleBt_Click;
                    ((ToggleButton)button).Click += ToggleBt_Click;
                }
                else if (button is Panel childPanel)
                {
                    GetGridChild(childPanel);
                }
            }
        }

        private void Bt_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string content = btn.Tag?.ToString();
                if (string.IsNullOrEmpty(content))
                {
                    return;
                }
                if (content == "BackTab")
                {
                    keybd_event(keycode["Shift"], 0, 0, 0);
                    keybd_event(keycode["Tab"], 0, 0, 0);
                    keybd_event(keycode["Tab"], 0, 2, 0);
                    keybd_event(keycode["Shift"], 0, 2, 0);
                    return;
                }
                if (content == "+")
                {
                    keybd_event(keycode["Shift"], 0, 0, 0);
                    keybd_event(keycode["="], 0, 0, 0);
                    keybd_event(keycode["="], 0, 2, 0);
                    keybd_event(keycode["Shift"], 0, 2, 0);
                    return;
                }
                keybd_event(keycode[content], 0, 0, 0);
                keybd_event(keycode[content], 0, 2, 0);
            }
        }

        private void ToggleBt_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is ToggleButton btn)
            {
                string content = btn.Tag?.ToString();
                if (string.IsNullOrEmpty(content))
                {
                    return;
                }

                if (content == "Shift")
                {
                    keybd_event(keycode["CapsLock"], 0, 0, 0);
                    keybd_event(keycode["CapsLock"], 0, 2, 0);

                    PlusShift(btn, _grid);

                    return;
                }
            }
        }

        /// <summary>按 Shift 键状态把面板上所有按键内容整体切换大小写（"+/-" 键同时切换 Tag 正负）。</summary>
        private void PlusShift(ToggleButton btn, Panel panel)
        {
            foreach (var children in panel.Children)
            {
                if (children is Button button && button.Content != null)
                {
                    if (btn.IsChecked.Value)
                    {
                        if (button.Content.ToString() == "+/-")
                        {
                            button.Tag = "-";
                        }

                        button.Content = button.Content.ToString().ToUpper();
                    }
                    else
                    {
                        if (button.Content.ToString() == "+/-")
                        {
                            button.Tag = "+";
                        }

                        button.Content = button.Content.ToString().ToLower();
                    }
                }
                else if (children is Panel childPanel)
                {
                    PlusShift(btn, childPanel);
                }

            }
        }
    }
}
