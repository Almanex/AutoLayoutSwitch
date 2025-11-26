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
            this.Size = new Size(700, 600);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(520, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowIcon = false;
            this.AutoScaleMode = AutoScaleMode.Dpi;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.ColumnCount = 1;
            root.RowCount = 7;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _chkPlaySound = new CheckBox();
            _chkPlaySound.Text = "Звук при переключении";
            _chkPlaySound.AutoSize = true;
            root.Controls.Add(_chkPlaySound, 0, 0);

            _chkAutoStart = new CheckBox();
            _chkAutoStart.Text = "Запускать вместе с Windows";
            _chkAutoStart.AutoSize = true;
            root.Controls.Add(_chkAutoStart, 0, 1);

            var lblHkTitle = new Label();
            lblHkTitle.Text = "Горячая клавиша исправления:";
            lblHkTitle.AutoSize = true;
            root.Controls.Add(lblHkTitle, 0, 2);

            var hotkeyRow = new FlowLayoutPanel();
            hotkeyRow.AutoSize = true;
            hotkeyRow.FlowDirection = FlowDirection.LeftToRight;
            hotkeyRow.WrapContents = false;
            hotkeyRow.Margin = new Padding(0, 4, 0, 8);

            _lblHotkey = new Label();
            _lblHotkey.Text = "Shift + F12";
            _lblHotkey.AutoSize = true;
            _lblHotkey.Font = new Font(this.Font, FontStyle.Bold);
            _lblHotkey.Margin = new Padding(0, 6, 12, 6);
            hotkeyRow.Controls.Add(_lblHotkey);

            _btnSetHotkey = new Button();
            _btnSetHotkey.Text = "Изменить...";
            _btnSetHotkey.AutoSize = true;
            _btnSetHotkey.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnSetHotkey.Padding = new Padding(10, 6, 10, 6);
            _btnSetHotkey.Click += BtnSetHotkey_Click;
            hotkeyRow.Controls.Add(_btnSetHotkey);

            root.Controls.Add(hotkeyRow, 0, 3);

            var lblEx = new Label();
            lblEx.Text = "Исключения (по одному в строке):";
            lblEx.AutoSize = true;
            root.Controls.Add(lblEx, 0, 4);

            _txtExceptions = new TextBox();
            _txtExceptions.Multiline = true;
            _txtExceptions.ScrollBars = ScrollBars.Vertical;
            _txtExceptions.Dock = DockStyle.Fill;
            root.Controls.Add(_txtExceptions, 0, 5);

            var bottomRow = new FlowLayoutPanel();
            bottomRow.FlowDirection = FlowDirection.RightToLeft;
            bottomRow.Dock = DockStyle.Fill;
            bottomRow.Padding = new Padding(0);
            bottomRow.Margin = new Padding(0, 12, 0, 0);

            _btnSave = new Button();
            _btnSave.Text = "Сохранить";
            _btnSave.AutoSize = true;
            _btnSave.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnSave.Padding = new Padding(20, 10, 20, 10);
            _btnSave.DialogResult = DialogResult.OK;
            _btnSave.Click += BtnSave_Click;
            bottomRow.Controls.Add(_btnSave);

            root.Controls.Add(bottomRow, 0, 6);
            this.Controls.Add(root);

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
