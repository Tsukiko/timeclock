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
            mainPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.FromArgb(10, 15, 30),
                Padding = new Padding(20, 20, 20, 20)
            };

            // 时间显示面板（带发光边框）
            timePanel = new Panel
            {
                Height = 160,
                Width = 480,
                BackColor = Color.FromArgb(18, 25, 45),
                Margin = new Padding(0, 0, 0, 15)
            };
            
            // 绘制渐变背景和发光边框
            timePanel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                
                // 渐变背景
                using (LinearGradientBrush brush = new LinearGradientBrush(timePanel.ClientRectangle, 
                    Color.FromArgb(18, 25, 45), Color.FromArgb(25, 35, 60), 90f))
                {
                    g.FillRectangle(brush, timePanel.ClientRectangle);
                }
                
                // 发光边框
                using (Pen pen = new Pen(Color.FromArgb(80, 100, 255, 200), 2))
                {
                    pen.Alignment = PenAlignment.Inset;
                    g.DrawRectangle(pen, 1, 1, timePanel.Width - 3, timePanel.Height - 3);
                }
            };

            // 时间标签（带荧光效果）
            timeLabel = new Label
            {
                Font = new Font("Segoe UI", 56, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 255),
                Text = "00:00:00.000",
                AutoSize = false,
                Size = new Size(480, 90),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 20),
                BackColor = Color.Transparent
            };
            
            // 添加荧光效果 - 使用阴影
            timeLabel.Paint += (s, e) =>
            {
                Label label = s as Label;
                if (label != null)
                {
                    // 绘制荧光光晕
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        path.AddString(label.Text, label.Font.FontFamily, (int)label.Font.Style, 
                            label.Font.SizeInPoints * 1.2f, new Point(10, 10), StringFormat.GenericDefault);
                        
                        using (Pen pen = new Pen(Color.FromArgb(80, 0, 230, 255), 8))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }
                }
            };

            // 日期标签
            dateLabel = new Label
            {
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 190, 220),
                Text = "2024年01月01日 星期一",
                AutoSize = false,
                Size = new Size(480, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 90),
                BackColor = Color.Transparent
            };

            // 时区标签
            timezoneLabel = new Label
            {
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 150, 200),
                Text = "★ Asia/Shanghai (UTC+8) ★",
                AutoSize = false,
                Size = new Size(480, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 122),
                BackColor = Color.Transparent
            };

            timePanel.Controls.AddRange(new Control[] { timeLabel, dateLabel, timezoneLabel });

            // 按钮面板
            Panel buttonPanel = new Panel
            {
                Height = 50,
                Width = 480,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12)
            };

            // 手动同步按钮（带渐变效果）
            manualSyncBtn = new Button
            {
                Text = "🔄 手动同步",
                Size = new Size(140, 40),
                Location = new Point(60, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            manualSyncBtn.FlatAppearance.BorderSize = 0;
            manualSyncBtn.Paint += (s, e) =>
            {
                Button btn = s as Button;
                using (LinearGradientBrush brush = new LinearGradientBrush(btn.ClientRectangle,
                    Color.FromArgb(0, 150, 240), Color.FromArgb(0, 100, 200), 90f))
                {
                    e.Graphics.FillRectangle(brush, btn.ClientRectangle);
                }
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, btn.ForeColor, 
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            manualSyncBtn.Click += ManualSyncBtn_Click;

            // 设置按钮
            settingsBtn = new Button
            {
                Text = "⚙️ 设置",
                Size = new Size(140, 40),
                Location = new Point(280, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 70, 95),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            settingsBtn.FlatAppearance.BorderSize = 0;
            settingsBtn.Paint += (s, e) =>
            {
                Button btn = s as Button;
                using (LinearGradientBrush brush = new LinearGradientBrush(btn.ClientRectangle,
                    Color.FromArgb(80, 90, 115), Color.FromArgb(50, 60, 85), 90f))
                {
                    e.Graphics.FillRectangle(brush, btn.ClientRectangle);
                }
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, btn.ForeColor, 
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            settingsBtn.Click += SettingsBtn_Click;

            buttonPanel.Controls.AddRange(new Control[] { manualSyncBtn, settingsBtn });

            // 设置面板（初始隐藏）
            settingsPanel = new Panel
            {
                Height = 0,
                Width = 480,
                BackColor = Color.FromArgb(20, 30, 55),
                Margin = new Padding(0, 0, 0, 12),
                Visible = false
            };
            settingsPanel.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(80, 0, 200, 255), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, settingsPanel.Width - 1, settingsPanel.Height - 1);
                }
            };

            // 置顶复选框
            topCheck = new CheckBox
            {
                Text = " 窗口置顶",
                Location = new Point(20, 15),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 210, 240),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10)
            };
            topCheck.CheckedChanged += TopCheck_CheckedChanged;

            // 自动同步复选框
            autoSyncCheck = new CheckBox
            {
                Text = " 自动同步时间",
                Location = new Point(20, 48),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 210, 240),
                BackColor = Color.Transparent,
                Checked = true,
                Font = new Font("Segoe UI", 10)
            };
            autoSyncCheck.CheckedChanged += AutoSyncCheck_CheckedChanged;

            // 声音提醒复选框
            alertCheck = new CheckBox
            {
                Text = " 启用整点提醒",
                Location = new Point(20, 81),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 210, 240),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10)
            };
            alertCheck.CheckedChanged += AlertCheck_CheckedChanged;

            // 提前秒数
            Label preLabel = new Label
            {
                Text = "提前",
                Location = new Point(40, 115),
                AutoSize = true,
                ForeColor = Color.FromArgb(150, 170, 210),
                Font = new Font("Segoe UI", 9)
            };
            alertSecondsNum = new NumericUpDown
            {
                Location = new Point(85, 112),
                Size = new Size(55, 25),
                Minimum = 1,
                Maximum = 30,
                Value = 5,
                BackColor = Color.FromArgb(30, 40, 65),
                ForeColor = Color.FromArgb(0, 230, 255),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            Label secLabel1 = new Label
            {
                Text = "秒时提醒",
                Location = new Point(150, 115),
                AutoSize = true,
                ForeColor = Color.FromArgb(150, 170, 210),
                Font = new Font("Segoe UI", 9)
            };

            // 持续秒数
            Label durLabel = new Label
            {
                Text = "持续",
                Location = new Point(260, 115),
                AutoSize = true,
                ForeColor = Color.FromArgb(150, 170, 210),
                Font = new Font("Segoe UI", 9)
            };
            alertDurationNum = new NumericUpDown
            {
                Location = new Point(305, 112),
                Size = new Size(55, 25),
                Minimum = 1,
                Maximum = 10,
                Value = 3,
                BackColor = Color.FromArgb(30, 40, 65),
                ForeColor = Color.FromArgb(0, 230, 255),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            Label secLabel2 = new Label
            {
                Text = "秒",
                Location = new Point(370, 115),
                AutoSize = true,
                ForeColor = Color.FromArgb(150, 170, 210),
                Font = new Font("Segoe UI", 9)
            };

            settingsPanel.Controls.AddRange(new Control[] { topCheck, autoSyncCheck, alertCheck, preLabel, alertSecondsNum, secLabel1, durLabel, alertDurationNum, secLabel2 });

            // 状态栏
            statusLabel = new Label
            {
                Text = "✅ 状态: 运行中",
                ForeColor = Color.FromArgb(0, 230, 150),
                Font = new Font("Segoe UI", 9),
                AutoSize = false,
                Size = new Size(480, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 5, 0, 0)
            };

            mainPanel.Controls.AddRange(new Control[] { timePanel, buttonPanel, settingsPanel, statusLabel });
            this.Controls.Add(mainPanel);
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

            timeLabel.Text = now.ToString("HH:mm:ss") + $".{now.Millisecond:000}";
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
            ThreadPool.QueueUserWorkItem(_ =>
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
            _ = SyncFromNetwork();
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
