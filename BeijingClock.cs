using System;
using System.Drawing;
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
        private CheckBox topCheck;

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
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(20, 25, 40);

            timeLabel = new Label();
            timeLabel.Font = new Font("Segoe UI", 42, FontStyle.Bold);
            timeLabel.ForeColor = Color.FromArgb(0, 200, 255);
            timeLabel.Text = "00:00:00.000";
            timeLabel.AutoSize = false;
            timeLabel.Size = new Size(380, 70);
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            timeLabel.Location = new Point(10, 20);
            timeLabel.BackColor = Color.Transparent;

            dateLabel = new Label();
            dateLabel.Font = new Font("Segoe UI", 12);
            dateLabel.ForeColor = Color.FromArgb(180, 190, 220);
            dateLabel.Text = "2024年01月01日 星期一";
            dateLabel.AutoSize = false;
            dateLabel.Size = new Size(380, 30);
            dateLabel.TextAlign = ContentAlignment.MiddleCenter;
            dateLabel.Location = new Point(10, 95);
            dateLabel.BackColor = Color.Transparent;

            topCheck = new CheckBox();
            topCheck.Text = "窗口置顶";
            topCheck.Location = new Point(150, 135);
            topCheck.Size = new Size(100, 25);
            topCheck.ForeColor = Color.White;
            topCheck.BackColor = Color.Transparent;
            topCheck.CheckedChanged += TopCheck_CheckedChanged;

            this.Controls.Add(timeLabel);
            this.Controls.Add(dateLabel);
            this.Controls.Add(topCheck);
        }

        private void TopCheck_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = topCheck.Checked;
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
