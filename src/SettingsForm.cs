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
            Text = "Настройки AutoLayoutSwitch";
            Size = new Size(520, 560);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(420, 480);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            AutoScaleMode = AutoScaleMode.Dpi;
            Padding = new Padding(12);

            var tlp = new TableLayoutPanel();
            tlp.ColumnCount = 2;
            tlp.RowCount = 6;
            tlp.Dock = DockStyle.Fill;
            tlp.AutoSize = false;
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _chkPlaySound = new CheckBox();
            _chkPlaySound.Text = "Звук при переключении";
            _chkPlaySound.AutoSize = true;
            _chkPlaySound.Margin = new Padding(4);
            tlp.Controls.Add(_chkPlaySound, 0, 0);
            tlp.SetColumnSpan(_chkPlaySound, 2);

            _chkAutoStart = new CheckBox();
            _chkAutoStart.Text = "Запускать вместе с Windows";
            _chkAutoStart.AutoSize = true;
            _chkAutoStart.Margin = new Padding(4);
            tlp.Controls.Add(_chkAutoStart, 0, 1);
            tlp.SetColumnSpan(_chkAutoStart, 2);

            var lblHkTitle = new Label();
            lblHkTitle.Text = "Горячая клавиша исправления:";
            lblHkTitle.AutoSize = true;
            lblHkTitle.Margin = new Padding(4, 8, 4, 4);
            tlp.Controls.Add(lblHkTitle, 0, 2);

            var hotkeyPanel = new FlowLayoutPanel();
            hotkeyPanel.FlowDirection = FlowDirection.RightToLeft;
            hotkeyPanel.Dock = DockStyle.Fill;
            hotkeyPanel.AutoSize = true;

            _btnSetHotkey = new Button();
            _btnSetHotkey.Text = "Изменить...";
            _btnSetHotkey.AutoSize = true;
            _btnSetHotkey.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnSetHotkey.Padding = new Padding(10, 6, 10, 6);
            _btnSetHotkey.Margin = new Padding(4);
            _btnSetHotkey.Click += BtnSetHotkey_Click;

            _lblHotkey = new Label();
            _lblHotkey.Text = "Shift + F12";
            _lblHotkey.AutoSize = true;
            _lblHotkey.Font = new Font(Font, FontStyle.Bold);
            _lblHotkey.Margin = new Padding(4, 10, 8, 0);

            hotkeyPanel.Controls.Add(_btnSetHotkey);
            hotkeyPanel.Controls.Add(_lblHotkey);
            tlp.Controls.Add(hotkeyPanel, 1, 2);

            var lblEx = new Label();
            lblEx.Text = "Исключения (по одному в строке):";
            lblEx.AutoSize = true;
            lblEx.Margin = new Padding(4, 12, 4, 4);
            tlp.Controls.Add(lblEx, 0, 3);
            tlp.SetColumnSpan(lblEx, 2);

            _txtExceptions = new TextBox();
            _txtExceptions.Multiline = true;
            _txtExceptions.ScrollBars = ScrollBars.Vertical;
            _txtExceptions.Dock = DockStyle.Fill;
            _txtExceptions.Margin = new Padding(4);
            tlp.Controls.Add(_txtExceptions, 0, 4);
            tlp.SetColumnSpan(_txtExceptions, 2);

            var buttonsPanel = new FlowLayoutPanel();
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Dock = DockStyle.Fill;
            buttonsPanel.AutoSize = true;
            buttonsPanel.Margin = new Padding(4);

            _btnSave = new Button();
            _btnSave.Text = "Сохранить";
            _btnSave.AutoSize = true;
            _btnSave.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnSave.Padding = new Padding(20, 10, 20, 10);
            _btnSave.DialogResult = DialogResult.OK;
            _btnSave.Click += BtnSave_Click;

            buttonsPanel.Controls.Add(_btnSave);
            tlp.Controls.Add(buttonsPanel, 0, 5);
            tlp.SetColumnSpan(buttonsPanel, 2);

            Controls.Add(tlp);
            AcceptButton = _btnSave;
            KeyPreview = true;
            KeyDown += SettingsForm_KeyDown;
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
