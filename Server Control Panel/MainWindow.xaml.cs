using System;
using System.Windows;
using Modetor.Net.Server;
using Modetor.Net.Server.Core.Backbone;
using Modetor.Net.Server.Core.HttpServers;
using Logger = Modetor.Net.Server.Core.Backbone.ErrorLogger;

namespace Server_Control_Panel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Properties....
        internal BaseServer current_server;

        System.Windows.Media.Imaging.BitmapImage run_img = new(new System.Uri("pack://application:,,,/Server Control Panel;component/Resources/icons8_Circled_Play_48px.png"));
        System.Windows.Media.Imaging.BitmapImage stop_img = new(new System.Uri("pack://application:,,,/Server Control Panel;component/Resources/icons8_private_48px.png"));
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            ServerPrepare();
        }

        private void ServerPrepare()
        {
            Logger.Initialize();

            dynamic l = null;
            try { if (System.IO.File.Exists(".workplace")) l = Newtonsoft.Json.JsonConvert.DeserializeObject(System.IO.File.ReadAllText(".workplace")); }
            catch (Exception exp)
            {
                Logger.WithTrace(string.Format("[Fatel][Backend error => ReadFileBytesAction()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
            }

            if (l == null)
                return;


            Settings.SetSource(l);
        }

        private void RefreshAndFillIPListView()
        {
            ls_ips.SelectedItem = null;
            ls_ips.Items.Clear();
            //ls_ips.Items.Add("Choose Ip");

            foreach (BaseServer s in BaseServer.Servers.Values)
                ls_ips.Items.Add(s.Address);

            server_ctrl_btn.Cursor = ls_ips.Items.Count > 0 ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.No;
            kill_server_btn.Cursor = ls_ips.Items.Count > 0 ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.No;
        }



        private void Image_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            /***********************************************************************
                        IN CASE SOMETHING WENT WRONG.
                        current server pointing at null - when no server is selected
            ****************************************************************************/
            if (current_server == null)
                return;
            /***********************************************************************
                        IN CASE SOMETHING WENT WRONG.
                        server is registered but disposed(null ptr)
            ****************************************************************************/
            if (current_server.IsShutdown)
            {
                
                ls_ips.Items.Remove(current_server.Address);
                current_server.Shutdown(); 
                BaseServer.Servers.Remove(current_server.Address);
                return;
            }

            /***********************************************************************
                        IN CASE SERVER IS SUSPENDED
                        sleep server can easly resumed :)
            ****************************************************************************/
            if (current_server.IsSuspended)
            {
                current_server?.Resume();
                new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                .AddArgument("action", 0)
                .AddText("Server resumed")
                .AddText(current_server.Address)
                .AddButton("Stop", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, "action")
                .Show();
            }
            else
            {
                current_server?.Suspend();
                server_ctrl_btn.Source = stop_img;
                new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                .AddArgument("action", 1)
                .AddText("Server suspended")
                .AddText(current_server.Address)
                .AddButton("Start", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, "action")
                .Show();
            }
                
        }

        private void refresh_ips_btn_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) => RefreshAndFillIPListView();

        private void add_server_btn_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ServerCreationWindow wnd = new();
            wnd.Owner = this;
            bool? result = wnd.ShowDialog();
            if(result ?? false)
            {
                RefreshAndFillIPListView();
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (BaseServer.Servers.Count == 0) return;

            foreach (BaseServer server in BaseServer.Servers.Values)
                server?.Shutdown();

            BaseServer.Servers.Clear();
        }

        private void kill_server_btn_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        private void ls_ips_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ls_ips.SelectedIndex == -1 || ls_ips.SelectedItem == null)
                return;

            current_server = BaseServer.Servers[ls_ips.SelectedItem.ToString()];
        }
    }
}
