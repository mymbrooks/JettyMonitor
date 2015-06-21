using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JettyMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FolderBrowserDialog folder;
        private Regex reg;
        public MainWindow()
        {
            InitializeComponent();
            folder = new FolderBrowserDialog();
            reg = new Regex(@"^\d+$");
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (EnvironmentUtil.CheckSysEnvironmentNameExist("JAVA_HOME"))
            {
                this.txtJavaHome.Text = EnvironmentUtil.GetSysEnvironmentByName("JAVA_HOME");
            }

            if (EnvironmentUtil.CheckSysEnvironmentNameExist("JETTY_HOME"))
            {
                this.txtJettyHome.Text = EnvironmentUtil.GetSysEnvironmentByName("JETTY_HOME");
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

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {

        }

        private bool Validate()
        {
            if (!File.Exists(this.txtJavaHome.Text))
            {
                this.labMessage.Content = "Java Home 不存在，请重新选择！";
                return false;
            }

            if (!File.Exists(this.txtJettyHome.Text))
            {
                this.labMessage.Content = "Jetty Home 不存在，请重新选择！";
                return false;
            }

            if (!File.Exists(this.txtJettyBase.Text))
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

        private void btnCopyCommand_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnClearCommand_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnCopyResult_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnClearResult_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}