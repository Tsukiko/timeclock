using System;
using System.Drawing;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

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
        private readonly HttpClient httpClient;
        private Label timeLabel;
        private Label dateLabel;
        private Label statusLabel;
        private CheckBox topCheck;
        private CheckBox autoSyncCheck;
        private Button manualSyncBtn;
        private CheckBox alertCheck;
        private NumericUpDown alertSecondsNum;
        private NumericUpDown alertDurationNum;

        public MainForm()
        {
            httpClient = new HttpClient();
            currentTime = DateTime.Now;
            InitializeComponent();
            StartTimers();
            StartAutoSync();
        }

        private void InitializeComponent()
        {
            this.Text = "北京时间同步器";
            this.Size = new Size(420, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // 时间标签
            timeLabel = new Label
            {
                Font = new Font("Arial", 48, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                Text = "00:00:00.000",
                AutoSize = true,
                Location = new Point(60, 30)
            };

            // 日期标签
            dateLabel = new Label
            {
                Font = new Font("Arial", 14),
                Text = "2024年01月01日 星期一",
                AutoSize = true,
                Location = new Point(80, 100)
            };

            // 控制面板
            Panel controlPanel = new Panel { Location = new Point(20, 140), Size = new Size(360, 40) };

            topCheck = new CheckBox
            {
                Text = "窗口置顶",
                Location = new Point(10, 10),
                AutoSize = true
            };
            topCheck.CheckedChanged += TopCheck_CheckedChanged;

            autoSyncCheck = new CheckBox
            {
                Text = "自动同步",
                Location = new Point(100, 10),
                AutoSize = true,
                Checked = true
            };
            autoSyncCheck.CheckedChanged += AutoSyncCheck_CheckedChanged;

            manualSyncBtn = new Button
            {
                Text = "手动同步",
                Location = new Point(190, 5),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            manualSyncBtn.Click += ManualSyncBtn_Click;

            controlPanel.Controls.AddRange(new Control[] { topCheck, autoSyncCheck, manualSyncBtn });

            // 声音提醒区域
            GroupBox alertGroup = new GroupBox
            {
                Text = "声音提醒设置",
                Location = new Point(20, 190),
                Size = new Size(360, 110)
            };

            alertCheck = new CheckBox
            {
                Text = "启用整点提醒",
                Location = new Point(15, 25),
                AutoSize = true
            };
            alertCheck.CheckedChanged += AlertCheck_CheckedChanged;

            Label preLabel = new Label { Text = "提前:", Location = new Point(15, 55), AutoSize = true };
            alertSecondsNum = new NumericUpDown
            {
                Location = new Point(55, 52),
                Size = new Size(50, 23),
                Minimum = 1,
                Maximum = 30,
                Value = 5
            };
            Label secLabel1 = new Label { Text = "秒时提醒", Location = new Point(110, 55), AutoSize = true };

            Label durLabel = new Label { Text = "提醒持续:", Location = new Point(15, 80), AutoSize = true };
            alertDurationNum = new NumericUpDown
            {
                Location = new Point(85, 77),
                Size = new Size(50, 23),
                Minimum = 1,
                Maximum = 10,
                Value = 3
            };
            Label secLabel2 = new Label { Text = "秒", Location = new Point(140, 80), AutoSize = true };

            Label tipLabel = new Label
            {
                Text = "* 在整点前指定秒数时发出提示音",
                Font = new Font("Arial", 8),
                ForeColor = Color.Gray,
                Location = new Point(15, 105),
                AutoSize = true
            };

            alertGroup.Controls.AddRange(new Control[] { alertCheck, preLabel, alertSecondsNum, secLabel1, durLabel, alertDurationNum, secLabel2, tipLabel });

            // 状态栏
            statusLabel = new Label
            {
                Text = "✅ 状态: 运行中",
                ForeColor = Color.Green,
                Font = new Font("Arial", 9),
                AutoSize = true,
                Location = new Point(20, 315)
            };

            // 添加控件
            this.Controls.AddRange(new Control[] { timeLabel, dateLabel, controlPanel, alertGroup, statusLabel });
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
                statusLabel.ForeColor = Color.Green;
            }
            else
            {
                statusLabel.Text = "❌ 状态: 同步失败";
                statusLabel.ForeColor = Color.Red;
            }

            manualSyncBtn.Enabled = true;
        }

        private async Task<bool> SyncFromNetwork()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    HttpResponseMessage response = await client.GetAsync("http://worldtimeapi.org/api/timezone/Asia/Shanghai");
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        // 简单解析 datetime 字段
                        int start = json.IndexOf("\"datetime\":\"") + 12;
                        int end = json.IndexOf("\"", start);
                        string dtStr = json.Substring(start, end - start);
                        currentTime = DateTime.Parse(dtStr.Replace("Z", ""));
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private void StartTimers()
        {
            // 更新时间显示（每20ms）
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

            // 更新时间显示
            timeLabel.Text = now.ToString("HH:mm:ss") + $".{now.Millisecond:000}";
            dateLabel.Text = now.ToString("yyyy年MM月dd日 dddd");

            // 检查提醒
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
            syncTimer.Interval = 60000; // 每分钟同步一次
            syncTimer.Tick += async (s, e) =>
            {
                if (autoSync)
                {
                    await SyncFromNetwork();
                }
            };
            syncTimer.Start();
            // 立即同步一次
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