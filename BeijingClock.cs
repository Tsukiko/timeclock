using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net;
using System.Windows.Forms;

namespace BeijingClock
{
    public partial class MainForm : Form
    {
        private Timer timer;
        private DateTime currentTime;
        private Label timeLabel;
        private Label dateLabel;
        private Label statusLabel;
        private Button manualSyncBtn;
        private Button topBtn;
        private Button soundBtn;
        private Panel timePanel;
        
        private bool isTopMost = false;
        private bool soundEnabled = false;
        private int alertSeconds = 5;
        private int alertDuration = 3;
        private bool lastAlertTriggered = false;
        private bool isAlerting = false;
        private Timer alertTimer;
        private NumericUpDown alertSecondsNum;
        private NumericUpDown alertDurationNum;
        private Panel soundPanel;
        private bool soundPanelVisible = false;

        public MainForm()
        {
            currentTime = DateTime.Now;
            InitializeComponent();
            StartTimer();
            SyncTime();
        }

        private void InitializeComponent()
        {
            this.Text = "北京时间同步器";
            this.Size = new Size(480, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(15, 20, 35);
            this.Opacity = 0.96;

            // 时间显示面板
            timePanel = new Panel();
            timePanel.Size = new Size(440, 130);
            timePanel.Location = new Point(20, 20);
            timePanel.BackColor = Color.FromArgb(25, 32, 55);
            timePanel.Paint += TimePanel_Paint;

            // 时间标签
            timeLabel = new Label();
            timeLabel.Font = new Font("Segoe UI", 46, FontStyle.Bold);
            timeLabel.ForeColor = Color.FromArgb(0, 210, 255);
            timeLabel.Text = "00:00:00.000";
            timeLabel.AutoSize = false;
            timeLabel.Size = new Size(440, 80);
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            timeLabel.Location = new Point(0, 15);
            timeLabel.BackColor = Color.Transparent;

            // 日期标签
            dateLabel = new Label();
            dateLabel.Font = new Font("Segoe UI", 12, FontStyle.Regular);
            dateLabel.ForeColor = Color.FromArgb(160, 180, 220);
            dateLabel.Text = "2024年01月01日 星期一";
            dateLabel.AutoSize = false;
            dateLabel.Size = new Size(440, 30);
            dateLabel.TextAlign = ContentAlignment.MiddleCenter;
            dateLabel.Location = new Point(0, 85);
            dateLabel.BackColor = Color.Transparent;

            timePanel.Controls.Add(timeLabel);
            timePanel.Controls.Add(dateLabel);

            // 手动同步按钮
            manualSyncBtn = new Button();
            manualSyncBtn.Text = "🔄 手动同步";
            manualSyncBtn.Size = new Size(120, 38);
            manualSyncBtn.Location = new Point(25, 170);
            manualSyncBtn.BackColor = Color.FromArgb(0, 120, 212);
            manualSyncBtn.ForeColor = Color.White;
            manualSyncBtn.FlatStyle = FlatStyle.Flat;
            manualSyncBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            manualSyncBtn.Cursor = Cursors.Hand;
            manualSyncBtn.FlatAppearance.BorderSize = 0;
            manualSyncBtn.Click += ManualSyncBtn_Click;

            // 窗口置顶按钮（开关样式）
            topBtn = new Button();
            topBtn.Text = "📌 窗口置顶 OFF";
            topBtn.Size = new Size(120, 38);
            topBtn.Location = new Point(160, 170);
            topBtn.BackColor = Color.FromArgb(60, 65, 85);
            topBtn.ForeColor = Color.FromArgb(180, 190, 220);
            topBtn.FlatStyle = FlatStyle.Flat;
            topBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            topBtn.Cursor = Cursors.Hand;
            topBtn.FlatAppearance.BorderSize = 0;
            topBtn.Click += TopBtn_Click;

            // 声音提醒按钮（开关样式）
            soundBtn = new Button();
            soundBtn.Text = "🔔 提醒 OFF";
            soundBtn.Size = new Size(120, 38);
            soundBtn.Location = new Point(295, 170);
            soundBtn.BackColor = Color.FromArgb(60, 65, 85);
            soundBtn.ForeColor = Color.FromArgb(180, 190, 220);
            soundBtn.FlatStyle = FlatStyle.Flat;
            soundBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            soundBtn.Cursor = Cursors.Hand;
            soundBtn.FlatAppearance.BorderSize = 0;
            soundBtn.Click += SoundBtn_Click;

            // 声音设置面板
            soundPanel = new Panel();
            soundPanel.Size = new Size(440, 45);
            soundPanel.Location = new Point(20, 220);
            soundPanel.BackColor = Color.FromArgb(30, 38, 60);
            soundPanel.Visible = false;
            soundPanel.Paint += SoundPanel_Paint;

            Label preLabel = new Label();
            preLabel.Text = "提前:";
            preLabel.Location = new Point(80, 12);
            preLabel.Size = new Size(40, 23);
            preLabel.ForeColor = Color.White;
            preLabel.BackColor = Color.Transparent;
            
            alertSecondsNum = new NumericUpDown();
            alertSecondsNum.Location = new Point(120, 10);
            alertSecondsNum.Size = new Size(55, 25);
            alertSecondsNum.Minimum = 1;
            alertSecondsNum.Maximum = 30;
            alertSecondsNum.Value = 5;
            alertSecondsNum.BackColor = Color.FromArgb(50, 55, 75);
            alertSecondsNum.ForeColor = Color.White;
            
            Label secLabel1 = new Label();
            secLabel1.Text = "秒时提醒";
            secLabel1.Location = new Point(180, 12);
            secLabel1.Size = new Size(70, 23);
            secLabel1.ForeColor = Color.White;
            secLabel1.BackColor = Color.Transparent;

            Label durLabel = new Label();
            durLabel.Text = "持续:";
            durLabel.Location = new Point(260, 12);
            durLabel.Size = new Size(40, 23);
            durLabel.ForeColor = Color.White;
            durLabel.BackColor = Color.Transparent;
            
            alertDurationNum = new NumericUpDown();
            alertDurationNum.Location = new Point(300, 10);
            alertDurationNum.Size = new Size(55, 25);
            alertDurationNum.Minimum = 1;
            alertDurationNum.Maximum = 10;
            alertDurationNum.Value = 3;
            alertDurationNum.BackColor = Color.FromArgb(50, 55, 75);
            alertDurationNum.ForeColor = Color.White;
            
            Label secLabel2 = new Label();
            secLabel2.Text = "秒";
            secLabel2.Location = new Point(360, 12);
            secLabel2.Size = new Size(30, 23);
            secLabel2.ForeColor = Color.White;
            secLabel2.BackColor = Color.Transparent;

            soundPanel.Controls.AddRange(new Control[] { preLabel, alertSecondsNum, secLabel1, durLabel, alertDurationNum, secLabel2 });

            // 状态栏
            statusLabel = new Label();
            statusLabel.Text = "✅ 状态: 运行中";
            statusLabel.ForeColor = Color.FromArgb(0, 210, 140);
            statusLabel.Font = new Font("Segoe UI", 9);
            statusLabel.AutoSize = false;
            statusLabel.Size = new Size(440, 30);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Location = new Point(20, 290);
            statusLabel.BackColor = Color.Transparent;

            this.Controls.Add(timePanel);
            this.Controls.Add(manualSyncBtn);
            this.Controls.Add(topBtn);
            this.Controls.Add(soundBtn);
            this.Controls.Add(soundPanel);
            this.Controls.Add(statusLabel);
        }

        private void TimePanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            GraphicsPath path = new GraphicsPath();
            int radius = 15;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(timePanel.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(timePanel.Width - radius, timePanel.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, timePanel.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();

            using (LinearGradientBrush brush = new LinearGradientBrush(
                timePanel.ClientRectangle,
                Color.FromArgb(30, 38, 65),
                Color.FromArgb(22, 28, 50),
                90f))
            {
                g.FillPath(brush, path);
            }

            using (Pen pen = new Pen(Color.FromArgb(100, 0, 180, 240), 1.5f))
            {
                g.DrawPath(pen, path);
            }
        }

        private void SoundPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Pen pen = new Pen(Color.FromArgb(80, 0, 180, 240), 1))
            {
                g.DrawRectangle(pen, 0, 0, soundPanel.Width - 1, soundPanel.Height - 1);
            }
        }

        private void TopBtn_Click(object sender, EventArgs e)
        {
            isTopMost = !isTopMost;
            this.TopMost = isTopMost;
            
            if (isTopMost)
            {
                topBtn.Text = "📌 窗口置顶 ON";
                topBtn.BackColor = Color.FromArgb(0, 120, 212);
                topBtn.ForeColor = Color.White;
            }
            else
            {
                topBtn.Text = "📌 窗口置顶 OFF";
                topBtn.BackColor = Color.FromArgb(60, 65, 85);
                topBtn.ForeColor = Color.FromArgb(180, 190, 220);
            }
        }

        private void SoundBtn_Click(object sender, EventArgs e)
        {
            soundEnabled = !soundEnabled;
            
            if (soundEnabled)
            {
                soundBtn.Text = "🔔 提醒 ON";
                soundBtn.BackColor = Color.FromArgb(0, 120, 212);
                soundBtn.ForeColor = Color.White;
                soundPanel.Visible = true;
                StartAlertTimer();
            }
            else
            {
                soundBtn.Text = "🔔 提醒 OFF";
                soundBtn.BackColor = Color.FromArgb(60, 65, 85);
                soundBtn.ForeColor = Color.FromArgb(180, 190, 220);
                soundPanel.Visible = false;
                StopAlertTimer();
            }
        }

        private void StartAlertTimer()
        {
            if (alertTimer == null)
            {
                alertTimer = new Timer();
                alertTimer.Interval = 100;
                alertTimer.Tick += AlertTimer_Tick;
            }
            alertTimer.Start();
        }

        private void StopAlertTimer()
        {
            if (alertTimer != null)
            {
                alertTimer.Stop();
            }
        }

        private void AlertTimer_Tick(object sender, EventArgs e)
        {
            if (!soundEnabled) return;
            
            DateTime now = currentTime;
            double secondsUntilNextHour = 60 - now.Second - (now.Millisecond / 1000.0);
            int alertSec = (int)alertSecondsNum.Value;
            
            if (Math.Abs(secondsUntilNextHour - alertSec) < 0.1)
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
            System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                isAlerting = true;
                try
                {
                    int duration = (int)alertDurationNum.Value;
                    for (int i = 0; i < duration; i++)
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

        private async void ManualSyncBtn_Click(object sender, EventArgs e)
        {
            statusLabel.Text = "⏳ 状态: 同步中...";
            statusLabel.ForeColor = Color.Orange;
            manualSyncBtn.Enabled = false;

            bool success = await SyncFromNetwork();

            if (success)
            {
                statusLabel.Text = "✅ 状态: 同步成功";
                statusLabel.ForeColor = Color.FromArgb(0, 210, 140);
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
            catch
            {
                return false;
            }
        }

        private void SyncTime()
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "BeijingClock/1.0");
                    string json = client.DownloadString("http://worldtimeapi.org/api/timezone/Asia/Shanghai");
                    int start = json.IndexOf("\"datetime\":\"") + 12;
                    int end = json.IndexOf("\"", start);
                    string dtStr = json.Substring(start, end - start);
                    currentTime = DateTime.Parse(dtStr.Replace("Z", ""));
                }
            }
            catch
            {
                currentTime = DateTime.UtcNow.AddHours(8);
            }
        }

        private void StartTimer()
        {
            timer = new Timer();
            timer.Interval = 20;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            DateTime now = currentTime.AddMilliseconds(20);
            currentTime = now;
            
            string ms = now.Millisecond.ToString("000");
            timeLabel.Text = now.ToString("HH:mm:ss") + "." + ms;
            dateLabel.Text = now.ToString("yyyy年MM月dd日 dddd");
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
