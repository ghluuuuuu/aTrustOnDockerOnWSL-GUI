using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace aTrustOnWsl
{
    public partial class Form1 : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private TabControl tabControl;
        private TabPage logTabPage;
        private TextBox logTextBox = new TextBox();
        private Process dockerProcess;
        private bool isNeedClosed = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void SetupForm()
        {
            this.Text = "aTrust in WSL Docker";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Resize += Form1_Resize;
        }

        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("还原", null, TrayRestore_Click);
            trayMenu.Items.Add("退出", null, TrayExit_Click);
            trayIcon = new NotifyIcon();
            trayIcon.Text = "aTrust安全客户端";
            trayIcon.Icon = this.Icon ?? SystemIcons.Application;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += TrayRestore_Click;
        }

        private void SetupTabControl()
        {
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;

            // Log Tab
            logTabPage = new TabPage("运行日志");
            SetupLogTab();

            tabControl.TabPages.Add(logTabPage);

            this.Controls.Add(tabControl);
        }

        private void SetupLogTab()
        {
            logTextBox.Multiline = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Dock = DockStyle.Fill;
            logTextBox.ReadOnly = true;
            logTextBox.BackColor = Color.Black;
            logTextBox.ForeColor = Color.White;

            logTabPage.Controls.Add(logTextBox);
        }

        private void StartDockerContainer()
        {
            try
            {
                AppendLog("正在启动Docker容器...\n");
                var stopProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wsl",
                        Arguments = "-u root --exec docker stop atrust",
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        RedirectStandardInput = false,
                        CreateNoWindow = true
                    }
                };
                stopProcess.Start();
                stopProcess.WaitForExit();
                stopProcess.Dispose();

                dockerProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wsl",
                        Arguments = "-u root --exec docker run --rm --name atrust --device /dev/net/tun --cap-add NET_ADMIN -i -e PASSWORD=sk19dj1fl1dj1ddfnm -e URLWIN=1 -e DISPLAY=:0 -v /tmp/.X11-unix:/tmp/.X11-unix -v /root/.atrust-data:/root -v /root/.atrust-data/usr/share/sangfor/.aTrust:/usr/share/sangfor/.aTrust -p 127.0.0.1:35901:5901 -p 127.0.0.1:31080:1080 -p 127.0.0.1:8888:8888 -p 127.0.0.1:54631:54631 --sysctl net.ipv4.conf.default.route_localnet=1 hagb/docker-atrust:vncless",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = false,
                        CreateNoWindow = true
                    }
                };
                dockerProcess.OutputDataReceived += DockerProcess_OutputDataReceived;
                dockerProcess.ErrorDataReceived += DockerProcess_ErrorDataReceived;
                dockerProcess.Start();
                dockerProcess.BeginOutputReadLine();
                dockerProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                AppendLog($"启动Docker容器失败: {ex.Message}\n");
            }
        }

        private void DockerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AppendLog(e.Data);
            }
        }

        private void DockerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AppendLog($"错误: {e.Data}");
            }
        }

        private void AppendLog(string message)
        {
            if (logTextBox == null || logTextBox.IsDisposed)
                return;
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action<string>(AppendLog), message + "\n\r");
            }
            else
            {
                logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n\r");
                logTextBox.ScrollToCaret();
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
        }

        private void MinimizeToTray()
        {
            this.Hide();
            trayIcon.Visible = true;
        }

        private void TrayRestore_Click(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void TrayExit_Click(object sender, EventArgs e)
        {
            isNeedClosed = true;
            ExitApplication();
        }

        private void ShowMainWindow()
        {
            // 托盘图标被点击时，执行命令激活软件界面
            try
            {
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wsl",
                        Arguments = "-u root --exec docker exec -it atrust /usr/share/sangfor/aTrust/aTrustTray --no-sandbox",
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        RedirectStandardInput = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.Dispose();
            }
            catch (Exception ex)
            {
                AppendLog($"执行命令失败: {ex.Message}\n");
            }
        }

        private void ExitApplication()
        {
            var stopProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wsl",
                    Arguments = "-u root --exec docker stop atrust",
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    RedirectStandardInput = false,
                    CreateNoWindow = true
                }
            };
            stopProcess.Start();
            stopProcess.WaitForExit();
            stopProcess.Dispose();

            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ExitApplication();
            base.OnFormClosed(e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetupTrayIcon();
            SetupForm();
            SetupTabControl();
            StartDockerContainer();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = !isNeedClosed;
            MinimizeToTray();
        }

        private void Form1_DpiChanged(object sender, DpiChangedEventArgs e)
        {
       
        }
    }
}