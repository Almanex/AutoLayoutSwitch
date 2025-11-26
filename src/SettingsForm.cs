using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AutoLayoutSwitch
{
    public class SettingsForm : Form
    {
        private CheckBox _chkPlaySound;
        private CheckBox _chkAutoStart;
        private Label _lblHotkey;
        private Button _btnSetHotkey;
        private TextBox _txtExceptions;
        private Button _btnSave;
        private Settings _settings;
        
        private int _tempHotkeyVk;
        private uint _tempHotkeyMod;
        private bool _waitingForKey;

        public SettingsForm(Settings settings)
        {
            _settings = settings;
            _tempHotkeyVk = settings.HotKeyVk;
            _tempHotkeyMod = settings.HotKeyModifiers;

            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Настройки AutoLayoutSwitch";
            this.Size = new Size(500, 550);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(400, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            int y = 20;

            _chkPlaySound = new CheckBox();
            _chkPlaySound.Text = "Звук при переключении";
            _chkPlaySound.Location = new Point(20, y);
            _chkPlaySound.AutoSize = true;
            this.Controls.Add(_chkPlaySound);

            y += 30;

            _chkAutoStart = new CheckBox();
            _chkAutoStart.Text = "Запускать вместе с Windows";
            _chkAutoStart.Location = new Point(20, y);
            _chkAutoStart.AutoSize = true;
            this.Controls.Add(_chkAutoStart);

            y += 40;

            Label lblHkTitle = new Label();
            lblHkTitle.Text = "Горячая клавиша исправления:";
            lblHkTitle.Location = new Point(20, y);
            lblHkTitle.AutoSize = true;
            this.Controls.Add(lblHkTitle);

            y += 25;

            _lblHotkey = new Label();
            _lblHotkey.Text = "Shift + F12"; // Placeholder
            _lblHotkey.Location = new Point(20, y + 5);
            _lblHotkey.AutoSize = true;
            _lblHotkey.Font = new Font(this.Font, FontStyle.Bold);
            this.Controls.Add(_lblHotkey);

            _btnSetHotkey = new Button();
            _btnSetHotkey.Text = "Изменить...";
            _btnSetHotkey.Location = new Point(150, y);
            _btnSetHotkey.AutoSize = true;
            _btnSetHotkey.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnSetHotkey.Padding = new Padding(10, 5, 10, 5);
            _btnSetHotkey.Click += BtnSetHotkey_Click;
            this.Controls.Add(_btnSetHotkey);

            y += 40;

            Label lblEx = new Label();
            lblEx.Text = "Исключения (по одному в строке):";
            lblEx.Location = new Point(20, y);
            lblEx.AutoSize = true;
            this.Controls.Add(lblEx);

            y += 25;

            _txtExceptions = new TextBox();
            _txtExceptions.Multiline = true;
            _txtExceptions.ScrollBars = ScrollBars.Vertical;
            _txtExceptions.Location = new Point(20, y);
            _txtExceptions.Size = new Size(440, 200);
            _txtExceptions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(_txtExceptions);

            y += 220;

            _btnSave = new Button();
            _btnSave.Text = "Сохранить";
            _btnSave.AutoSize = true;
            _btnSave.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnSave.Padding = new Padding(20, 10, 20, 10);
            _btnSave.Location = new Point(this.ClientSize.Width - 150, y);
            _btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnSave.DialogResult = DialogResult.OK;
            _btnSave.Click += BtnSave_Click;
            this.Controls.Add(_btnSave);
            
            this.KeyPreview = true;
            this.KeyDown += SettingsForm_KeyDown;
        }

        private void LoadSettings()
        {
            _chkPlaySound.Checked = _settings.PlaySound;
            _chkAutoStart.Checked = _settings.AutoStart;
            _txtExceptions.Text = string.Join(Environment.NewLine, _settings.Exceptions);
            UpdateHotkeyLabel();
        }

        private void UpdateHotkeyLabel()
        {
            string mod = "";
            if ((_tempHotkeyMod & Win32.MOD_CONTROL) != 0) mod += "Ctrl + ";
            if ((_tempHotkeyMod & Win32.MOD_SHIFT) != 0) mod += "Shift + ";
            if ((_tempHotkeyMod & Win32.MOD_ALT) != 0) mod += "Alt + ";
            
            Keys key = (Keys)_tempHotkeyVk;
            _lblHotkey.Text = mod + key.ToString();
        }

        private void BtnSetHotkey_Click(object? sender, EventArgs e)
        {
            _btnSetHotkey.Text = "Нажмите клавиши...";
            _btnSetHotkey.Enabled = false;
            _waitingForKey = true;
            this.Focus();
        }

        private void SettingsForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_waitingForKey)
            {
                // Ignore modifier keys alone
                if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu)
                    return;

                _tempHotkeyVk = (int)e.KeyCode;
                _tempHotkeyMod = 0;
                if (e.Control) _tempHotkeyMod |= Win32.MOD_CONTROL;
                if (e.Shift) _tempHotkeyMod |= Win32.MOD_SHIFT;
                if (e.Alt) _tempHotkeyMod |= Win32.MOD_ALT;

                UpdateHotkeyLabel();
                _waitingForKey = false;
                _btnSetHotkey.Text = "Изменить...";
                _btnSetHotkey.Enabled = true;
                e.Handled = true;
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            _settings.PlaySound = _chkPlaySound.Checked;
            _settings.AutoStart = _chkAutoStart.Checked;
            _settings.HotKeyVk = _tempHotkeyVk;
            _settings.HotKeyModifiers = _tempHotkeyMod;

            _settings.Exceptions.Clear();
            var lines = _txtExceptions.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    _settings.Exceptions.Add(trimmed);
                }
            }

            _settings.Save();
            this.Close();
        }
    }
}
