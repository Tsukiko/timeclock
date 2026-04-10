using System;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace BeijingClock
{
    public partial class MainForm : Form
    {
        // 窗口置顶
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

        // 毛玻璃效果 (Windows 10/11)
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
        
        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }
        
        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }

        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.Timer syncTimer;
        private DateTime currentTime;
        private bool isTopMost = false;
        private bool autoSync = true;
        private bool alertEnabled = false;
        private int alertSeconds = 5;
        private int alertDuration = 3;
        private bool lastAlertTriggered = false;
        private bool isAlerting = false;
        private bool isTransparent = true;
        
        private Label timeLabel;
        private Label dateLabel;
        private Label statusLabel;
        private Button manualSyncBtn;
        private CheckBox topCheck;
        private CheckBox autoSyncCheck;
        private CheckBox alertCheck;
        private CheckBox transparentCheck;
        private NumericUpDown alertSecondsNum;
        private NumericUpDown alertDurationNum;
        private Panel settingsPanel;
        private bool settingsVisible = false;

        public MainForm()
        {
            currentTime = DateTime.Now;
            InitializeComponent();
            ApplyAcrylicBlur();
            StartTimers();
            StartAutoSync();
        }

        private void ApplyAcrylicBlur()
        {
            try
            {
                var accent = new AccentPolicy();
                accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
                accent.GradientColor = (0x40 << 24) | (0x1A << 16) | (0x1F << 8) | 0x2C;
                
                var accentStructSize = Marshal.SizeOf(accent);
                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);
                
                var data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;
                
                SetWindowCompositionAttribute(this.Handle, ref data);
                
                Marshal.FreeHGlobal(accentPtr);
                
                this.BackColor = Color.FromArgb(50, 20, 25, 40);
                this.TransparencyKey = Color.Empty;
            }
            catch
            {
                this.BackColor = Color.FromArgb(200, 20, 25, 40);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "北京时间同步器";
            this.Size = new Size(440, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(220, 20, 25, 40);
            this.Opacity = 0.92;

            // 时间标签
            timeLabel = new Label();
            timeLabel.Font = new Font("Segoe UI", 48, FontStyle.Bold);
            timeLabel.ForeColor = Color.FromArgb(0, 230, 255);
            timeLabel.Text = "00:00:00.000";
            timeLabel.AutoSize = false;
            timeLabel.Size = new Size(400, 80);
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            timeLabel.Location = new Point(20, 20);
            timeLabel.BackColor = Color.Transparent;

            // 日期标签
            dateLabel = new Label();
            dateLabel.Font = new Font("Segoe UI", 12);
            dateLabel.ForeColor = Color.FromArgb(220, 230, 255);
            dateLabel.Text = "2024年01月01日 星期一";
            dateLabel.AutoSize = false;
            dateLabel.Size = new Size(400, 30);
            dateLabel.TextAlign = ContentAlignment.MiddleCenter;
            dateLabel.Location = new Point(20, 100);
            dateLabel.BackColor = Color.Transparent;

            // 手动同步按钮
            manualSyncBtn = new Button();
            manualSyncBtn.Text = "🔄 手动同步";
            manualSyncBtn.Size = new Size(120, 35);
            manualSyncBtn.Location = new Point(150, 145);
            manualSyncBtn.BackColor = Color.FromArgb(80, 0, 120, 212);
            manualSyncBtn.ForeColor = Color.White;
            manualSyncBtn.FlatStyle = FlatStyle.Flat;
            manualSyncBtn.FlatAppearance.BorderSize = 0;
            manualSyncBtn.Click += ManualSyncBtn_Click;

            // 置顶复选框
            topCheck = new CheckBox();
            topCheck.Text = "📌 窗口置顶";
            topCheck.Location = new Point(15, 195);
            topCheck.Size = new Size(100, 25);
            topCheck.ForeColor = Color.White;
            topCheck.BackColor = Color.Transparent;
            topCheck.CheckedChanged += TopCheck_CheckedChanged;

            // 自动同步复选框
            autoSyncCheck = new CheckBox();
            autoSyncCheck.Text = "🔄 自动同步";
            autoSyncCheck.Location = new Point(125, 195);
            autoSyncCheck.Size = new Size(100, 25);
            autoSyncCheck.ForeColor = Color.White;
            autoSyncCheck.BackColor = Color.Transparent;
            autoSyncCheck.Checked = true;
            autoSyncCheck.CheckedChanged += AutoSyncCheck_CheckedChanged;

            // 声音提醒复选框
            alertCheck = new CheckBox();
            alertCheck.Text = "🔔 启用提醒";
            alertCheck.Location = new Point(235, 195);
            alertCheck.Size = new Size(100, 25);
            alertCheck.ForeColor = Color.White;
            alertCheck.BackColor = Color.Transparent;
            alertCheck.CheckedChanged += AlertCheck_CheckedChanged;

            // 透明效果复选框
            transparentCheck = new CheckBox();
            transparentCheck.Text = "✨ 毛玻璃";
            transparentCheck.Location = new Point(345, 195);
            transparentCheck.Size = new Size(80, 25);
            transparentCheck.ForeColor = Color.FromArgb(0, 230, 255);
            transparentCheck.BackColor = Color.Transparent;
            transparentCheck.Checked = true;
            transparentCheck.CheckedChanged += TransparentCheck_CheckedChanged;

            // 设置面板
            settingsPanel = new Panel();
            settingsPanel.Height = 0;
            settingsPanel.Width = 400;
            settingsPanel.BackColor = Color.FromArgb(60, 30, 35, 55);
            settingsPanel.Location = new Point(20, 225);
            settingsPanel.Visible = false;

            Label preLabel = new Label();
            preLabel.Text = "提前:";
            preLabel.Location = new Point(15, 12);
            preLabel.Size = new Size(40, 23);
            preLabel.ForeColor = Color.White;
            preLabel.BackColor = Color.Transparent;
            
            alertSecondsNum = new NumericUpDown();
            alertSecondsNum.Location = new Point(55, 10);
            alertSecondsNum.Size = new Size(55, 25);
            alertSecondsNum.Minimum = 1;
            alertSecondsNum.Maximum = 30;
            alertSecondsNum.Value = 5;
            alertSecondsNum.BackColor = Color.FromArgb(80, 50, 55, 75);
            alertSecondsNum.ForeColor = Color.White;
            
            Label secLabel1 = new Label();
            secLabel1.Text = "秒时提醒";
            secLabel1.Location = new Point(115, 12);
            secLabel1.Size = new Size(70, 23);
            secLabel1.ForeColor = Color.White;
            secLabel1.BackColor = Color.Transparent;

            Label durLabel = new Label();
            durLabel.Text = "持续:";
            durLabel.Location = new Point(195, 12);
            durLabel.Size = new Size(40, 23);
            durLabel.ForeColor = Color.White;
            durLabel.BackColor = Color.Transparent;
            
            alertDurationNum = new NumericUpDown();
            alertDurationNum.Location = new Point(235, 10);
            alertDurationNum.Size = new Size(55, 25);
            alertDurationNum.Minimum = 1;
            alertDurationNum.Maximum = 10;
            alertDurationNum.Value = 3;
            alertDurationNum.BackColor = Color.FromArgb(80, 50, 55, 75);
            alertDurationNum.ForeColor = Color.White;
            
            Label secLabel2 = new Label();
            secLabel2.Text = "秒";
            secLabel2.Location = new Point(295, 12);
            secLabel2.Size = new Size(30, 23);
            secLabel2.ForeColor = Color.White;
            secLabel2.BackColor = Color.Transparent;

            settingsPanel.Controls.AddRange(new Control[] { preLabel, alertSecondsNum, secLabel1, durLabel, alertDurationNum, secLabel2 });

            // 状态栏
            statusLabel = new Label();
            statusLabel.Text = "✅ 状态: 运行中";
            statusLabel.ForeColor = Color.FromArgb(0, 230, 150);
            statusLabel.AutoSize = false;
            statusLabel.Size = new Size(400, 25);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Location = new Point(20, 300);
            statusLabel.BackColor = Color.Transparent;

            this.Controls.AddRange(new Control[] { timeLabel, dateLabel, manualSyncBtn, topCheck, autoSyncCheck, alertCheck, transparentCheck, settingsPanel, statusLabel });
        }

        private void TransparentCheck_CheckedChanged(object sender, EventArgs e)
        {
            isTransparent = transparentCheck.Checked;
            if (isTransparent)
            {
                this.Opacity = 0.92;
                this.BackColor = Color.FromArgb(220, 20, 25, 40);
                ApplyAcrylicBlur();
            }
            else
            {
                this.Opacity = 1.0;
                this.BackColor = Color.FromArgb(20, 25, 40);
            }
        }

        private void TopCheck_CheckedChanged(object sender, EventArgs e)
        {
            isTopMost = topCheck.Checked;
            this.TopMost = isTopMost;
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
            
            if (alertCheck.Checked)
            {
                settingsPanel.Height = 45;
                settingsPanel.Visible = true;
            }
            else
            {
                settingsPanel.Height = 0;
                settingsPanel.Visible = false;
            }
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
                statusLabel.ForeColor = Color.Red;
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
            timer = new System.Windows.Forms.Timer();
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
            syncTimer = new System.Windows.Forms.Timer();
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
