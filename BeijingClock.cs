using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace BeijingClock
{
    public partial class MainForm : Form
    {
        // 窗口置顶和圆角
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

        [DllImport("Gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        // 变量
        private Timer timer;
        private Timer syncTimer;
        private DateTime currentTime;
        private bool isTopMost = false;
        private bool autoSync = true;
        private bool alertEnabled = false;
        private int alertSeconds = 5;
        private int alertDuration = 3;
        private bool lastAlertTriggered = false;
        private bool isAlerting = false;
        
        // UI 控件
        private Label timeLabel;
        private Label dateLabel;
        private Label statusLabel;
        private Label timezoneLabel;
        private Button manualSyncBtn;
        private Button settingsBtn;
        private FlowLayoutPanel mainPanel;
        private Panel timePanel;
        
        // 设置面板
        private Panel settingsPanel;
        private bool settingsVisible = false;
        private CheckBox topCheck;
        private CheckBox autoSyncCheck;
        private CheckBox alertCheck;
        private NumericUpDown alertSecondsNum;
        private NumericUpDown alertDurationNum;

        public MainForm()
        {
            currentTime = DateTime.Now;
            InitializeComponent();
            ApplyRoundedCorners();
            StartTimers();
            StartAutoSync();
        }

        private void ApplyRoundedCorners()
        {
            IntPtr hRgn = CreateRoundRectRgn(0, 0, this.Width, this.Height, 20, 20);
            this.Region = Region.FromHrgn(hRgn);
        }

        private void InitializeComponent()
        {
            this.Text = "北京时间同步器";
            this.Size = new Size(520, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(10, 15, 30);
            this.Icon = null;
            this.DoubleBuffered = true;

            // 主面板
            mainPanel = new FlowLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.FlowDirection = FlowDirection.TopDown;
            mainPanel.WrapContents = false;
            mainPanel.AutoScroll = false;
            mainPanel.BackColor = Color.FromArgb(10, 15, 30);
            mainPanel.Padding = new Padding(20, 20, 20, 20);

            // 时间显示面板
            timePanel = new Panel();
            timePanel.Height = 160;
            timePanel.Width = 480;
            timePanel.BackColor = Color.FromArgb(18, 25, 45);
            timePanel.Margin = new Padding(0, 0, 0, 15);
            
            timePanel.Paint += OnTimePanelPaint;

            // 时间标签
            timeLabel = new Label();
            timeLabel.Font = new Font("Segoe UI", 56, FontStyle.Bold);
            timeLabel.ForeColor = Color.FromArgb(0, 230, 255);
            timeLabel.Text = "00:00:00.000";
            timeLabel.AutoSize = false;
            timeLabel.Size = new Size(480, 90);
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            timeLabel.Location = new Point(0, 20);
            timeLabel.BackColor = Color.Transparent;

            // 日期标签
            dateLabel = new Label();
            dateLabel.Font = new Font("Segoe UI", 12, FontStyle.Regular);
            dateLabel.ForeColor = Color.FromArgb(180, 190, 220);
            dateLabel.Text = "2024年01月01日 星期一";
            dateLabel.AutoSize = false;
            dateLabel.Size = new Size(480, 28);
            dateLabel.TextAlign = ContentAlignment.MiddleCenter;
            dateLabel.Location = new Point(0, 90);
            dateLabel.BackColor = Color.Transparent;

            // 时区标签
            timezoneLabel = new Label();
            timezoneLabel.Font = new Font("Segoe UI", 9, FontStyle.Italic);
            timezoneLabel.ForeColor = Color.FromArgb(100, 150, 200);
            timezoneLabel.Text = "★ Asia/Shanghai (UTC+8) ★";
            timezoneLabel.AutoSize = false;
            timezoneLabel.Size = new Size(480, 22);
            timezoneLabel.TextAlign = ContentAlignment.MiddleCenter;
            timezoneLabel.Location = new Point(0, 122);
            timezoneLabel.BackColor = Color.Transparent;

            timePanel.Controls.AddRange(new Control[] { timeLabel, dateLabel, timezoneLabel });

            // 按钮面板
            Panel buttonPanel = new Panel();
            buttonPanel.Height = 50;
            buttonPanel.Width = 480;
            buttonPanel.BackColor = Color.Transparent;
            buttonPanel.Margin = new Padding(0, 0, 0, 12);

            // 手动同步按钮
            manualSyncBtn = new Button();
            manualSyncBtn.Text = "🔄 手动同步";
            manualSyncBtn.Size = new Size(140, 40);
            manualSyncBtn.Location = new Point(60, 5);
            manualSyncBtn.FlatStyle = FlatStyle.Flat;
            manualSyncBtn.BackColor = Color.FromArgb(0, 120, 212);
            manualSyncBtn.ForeColor = Color.White;
            manualSyncBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            manualSyncBtn.Cursor = Cursors.Hand;
            manualSyncBtn.FlatAppearance.BorderSize = 0;
            manualSyncBtn.Paint += OnManualSyncBtnPaint;
            manualSyncBtn.Click += ManualSyncBtn_Click;

            // 设置按钮
            settingsBtn = new Button();
            settingsBtn.Text = "⚙️ 设置";
            settingsBtn.Size = new Size(140, 40);
            settingsBtn.Location = new Point(280, 5);
            settingsBtn.FlatStyle = FlatStyle.Flat;
            settingsBtn.BackColor = Color.FromArgb(60, 70, 95);
            settingsBtn.ForeColor = Color.White;
            settingsBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            settingsBtn.Cursor = Cursors.Hand;
            settingsBtn.FlatAppearance.BorderSize = 0;
            settingsBtn.Paint += OnSettingsBtnPaint;
            settingsBtn.Click += SettingsBtn_Click;

            buttonPanel.Controls.AddRange(new Control[] { manualSyncBtn, settingsBtn });

            // 设置面板（初始隐藏）
            settingsPanel = new Panel();
            settingsPanel.Height = 0;
            settingsPanel.Width = 480;
            settingsPanel.BackColor = Color.FromArgb(20, 30, 55);
            settingsPanel.Margin = new Padding(0, 0, 0, 12);
            settingsPanel.Visible = false;
            settingsPanel.Paint += OnSettingsPanelPaint;

            // 置顶复选框
            topCheck = new CheckBox();
            topCheck.Text = " 窗口置顶";
            topCheck.Location = new Point(20, 15);
            topCheck.AutoSize = true;
            topCheck.ForeColor = Color.FromArgb(200, 210, 240);
            topCheck.BackColor = Color.Transparent;
            topCheck.Font = new Font("Segoe UI", 10);
            topCheck.CheckedChanged += TopCheck_CheckedChanged;

            // 自动同步复选框
            autoSyncCheck = new CheckBox();
            autoSyncCheck.Text = " 自动同步时间";
            autoSyncCheck.Location = new Point(20, 48);
            autoSyncCheck.AutoSize = true;
            autoSyncCheck.ForeColor = Color.FromArgb(200, 210, 240);
            autoSyncCheck.BackColor = Color.Transparent;
            autoSyncCheck.Checked = true;
            autoSyncCheck.Font = new Font("Segoe UI", 10);
            autoSyncCheck.CheckedChanged += AutoSyncCheck_CheckedChanged;

            // 声音提醒复选框
            alertCheck = new CheckBox();
            alertCheck.Text = " 启用整点提醒";
            alertCheck.Location = new Point(20, 81);
            alertCheck.AutoSize = true;
            alertCheck.ForeColor = Color.FromArgb(200, 210, 240);
            alertCheck.BackColor = Color.Transparent;
            alertCheck.Font = new Font("Segoe UI", 10);
            alertCheck.CheckedChanged += AlertCheck_CheckedChanged;

            // 提前秒数
            Label preLabel = new Label();
            preLabel.Text = "提前";
            preLabel.Location = new Point(40, 115);
            preLabel.AutoSize = true;
            preLabel.ForeColor = Color.FromArgb(150, 170, 210);
            preLabel.Font = new Font("Segoe UI", 9);
            
            alertSecondsNum = new NumericUpDown();
            alertSecondsNum.Location = new Point(85, 112);
            alertSecondsNum.Size = new Size(55, 25);
            alertSecondsNum.Minimum = 1;
            alertSecondsNum.Maximum = 30;
            alertSecondsNum.Value = 5;
            alertSecondsNum.BackColor = Color.FromArgb(30, 40, 65);
            alertSecondsNum.ForeColor = Color.FromArgb(0, 230, 255);
            alertSecondsNum.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            
            Label secLabel1 = new Label();
            secLabel1.Text = "秒时提醒";
            secLabel1.Location = new Point(150, 115);
            secLabel1.AutoSize = true;
            secLabel1.ForeColor = Color.FromArgb(150, 170, 210);
            secLabel1.Font = new Font("Segoe UI", 9);

            // 持续秒数
            Label durLabel = new Label();
            durLabel.Text = "持续";
            durLabel.Location = new Point(260, 115);
            durLabel.AutoSize = true;
            durLabel.ForeColor = Color.FromArgb(150, 170, 210);
            durLabel.Font = new Font("Segoe UI", 9);
            
            alertDurationNum = new NumericUpDown();
            alertDurationNum.Location = new Point(305, 112);
            alertDurationNum.Size = new Size(55, 25);
            alertDurationNum.Minimum = 1;
            alertDurationNum.Maximum = 10;
            alertDurationNum.Value = 3;
            alertDurationNum.BackColor = Color.FromArgb(30, 40, 65);
            alertDurationNum.ForeColor = Color.FromArgb(0, 230, 255);
            alertDurationNum.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            
            Label secLabel2 = new Label();
            secLabel2.Text = "秒";
            secLabel2.Location = new Point(370, 115);
            secLabel2.AutoSize = true;
            secLabel2.ForeColor = Color.FromArgb(150, 170, 210);
            secLabel2.Font = new Font("Segoe UI", 9);

            settingsPanel.Controls.AddRange(new Control[] { topCheck, autoSyncCheck, alertCheck, preLabel, alertSecondsNum, secLabel1, durLabel, alertDurationNum, secLabel2 });

            // 状态栏
            statusLabel = new Label();
            statusLabel.Text = "✅ 状态: 运行中";
            statusLabel.ForeColor = Color.FromArgb(0, 230, 150);
            statusLabel.Font = new Font("Segoe UI", 9);
            statusLabel.AutoSize = false;
            statusLabel.Size = new Size(480, 28);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Margin = new Padding(0, 5, 0, 0);

            mainPanel.Controls.AddRange(new Control[] { timePanel, buttonPanel, settingsPanel, statusLabel });
            this.Controls.Add(mainPanel);
        }

        private void OnTimePanelPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            using (LinearGradientBrush brush = new LinearGradientBrush(timePanel.ClientRectangle, 
                Color.FromArgb(18, 25, 45), Color.FromArgb(25, 35, 60), 90f))
            {
                g.FillRectangle(brush, timePanel.ClientRectangle);
            }
            
            using (Pen pen = new Pen(Color.FromArgb(80, 100, 255, 200), 2))
            {
                pen.Alignment = PenAlignment.Inset;
                g.DrawRectangle(pen, 1, 1, timePanel.Width - 3, timePanel.Height - 3);
            }
        }

        private void OnManualSyncBtnPaint(object sender, PaintEventArgs e)
        {
            Button btn = sender as Button;
            using (LinearGradientBrush brush = new LinearGradientBrush(btn.ClientRectangle,
                Color.FromArgb(0, 150, 240), Color.FromArgb(0, 100, 200), 90f))
            {
                e.Graphics.FillRectangle(brush, btn.ClientRectangle);
            }
            TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, btn.ForeColor, 
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void OnSettingsBtnPaint(object sender, PaintEventArgs e)
        {
            Button btn = sender as Button;
            using (LinearGradientBrush brush = new LinearGradientBrush(btn.ClientRectangle,
                Color.FromArgb(80, 90, 115), Color.FromArgb(50, 60, 85), 90f))
            {
                e.Graphics.FillRectangle(brush, btn.ClientRectangle);
            }
            TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, btn.ForeColor, 
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void OnSettingsPanelPaint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.FromArgb(80, 0, 200, 255), 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, settingsPanel.Width - 1, settingsPanel.Height - 1);
            }
        }

        private void SettingsBtn_Click(object sender, EventArgs e)
        {
            settingsVisible = !settingsVisible;
            if (settingsVisible)
            {
                settingsPanel.Height = 155;
                settingsPanel.Visible = true;
                settingsBtn.Text = "✖ 关闭";
            }
            else
            {
                settingsPanel.Height = 0;
                settingsPanel.Visible = false;
                settingsBtn.Text = "⚙️ 设置";
            }
        }

        private void TopCheck_CheckedChanged(object sender, EventArgs e)
        {
            isTopMost = topCheck.Checked;
            SetWindowPos(this.Handle, isTopMost ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        private void AutoSyncCheck_CheckedChanged(object sender, EventArgs e)
        {
            autoSync = autoSyncCheck.Checked;
        }

        private void AlertCheck_CheckedChanged(object sender, EventArgs e)
        {
            alertEnabled = alertCheck.Checked;
            alertSeconds = (int)alertSecondsNum.Value;
            alertDuration = (int)alertDurationNum.Value;
        }

        private async void ManualSyncBtn_Click(object sender, EventArgs e)
        {
            statusLabel.Text = "⏳ 状态: 同步中...";
            statusLabel.ForeColor = Color.Orange;
            manualSyncBtn.Enabled = false;

            bool success = await SyncFromNetwork();

            if (success)
            {
                statusLabel.Text = "✅ 状态: 同步成功";
                statusLabel.ForeColor = Color.FromArgb(0, 230, 150);
            }
            else
            {
                statusLabel.Text = "❌ 状态: 同步失败";
                statusLabel.ForeColor = Color.FromArgb(255, 100, 100);
            }

            manualSyncBtn.Enabled = true;
        }

        private async System.Threading.Tasks.Task<bool> SyncFromNetwork()
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "BeijingClock/1.0");
                    string json = await client.DownloadStringTaskAsync("http://worldtimeapi.org/api/timezone/Asia/Shanghai");
                    int start = json.IndexOf("\"datetime\":\"") + 12;
                    int end = json.IndexOf("\"", start);
                    string dtStr = json.Substring(start, end - start);
                    currentTime = DateTime.Parse(dtStr.Replace("Z", ""));
                    return true;
                }
            }
            catch { }
            return false;
        }

        private void StartTimers()
        {
            timer = new Timer();
            timer.Interval = 20;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            DateTime now;
            if (autoSync)
                now = currentTime;
            else
                now = DateTime.UtcNow.AddHours(8);

            string ms = now.Millisecond.ToString("000");
            timeLabel.Text = now.ToString("HH:mm:ss") + "." + ms;
            dateLabel.Text = now.ToString("yyyy年MM月dd日 dddd");

            if (alertEnabled)
            {
                CheckAlert(now);
            }
        }

        private void CheckAlert(DateTime now)
        {
            double secondsUntilNextHour = 60 - now.Second - (now.Millisecond / 1000.0);
            if (Math.Abs(secondsUntilNextHour - alertSeconds) < 0.05)
            {
                if (!lastAlertTriggered && !isAlerting)
                {
                    lastAlertTriggered = true;
                    TriggerAlert();
                }
            }
            else
            {
                lastAlertTriggered = false;
            }
        }

        private void TriggerAlert()
        {
            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                isAlerting = true;
                try
                {
                    for (int i = 0; i < alertDuration; i++)
                    {
                        Console.Beep(1000, 1000);
                    }
                }
                finally
                {
                    isAlerting = false;
                }
            });
        }

        private void StartAutoSync()
        {
            syncTimer = new Timer();
            syncTimer.Interval = 60000;
            syncTimer.Tick += async (s, e) =>
            {
                if (autoSync)
                {
                    await SyncFromNetwork();
                }
            };
            syncTimer.Start();
            var task = SyncFromNetwork();
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
