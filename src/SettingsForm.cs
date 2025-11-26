using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AutoLayoutSwitch
{
    public class SettingsForm : Form
    {
        private CheckBox _chkPlaySound = null!;
        private CheckBox _chkAutoStart = null!;
        
        private TextBox _txtExceptions = null!;
        private TextBox _txtRuShort = null!;
        private TextBox _txtRuPrefixes = null!;
        private Button _btnSave = null!;
        private Settings _settings;
        
        

        public SettingsForm(Settings settings)
        {
            _settings = settings;
            

            InitializeComponent();
            LoadSettings();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var icon = Program.GetWindowIcon();
            if (icon != null)
            {
                this.Icon = icon;
            }
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
            this.ShowIcon = true;
            this.Icon = Program.GetWindowIcon();
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

            

            var lblEx = new Label();
            lblEx.Text = "Исключения (по одному в строке):";
            lblEx.AutoSize = true;
            root.Controls.Add(lblEx, 0, 4);

            _txtExceptions = new TextBox();
            _txtExceptions.Multiline = true;
            _txtExceptions.ScrollBars = ScrollBars.Vertical;
            _txtExceptions.Dock = DockStyle.Fill;
            root.Controls.Add(_txtExceptions, 0, 5);

            var ruPanel = new TableLayoutPanel();
            ruPanel.ColumnCount = 2;
            ruPanel.RowCount = 2;
            ruPanel.Dock = DockStyle.Fill;
            ruPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            ruPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            ruPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            ruPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblRuShort = new Label();
            lblRuShort.Text = "Короткие RU слова:";
            lblRuShort.AutoSize = true;
            ruPanel.Controls.Add(lblRuShort, 0, 0);

            var lblRuPrefixes = new Label();
            lblRuPrefixes.Text = "RU префиксы:";
            lblRuPrefixes.AutoSize = true;
            ruPanel.Controls.Add(lblRuPrefixes, 1, 0);

            _txtRuShort = new TextBox();
            _txtRuShort.Multiline = true;
            _txtRuShort.ScrollBars = ScrollBars.Vertical;
            _txtRuShort.Dock = DockStyle.Fill;
            ruPanel.Controls.Add(_txtRuShort, 0, 1);

            _txtRuPrefixes = new TextBox();
            _txtRuPrefixes.Multiline = true;
            _txtRuPrefixes.ScrollBars = ScrollBars.Vertical;
            _txtRuPrefixes.Dock = DockStyle.Fill;
            ruPanel.Controls.Add(_txtRuPrefixes, 1, 1);

            root.Controls.Add(ruPanel, 0, 3);

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

            root.Controls.Add(bottomRow, 0, 7);
            this.Controls.Add(root);

            
        }

        private void LoadSettings()
        {
            _chkPlaySound.Checked = _settings.PlaySound;
            _chkAutoStart.Checked = _settings.AutoStart;
            _txtExceptions.Text = string.Join(Environment.NewLine, _settings.Exceptions);
            _txtRuShort.Text = string.Join(Environment.NewLine, _settings.RuShortWhitelist);
            _txtRuPrefixes.Text = string.Join(Environment.NewLine, _settings.RuPrefixes);
        }

        

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            _settings.PlaySound = _chkPlaySound.Checked;
            _settings.AutoStart = _chkAutoStart.Checked;
            

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

            _settings.RuShortWhitelist.Clear();
            var shortLines = _txtRuShort.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in shortLines)
            {
                var t = line.Trim();
                if (!string.IsNullOrEmpty(t)) _settings.RuShortWhitelist.Add(t);
            }

            _settings.RuPrefixes.Clear();
            var prefLines = _txtRuPrefixes.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in prefLines)
            {
                var t = line.Trim();
                if (!string.IsNullOrEmpty(t)) _settings.RuPrefixes.Add(t);
            }

            _settings.Save();
            this.Close();
        }
    }
}
