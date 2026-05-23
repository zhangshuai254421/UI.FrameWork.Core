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
    /// 大型数字键盘
    /// </summary>
    public class BigNumericKeypad : Control
    {
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


        public static readonly RoutedEvent EnterKeyPressedEvent = EventManager.RegisterRoutedEvent("EnterKeyPressed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(BigNumericKeypad));

        // 提供公共事件访问器
        public event RoutedEventHandler EnterKeyPressed
        {
            add { AddHandler(EnterKeyPressedEvent, value); }
            remove { RemoveHandler(EnterKeyPressedEvent, value); }
        }
        /// <summary>
        /// 键盘输入
        /// </summary>
        /// <param name="bVK"></param>
        /// <param name="bScan"></param>
        /// <param name="dwFlags"></param>
        /// <param name="dwExtraInfo"></param>
        [DllImport("User32.dll")]
        public static extern void keybd_event(byte bVK, byte bScan, Int32 dwFlags, int dwExtraInfo);

        private Grid _grid;
        private ToggleButton _shift;

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
            if (Console.CapsLock)
            {
                _shift.IsChecked = true;
                PlusShift(_shift, _grid);
            }
            else
            {
                _shift.IsChecked = false;
                PlusShift(_shift, _grid);
            }
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
                if (btn == null)
                {
                    return;
                }
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
                if (btn == null)
                {
                    return;
                }
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
