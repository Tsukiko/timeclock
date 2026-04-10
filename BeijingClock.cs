using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace BeijingClock
{
    public partial class MainForm : Form
    {
        // 用于窗口拖动
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int Msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 2;

        // 窗口阴影
        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        private Timer timer;
        private DateTime currentTime;
        private Label timeLabel;
        private Label dateLabel;
        private Label statusLabel;
        private RoundedButton manualSyncBtn;
        private RoundedButton topBtn;
        private RoundedButton soundBtn;
        private RoundedButton closeBtn;
        private RoundedButton minBtn;
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
            SetupWindow();
            StartTimer();
            SyncTime();
        }

        private void SetupWindow()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.AllowTransparency = true;
            this.BackColor = Color.FromArgb(15, 20, 35);
            this.Opacity = 0.96;
            
            MARGINS margins = new MARGINS();
            margins.leftWidth = 5;
            margins.rightWidth = 5;
            margins.topHeight = 5;
            margins.bottomHeight = 5;
            DwmExtendFrameIntoClientArea(this.Handle, ref margins);
            
            this.MouseDown += Form_MouseDown;
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "北京时间同步器";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 20, 35);

            // 关闭按钮（圆角红色）
            closeBtn = new RoundedButton();
            closeBtn.Text = "✕";
            closeBtn.Size = new Size(36, 36);
            closeBtn.Location = new Point(452, 8);
            closeBtn.BorderRadius = 18;
            closeBtn.NormalColor = Color.FromArgb(45, 50, 70);
            closeBtn.HoverColor = Color.FromArgb(220, 60, 60);
            closeBtn.PressColor = Color.FromArgb(180, 40, 40);
            closeBtn.ForeColor = Color.FromArgb(200, 210, 240);
            closeBtn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            closeBtn.Click += (s, e) => this.Close();

            // 最小化按钮（圆角灰色）
            minBtn = new RoundedButton();
            minBtn.Text = "─";
            minBtn.Size = new Size(36, 36);
            minBtn.Location = new Point(410, 8);
            minBtn.BorderRadius = 18;
            minBtn.NormalColor = Color.FromArgb(45, 50, 70);
            minBtn.HoverColor = Color.FromArgb(80, 90, 120);
            minBtn.PressColor = Color.FromArgb(60, 70, 95);
            minBtn.ForeColor = Color.FromArgb(200, 210, 240);
            minBtn.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            minBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            // 时间显示面板
            timePanel = new Panel();
            timePanel.Size = new Size(460, 140);
            timePanel.Location = new Point(20, 55);
            timePanel.BackColor = Color.FromArgb(25, 32, 55);
            timePanel.Paint += TimePanel_Paint;

            // 时间标签
            timeLabel = new Label();
            timeLabel.Font = new Font("Segoe UI", 48, FontStyle.Bold);
            timeLabel.ForeColor = Color.FromArgb(0, 210, 255);
            timeLabel.Text = "00:00:00.000";
            timeLabel.AutoSize = false;
            timeLabel.Size = new Size(460, 85);
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            timeLabel.Location = new Point(0, 15);
            timeLabel.BackColor = Color.Transparent;

            // 日期标签
            dateLabel = new Label();
            dateLabel.Font = new Font("Segoe UI", 12, FontStyle.Regular);
            dateLabel.ForeColor = Color.FromArgb(160, 180, 220);
            dateLabel.Text = "2024年01月01日 星期一";
            dateLabel.AutoSize = false;
            dateLabel.Size = new Size(460, 30);
            dateLabel.TextAlign = ContentAlignment.MiddleCenter;
            dateLabel.Location = new Point(0, 95);
            dateLabel.BackColor = Color.Transparent;

            timePanel.Controls.Add(timeLabel);
            timePanel.Controls.Add(dateLabel);

            // 手动同步按钮
            manualSyncBtn = new RoundedButton();
            manualSyncBtn.Text = "🔄 手动同步";
            manualSyncBtn.Size = new Size(130, 42);
            manualSyncBtn.Location = new Point(35, 215);
            manualSyncBtn.BorderRadius = 21;
            manualSyncBtn.NormalColor = Color.FromArgb(0, 120, 212);
            manualSyncBtn.HoverColor = Color.FromArgb(0, 140, 240);
            manualSyncBtn.PressColor = Color.FromArgb(0, 100, 180);
            manualSyncBtn.ForeColor = Color.White;
            manualSyncBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            manualSyncBtn.Click += ManualSyncBtn_Click;

            // 窗口置顶按钮
            topBtn = new RoundedButton();
            topBtn.Text = "📌 置顶 OFF";
            topBtn.Size = new Size(120, 42);
            topBtn.Location = new Point(185, 215);
            topBtn.BorderRadius = 21;
            topBtn.NormalColor = Color.FromArgb(60, 65, 85);
            topBtn.HoverColor = Color.FromArgb(80, 85, 110);
            topBtn.PressColor = Color.FromArgb(50, 55, 75);
            topBtn.ForeColor = Color.FromArgb(180, 190, 220);
            topBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            topBtn.Click += TopBtn_Click;

            // 声音提醒按钮
            soundBtn = new RoundedButton();
            soundBtn.Text = "🔔 提醒 OFF";
            soundBtn.Size = new Size(120, 42);
            soundBtn.Location = new Point(325, 215);
            soundBtn.BorderRadius = 21;
            soundBtn.NormalColor = Color.FromArgb(60, 65, 85);
            soundBtn.HoverColor = Color.FromArgb(80, 85, 110);
            soundBtn.PressColor = Color.FromArgb(50, 55, 75);
            soundBtn.ForeColor = Color.FromArgb(180, 190, 220);
            soundBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            soundBtn.Click += SoundBtn_Click;

            // 声音设置面板
            soundPanel = new Panel();
            soundPanel.Size = new Size(460, 50);
            soundPanel.Location = new Point(20, 270);
            soundPanel.BackColor = Color.FromArgb(30, 38, 60);
            soundPanel.Visible = false;
            soundPanel.Paint += SoundPanel_Paint;

            Label preLabel = new Label();
            preLabel.Text = "⏰ 提前:";
            preLabel.Location = new Point(80, 15);
            preLabel.Size = new Size(55, 25);
            preLabel.ForeColor = Color.FromArgb(180, 200, 240);
            preLabel.BackColor = Color.Transparent;
            preLabel.Font = new Font("Segoe UI", 9);
            
            alertSecondsNum = new NumericUpDown();
            alertSecondsNum.Location = new Point(135, 13);
            alertSecondsNum.Size = new Size(55, 25);
            alertSecondsNum.Minimum = 1;
            alertSecondsNum.Maximum = 30;
            alertSecondsNum.Value = 5;
            alertSecondsNum.BackColor = Color.FromArgb(50, 55, 75);
            alertSecondsNum.ForeColor = Color.FromArgb(0, 210, 255);
            alertSecondsNum.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            
            Label secLabel1 = new Label();
            secLabel1.Text = "秒时提醒";
            secLabel1.Location = new Point(195, 15);
            secLabel1.Size = new Size(65, 25);
            secLabel1.ForeColor = Color.FromArgb(180, 200, 240);
            secLabel1.BackColor = Color.Transparent;
            secLabel1.Font = new Font("Segoe UI", 9);

            Label durLabel = new Label();
            durLabel.Text = "持续:";
            durLabel.Location = new Point(275, 15);
            durLabel.Size = new Size(45, 25);
            durLabel.ForeColor = Color.FromArgb(180, 200, 240);
            durLabel.BackColor = Color.Transparent;
            durLabel.Font = new Font("Segoe UI", 9);
            
            alertDurationNum = new NumericUpDown();
            alertDurationNum.Location = new Point(320, 13);
            alertDurationNum.Size = new Size(55, 25);
            alertDurationNum.Minimum = 1;
            alertDurationNum.Maximum = 10;
            alertDurationNum.Value = 3;
            alertDurationNum.BackColor = Color.FromArgb(50, 55, 75);
            alertDurationNum.ForeColor = Color.FromArgb(0, 210, 255);
            alertDurationNum.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            
            Label secLabel2 = new Label();
            secLabel2.Text = "秒";
            secLabel2.Location = new Point(380, 15);
            secLabel2.Size = new Size(30, 25);
            secLabel2.ForeColor = Color.FromArgb(180, 200, 240);
            secLabel2.BackColor = Color.Transparent;
            secLabel2.Font = new Font("Segoe UI", 9);

            soundPanel.Controls.AddRange(new Control[] { preLabel, alertSecondsNum, secLabel1, durLabel, alertDurationNum, secLabel2 });

            // 状态栏
            statusLabel = new Label();
            statusLabel.Text = "✅ 状态: 运行中";
            statusLabel.ForeColor = Color.FromArgb(0, 210, 140);
            statusLabel.Font = new Font("Segoe UI", 9);
            statusLabel.AutoSize = false;
            statusLabel.Size = new Size(460, 30);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Location = new Point(20, 345);
            statusLabel.BackColor = Color.Transparent;

            this.Controls.Add(closeBtn);
            this.Controls.Add(minBtn);
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
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            GraphicsPath path = new GraphicsPath();
            int radius = 20;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(timePanel.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(timePanel.Width - radius, timePanel.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, timePanel.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();

            using (LinearGradientBrush brush = new LinearGradientBrush(
                timePanel.ClientRectangle,
                Color.FromArgb(35, 42, 70),
                Color.FromArgb(22, 28, 50),
                90f))
            {
                g.FillPath(brush, path);
            }

            using (Pen pen = new Pen(Color.FromArgb(150, 0, 200, 255), 2))
            {
                g.DrawPath(pen, path);
            }
        }

        private void SoundPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            GraphicsPath path = new GraphicsPath();
            int radius = 12;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(soundPanel.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(soundPanel.Width - radius, soundPanel.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, soundPanel.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            
            using (LinearGradientBrush brush = new LinearGradientBrush(
                soundPanel.ClientRectangle,
                Color.FromArgb(38, 45, 70),
                Color.FromArgb(28, 35, 58),
                90f))
            {
                g.FillPath(brush, path);
            }
            
            using (Pen pen = new Pen(Color.FromArgb(80, 0, 180, 240), 1))
            {
                g.DrawPath(pen, path);
            }
        }

        private void TopBtn_Click(object sender, EventArgs e)
        {
            isTopMost = !isTopMost;
            this.TopMost = isTopMost;
            
            if (isTopMost)
            {
                topBtn.Text = "📌 置顶 ON";
                topBtn.NormalColor = Color.FromArgb(0, 120, 212);
                topBtn.HoverColor = Color.FromArgb(0, 140, 240);
                topBtn.PressColor = Color.FromArgb(0, 100, 180);
                topBtn.ForeColor = Color.White;
            }
            else
            {
                topBtn.Text = "📌 置顶 OFF";
                topBtn.NormalColor = Color.FromArgb(60, 65, 85);
                topBtn.HoverColor = Color.FromArgb(80, 85, 110);
                topBtn.PressColor = Color.FromArgb(50, 55, 75);
                topBtn.ForeColor = Color.FromArgb(180, 190, 220);
            }
        }

        private void SoundBtn_Click(object sender, EventArgs e)
        {
            soundEnabled = !soundEnabled;
            
            if (soundEnabled)
            {
                soundBtn.Text = "🔔 提醒 ON";
                soundBtn.NormalColor = Color.FromArgb(0, 120, 212);
                soundBtn.HoverColor = Color.FromArgb(0, 140, 240);
                soundBtn.PressColor = Color.FromArgb(0, 100, 180);
                soundBtn.ForeColor = Color.White;
                soundPanel.Visible = true;
                StartAlertTimer();
            }
            else
            {
                soundBtn.Text = "🔔 提醒 OFF";
                soundBtn.NormalColor = Color.FromArgb(60, 65, 85);
                soundBtn.HoverColor = Color.FromArgb(80, 85, 110);
                soundBtn.PressColor = Color.FromArgb(50, 55, 75);
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

    // 圆角按钮自定义控件
    public class RoundedButton : Button
    {
        private Color _normalColor = Color.FromArgb(0, 120, 212);
        private Color _hoverColor = Color.FromArgb(0, 140, 240);
        private Color _pressColor = Color.FromArgb(0, 100, 180);
        private int _borderRadius = 10;
        private bool _isHovering = false;
        private bool _isPressing = false;

        public Color NormalColor
        {
            get { return _normalColor; }
            set { _normalColor = value; Invalidate(); }
        }

        public Color HoverColor
        {
            get { return _hoverColor; }
            set { _hoverColor = value; Invalidate(); }
        }

        public Color PressColor
        {
            get { return _pressColor; }
            set { _pressColor = value; Invalidate(); }
        }

        public int BorderRadius
        {
            get { return _borderRadius; }
            set { _borderRadius = value; Invalidate(); }
        }

        public RoundedButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.BackColor = _normalColor;
            this.ForeColor = Color.White;
            this.Cursor = Cursors.Hand;
            
            this.MouseEnter += (s, e) => { _isHovering = true; _isPressing = false; Invalidate(); };
            this.MouseLeave += (s, e) => { _isHovering = false; _isPressing = false; Invalidate(); };
            this.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _isPressing = true; Invalidate(); } };
            this.MouseUp += (s, e) => { _isPressing = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color currentColor;
            if (_isPressing)
                currentColor = _pressColor;
            else if (_isHovering)
                currentColor = _hoverColor;
            else
                currentColor = _normalColor;

            using (GraphicsPath path = GetRoundedRectangle(this.ClientRectangle, _borderRadius))
            using (SolidBrush brush = new SolidBrush(currentColor))
            {
                g.FillPath(brush, path);
            }

            // 绘制文字
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                using (SolidBrush textBrush = new SolidBrush(this.ForeColor))
                {
                    g.DrawString(this.Text, this.Font, textBrush, this.ClientRectangle, sf);
                }
            }
        }

        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
