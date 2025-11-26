using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AutoLayoutSwitch
{
    static class Program
    {
        static LayoutWatcher? _watcher;
        static KeyboardHook? _hook;
        static Settings? _settings;
        static IntPtr _hWnd;
        static Win32.NOTIFYICONDATA _nid;
        
        const int WM_TRAYICON = Win32.WM_USER + 1;
        const int WM_COMMAND = 0x0111;
        const int WM_DESTROY = 0x0002;
        const int WM_HOTKEY = 0x0312;
        
        const int ID_TRAYICON = 1001;
        const int IDM_EXIT = 1002;
        const int IDM_TOGGLE = 1003;
        const int IDM_SETTINGS = 1005;
        const int ID_HOTKEY = 1004;
        
        const int MF_CHECKED = 0x0008;
        const int MF_UNCHECKED = 0x0000;
        const int MF_STRING = 0x0000;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (Mutex mutex = new Mutex(false, "Global\\AutoLayoutSwitch_Mutex"))
            {
                if (!mutex.WaitOne(0, false))
                {
                    return; // Приложение уже запущено
                }

                _settings = Settings.Load();
                ManageAutoStart(_settings.AutoStart);

                _watcher = new LayoutWatcher(_settings);
                _hook = new KeyboardHook(_watcher.OnKeyPress);

                CreateMessageWindow();
                CreateTrayIcon();
                
                // Register Hotkey from settings (Default: Shift + F12)
                Win32.RegisterHotKey(_hWnd, ID_HOTKEY, _settings.HotKeyModifiers, (uint)_settings.HotKeyVk);

                RunMessageLoop();

                // Очистка ресурсов при выходе
                Win32.UnregisterHotKey(_hWnd, ID_HOTKEY);
                Win32.Shell_NotifyIcon(Win32.NIM_DELETE, ref _nid);
                _hook.Dispose();
            }
        }

        static void ManageAutoStart(bool enable)
        {
            try
            {
                string keyName = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
                using (Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyName, true))
                {
                    if (key == null) return;
                    
                    string appName = "AutoLayoutSwitch";
                    if (enable)
                    {
                        string? exePath = Environment.ProcessPath;
                        if (exePath != null)
                            key.SetValue(appName, $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(appName, false);
                    }
                }
            }
            catch { }
        }

        static void CreateMessageWindow()
        {
            Win32.WNDCLASS wc = new Win32.WNDCLASS();
            wc.lpfnWndProc = WndProc;
            wc.lpszClassName = "AutoLayoutSwitch_Class";
            wc.hInstance = Win32.GetModuleHandle(null);
            
            Win32.RegisterClass(ref wc);
            
            _hWnd = Win32.CreateWindowEx(0, "AutoLayoutSwitch_Class", "AutoLayoutSwitch", 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, wc.hInstance, IntPtr.Zero);
        }

        static void CreateTrayIcon()
        {
            _nid = new Win32.NOTIFYICONDATA();
            _nid.cbSize = Marshal.SizeOf(_nid);
            _nid.hWnd = _hWnd;
            _nid.uID = ID_TRAYICON;
            _nid.uFlags = Win32.NIF_ICON | Win32.NIF_TIP | Win32.NIF_MESSAGE;
            _nid.uCallbackMessage = WM_TRAYICON;
            _nid.szTip = "AutoLayoutSwitch";

            // Try load icon from file
            IntPtr hIcon = Win32.LoadImage(IntPtr.Zero, "icon.ico", Win32.IMAGE_ICON, 16, 16, Win32.LR_LOADFROMFILE);
            if (hIcon == IntPtr.Zero)
            {
                // Fallback to system application icon
                 hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512); // IDI_APPLICATION
            }
            _nid.hIcon = hIcon;

            Win32.Shell_NotifyIcon(Win32.NIM_ADD, ref _nid);
        }

        static void RunMessageLoop()
        {
            Win32.MSG msg;
            while (Win32.GetMessage(out msg, IntPtr.Zero, 0, 0) > 0)
            {
                Win32.TranslateMessage(ref msg);
                Win32.DispatchMessage(ref msg);
            }
        }

        static IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_TRAYICON:
                    if ((int)lParam == Win32.WM_RBUTTONUP)
                    {
                        ShowContextMenu();
                    }
                    break;
                
                case WM_HOTKEY:
                    if (wParam == (IntPtr)ID_HOTKEY)
                    {
                        _watcher?.ManualSwitch();
                    }
                    break;

                case WM_COMMAND:
                    int id = (int)wParam & 0xFFFF;
                    if (id == IDM_EXIT)
                    {
                        Win32.PostQuitMessage(0);
                    }
                    else if (id == IDM_TOGGLE)
                    {
                        _watcher!.Enabled = !_watcher.Enabled;
                    }
                    else if (id == IDM_SETTINGS)
                    {
                        OpenSettings();
                    }
                    break;

                case WM_DESTROY:
                    Win32.PostQuitMessage(0);
                    break;
            }
            return Win32.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        static void ShowContextMenu()
        {
            IntPtr hMenu = Win32.CreatePopupMenu();
            
            int flags = MF_STRING;
            if (_watcher!.Enabled) flags |= MF_CHECKED;
            
            Win32.AppendMenu(hMenu, flags, IDM_TOGGLE, "Включить авто-переключение");
            Win32.AppendMenu(hMenu, MF_STRING, IDM_SETTINGS, "Настройки");
            Win32.AppendMenu(hMenu, MF_STRING, IDM_EXIT, "Выход");

            Win32.POINT pt;
            Win32.GetCursorPos(out pt);
            
            SetForegroundWindow(_hWnd);
            Win32.TrackPopupMenu(hMenu, Win32.TPM_RIGHTBUTTON, pt.X, pt.Y, 0, _hWnd, IntPtr.Zero);
            Win32.DestroyMenu(hMenu);
        }

        static void OpenSettings()
        {
            // Unregister hotkey while settings are open to avoid conflicts
            Win32.UnregisterHotKey(_hWnd, ID_HOTKEY);

            using (var form = new SettingsForm(_settings!))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Settings are saved inside the form
                    // Reload/Apply effects
                    ManageAutoStart(_settings!.AutoStart);
                    
                    // Re-register hotkey
                    Win32.RegisterHotKey(_hWnd, ID_HOTKEY, _settings.HotKeyModifiers, (uint)_settings.HotKeyVk);
                }
                else
                {
                    // If canceled, just re-register old hotkey
                    Win32.RegisterHotKey(_hWnd, ID_HOTKEY, _settings.HotKeyModifiers, (uint)_settings.HotKeyVk);
                }
            }
        }
    }
}
