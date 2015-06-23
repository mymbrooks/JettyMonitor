using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;

namespace JettyMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FolderBrowserDialog folder;
        private Regex reg;
        private Configuration config;
        private Process startProcess;
        private NotifyIcon notifyIcon;

        public delegate void DelReadStdOutput(string result);
        public delegate void DelReadErrOutput(string result);

        public event DelReadStdOutput ReadStdOutput;
        public event DelReadErrOutput ReadErrOutput;

        private delegate void GetResult();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            this.folder = new FolderBrowserDialog();
            this.reg = new Regex(@"^\d+$");
            this.config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            this.notifyIcon = new NotifyIcon();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            this.notifyIcon.Text = "JettyMonitor";
            this.notifyIcon.Icon = new Icon("logo.ico");
            this.notifyIcon.Visible = false;
            this.notifyIcon.Click += notifyIcon_Click;

            ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
            MenuItem menuItemStart = new MenuItem("启动Jetty");
            menuItemStart.Click += menuItemStart_Click;
            contextMenu.MenuItems.Add(menuItemStart);

            MenuItem menuItemStop = new MenuItem("停止Jetty");
            menuItemStop.Click += menuItemStop_Click;
            contextMenu.MenuItems.Add(menuItemStop);

            MenuItem menuItemExit = new MenuItem("退出");
            menuItemExit.Click += menuItemExit_Click;
            contextMenu.MenuItems.Add(menuItemExit);

            this.notifyIcon.ContextMenu = contextMenu;

            ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
            ReadErrOutput += new DelReadErrOutput(ReadErrOutputAction);

            if (string.IsNullOrWhiteSpace(config.AppSettings.Settings["JavaHome"].Value))
            {
                if (EnvironmentUtil.CheckSysEnvironmentNameExist("JAVA_HOME"))
                {
                    this.txtJavaHome.Text = EnvironmentUtil.GetSysEnvironmentByName("JAVA_HOME");
                }
            }
            else
            {
                this.txtJavaHome.Text = config.AppSettings.Settings["JavaHome"].Value;
            }

            this.txtJettyHome.Text = config.AppSettings.Settings["JettyHome"].Value;
            this.txtJettyBase.Text = config.AppSettings.Settings["JettyBase"].Value;
            this.txtLocalPort.Text = config.AppSettings.Settings["LocalPort"].Value;
            this.txtRemotePort.Text = config.AppSettings.Settings["RemotePort"].Value;
            this.txtCommand.Text = config.AppSettings.Settings["Command"].Value;
        }

        private void menuItemStart_Click(object sender, EventArgs e)
        {
            this.StartJetty();
        }

        void menuItemStop_Click(object sender, EventArgs e)
        {
            this.StopJetty();
        }

        void menuItemExit_Click(object sender, EventArgs e)
        {
            this.notifyIcon.Visible = true;
            this.Close();
        }

        void notifyIcon_Click(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                this.WindowState = System.Windows.WindowState.Minimized;
                this.ShowInTaskbar = true;
                this.notifyIcon.Visible = true;
            }
            else
            {
                this.WindowState = System.Windows.WindowState.Normal;
                this.ShowInTaskbar = false;
                this.notifyIcon.Visible = false;
            }
        }

        private void btnJavaBrowser_Click(object sender, RoutedEventArgs e)
        {
            folder.ShowDialog();
            if (!string.IsNullOrWhiteSpace(folder.SelectedPath))
            {
                this.txtJavaHome.Text = folder.SelectedPath;
            }
        }

        private void btnJettyHomeBrowser_Click(object sender, RoutedEventArgs e)
        {
            folder.ShowDialog();
            if (!string.IsNullOrWhiteSpace(folder.SelectedPath))
            {
                this.txtJettyHome.Text = folder.SelectedPath;
            }
        }

        private void btnJettyBaseBrowser_Click(object sender, RoutedEventArgs e)
        {
            folder.ShowDialog();
            if (!string.IsNullOrWhiteSpace(folder.SelectedPath))
            {
                this.txtJettyBase.Text = folder.SelectedPath;
            }
        }

        private bool Validate()
        {
            if (!Directory.Exists(this.txtJavaHome.Text))
            {
                this.labMessage.Content = "Java Home 不存在，请重新选择！";
                return false;
            }

            if (!Directory.Exists(this.txtJettyHome.Text))
            {
                this.labMessage.Content = "Jetty Home 不存在，请重新选择！";
                return false;
            }

            if (!Directory.Exists(this.txtJettyBase.Text))
            {
                this.labMessage.Content = "Jetty Base 不存在，请重新选择！";
                return false;
            }

            //本地端口不允许为空
            if (string.IsNullOrWhiteSpace(this.txtLocalPort.Text) || !reg.IsMatch(this.txtLocalPort.Text) || Int32.Parse(this.txtLocalPort.Text) > 65535)
            {
                this.labMessage.Content = "端口范围： 0 -- 65535！";
                return false;
            }

            //远程端口可为空，即可以不启用Jetty的远程调试
            if (!string.IsNullOrWhiteSpace(this.txtRemotePort.Text) && (!reg.IsMatch(this.txtRemotePort.Text) || Int32.Parse(this.txtRemotePort.Text) > 65535))
            {
                this.labMessage.Content = "端口范围： 0 -- 65535！";
                return false;
            }

            return true;
        }

        private void ReadStdOutputAction(string result)
        {
            this.txtResult.Text += (result + "\r\n");
            this.sv.ScrollToBottom();
        }

        private void ReadErrOutputAction(string result)
        {
            this.txtResult.Text += (result + "\r\n");
            this.sv.ScrollToBottom();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            this.SaveConfig(this.config);
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            this.StartJetty();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            this.StopJetty();
        }

        private void startProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                this.Dispatcher.Invoke(ReadErrOutput, new object[] { e.Data });
            }
        }

        void startProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                this.Dispatcher.Invoke(ReadStdOutput, new object[] { e.Data });
            }
        }

        void stopProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                this.Dispatcher.Invoke(ReadStdOutput, new object[] { e.Data });
            }
        }

        void stopProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                this.Dispatcher.Invoke(ReadErrOutput, new object[] { e.Data });
            }
        }

        private void btnCopyCommand_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.DataObject dataObject = new System.Windows.DataObject();
            dataObject.SetData(System.Windows.DataFormats.UnicodeText, this.txtCommand.Text);
            System.Windows.Clipboard.SetDataObject(dataObject);

            this.labMessage.Content = "配置命令行已成功复制！";
        }

        private void btnClearCommand_Click(object sender, RoutedEventArgs e)
        {
            this.txtCommand.Text = "";
            this.labMessage.Content = "配置命令行已清空！";
        }

        private void btnCopyResult_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.DataObject dataObject = new System.Windows.DataObject();
            dataObject.SetData(System.Windows.DataFormats.UnicodeText, this.txtResult.Text);
            System.Windows.Clipboard.SetDataObject(dataObject);

            this.labMessage.Content = "执行结果已成功复制！";
        }

        private void btnClearResult_Click(object sender, RoutedEventArgs e)
        {
            this.txtResult.Text = "";
            this.labMessage.Content = "执行结果已清空！";
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void SaveConfig(Configuration config)
        {
            StreamWriter sw = null;
            FileStream fileStream = null;
            try
            {
                if (Validate())
                {
                    config.AppSettings.Settings["JavaHome"].Value = this.txtJavaHome.Text;
                    config.AppSettings.Settings["JettyHome"].Value = this.txtJettyHome.Text;
                    config.AppSettings.Settings["JettyBase"].Value = this.txtJettyBase.Text;
                    config.AppSettings.Settings["LocalPort"].Value = this.txtLocalPort.Text;
                    config.AppSettings.Settings["RemotePort"].Value = this.txtRemotePort.Text;

                    if (string.IsNullOrWhiteSpace(this.txtCommand.Text))
                    {
                        if (string.IsNullOrWhiteSpace(config.AppSettings.Settings["Command"].Value))
                        {
                            StringBuilder sbCommand = new StringBuilder();
                            if (string.IsNullOrWhiteSpace(this.txtRemotePort.Text))
                            {
                                sbCommand.AppendLine(@"cd /d " + this.txtJettyBase.Text);
                                sbCommand.AppendLine("\"" + this.txtJavaHome.Text + "\\bin\\java.exe\" -jar " + this.txtJettyHome.Text + @"\start.jar --add-to-startd=http,deploy,spring,servlet,servlets,webapp,jsp,jstl,server");
                                sbCommand.AppendLine("\"" + this.txtJavaHome.Text + "\\bin\\java.exe\" " + this.txtJettyHome.Text + @"\start.jar jetty.http.port=" + this.txtLocalPort.Text);
                            }
                            else
                            {
                                sbCommand.AppendLine(@"cd /d " + this.txtJettyBase.Text);
                                sbCommand.AppendLine("\"" + this.txtJavaHome.Text + "\\bin\\java.exe\" -jar " + this.txtJettyHome.Text + @"\start.jar --add-to-startd=http,deploy,spring,servlet,servlets,webapp,jsp,jstl,server");
                                sbCommand.AppendLine("\"" + this.txtJavaHome.Text + "\\bin\\java.exe\" -Xdebug -agentlib:jdwp=transport=dt_socket,address=" + this.txtRemotePort.Text + ",server=y,suspend=n -jar " + this.txtJettyHome.Text + @"\start.jar jetty.http.port=" + this.txtLocalPort.Text);
                            }

                            this.txtCommand.Text = sbCommand.ToString();
                            config.AppSettings.Settings["Command"].Value = sbCommand.ToString();
                        }
                        else
                        {
                            this.txtCommand.Text = config.AppSettings.Settings["Command"].Value;
                        }
                    }
                    else
                    {
                        config.AppSettings.Settings["Command"].Value = this.txtCommand.Text;
                    }

                    config.Save();
                    ConfigurationManager.RefreshSection("appSettings");

                    fileStream = File.Create(AppDomain.CurrentDomain.BaseDirectory + @"\runjetty.bat");
                    sw = new StreamWriter(fileStream);
                    sw.Write(this.txtCommand.Text);

                    sw.Flush();
                    this.labMessage.Content = "保存配置成功！";
                }
            }
            catch (Exception ex)
            {
                this.labMessage.Content = ex.Message;
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
        }

        private void StartJetty()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.txtCommand.Text))
                {
                    this.labMessage.Content = "请先保存配置！";
                    return;
                }

                this.SaveConfig(this.config);

                this.txtResult.Text = "";

                startProcess = new Process();
                startProcess.StartInfo.FileName = @"cmd";
                startProcess.StartInfo.Arguments = @"/c " + AppDomain.CurrentDomain.BaseDirectory + @"\runjetty.bat";
                startProcess.StartInfo.UseShellExecute = false;
                startProcess.StartInfo.CreateNoWindow = true;
                startProcess.StartInfo.RedirectStandardOutput = true;
                startProcess.StartInfo.RedirectStandardError = true;
                startProcess.OutputDataReceived += startProcess_OutputDataReceived;
                startProcess.ErrorDataReceived += startProcess_ErrorDataReceived;
                startProcess.Start();
                startProcess.BeginOutputReadLine();
                startProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                this.labMessage.Content = ex.Message;
            }
        }

        private void StopJetty()
        {
            Process stopProcess = new Process();
            try
            {
                if (startProcess == null)
                {
                    this.labMessage.Content = "请先启动服务！";
                }
                else
                {
                    this.txtResult.Text = "";

                    stopProcess.StartInfo.FileName = @"cmd";
                    stopProcess.StartInfo.Arguments = "/c \"TASKKILL /F /PID " + startProcess.Id + " /T\"";
                    stopProcess.StartInfo.UseShellExecute = false;
                    stopProcess.StartInfo.CreateNoWindow = true;
                    stopProcess.StartInfo.RedirectStandardOutput = true;
                    stopProcess.StartInfo.RedirectStandardError = true;
                    stopProcess.ErrorDataReceived += stopProcess_ErrorDataReceived;
                    stopProcess.OutputDataReceived += stopProcess_OutputDataReceived;
                    stopProcess.Start();
                    stopProcess.BeginOutputReadLine();
                    stopProcess.BeginErrorReadLine();

                    this.labMessage.Content = "停止Jetty成功！";
                }
            }
            catch (Exception ex)
            {
                this.labMessage.Content = ex.Message;
            }
            finally
            {
                if (startProcess != null)
                {
                    startProcess.Close();
                }

                if (stopProcess != null)
                {
                    stopProcess.Close();
                }
            }
        }

        private bool Exit()
        {            
            if (System.Windows.MessageBox.Show("确定要退出吗？这将停止Jetty。", "退出确认", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes)
            {
                this.StopJetty();
                this.notifyIcon.Visible = false;
                return true;
            }
            else
            {
                this.notifyIcon.Visible = true;
                return false;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!this.notifyIcon.Visible && this.WindowState != System.Windows.WindowState.Minimized)
            {
                this.WindowState = System.Windows.WindowState.Minimized;
                this.ShowInTaskbar = true;
                this.notifyIcon.Visible = true;

                e.Cancel = true;
            }
            else
            {
                if (!this.Exit())
                {
                    e.Cancel = true;
                }
            }
        }
    }
}