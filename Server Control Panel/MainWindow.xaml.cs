using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
        System.Timers.Timer timer;
        System.Windows.Media.Imaging.BitmapImage run_img = new(new System.Uri("pack://application:,,,/Server Control Panel;component/Resources/icons8_Circled_Play_48px.png"));
        System.Windows.Media.Imaging.BitmapImage stop_img = new(new System.Uri("pack://application:,,,/Server Control Panel;component/Resources/icons8_private_48px.png"));
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            ServerPrepare();
            BindShortcuts();
        }

        private void BindShortcuts()
        {
            BindShortcut(Key.F2, ModifierKeys.None, (s,e) => { add_server_btn_PreviewMouseLeftButtonUp(s, null); });
            BindShortcut(Key.F3, ModifierKeys.None, (s, e) => { Image_PreviewMouseLeftButtonUp(s, null); });
            BindShortcut(Key.F4, ModifierKeys.None, (s,e) => { kill_server_btn_PreviewMouseLeftButtonUp(s, null); });
            BindShortcut(Key.F5, ModifierKeys.None, (s,e) => { refresh_ips_btn_PreviewMouseLeftButtonUp(s, null); });
        }
        private void BindShortcut(Key key, ModifierKeys modifiers, ExecutedRoutedEventHandler executed)
        {
            RoutedCommand cmd = new();
            _ = cmd.InputGestures.Add(new KeyGesture(key, modifiers));
            _ = CommandBindings.Add(new CommandBinding(cmd, executed));
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

            /// Bind timer...
            /// 
            timer = new();
           
            timer.Interval = Properties.Settings.Default.StatusBarRefreshInterval;
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();
            timer.Enabled = false;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (ls_ips.Items.Count != BaseServer.Servers.Count)
                Dispatcher.Invoke(delegate { RefreshAndFillIPListView(); }); 

            int active, sleep;
            active = sleep = 0;
            foreach(BaseServer server in BaseServer.Servers.Values)
            {
                if (server.IsShutdown) continue;
                if (server.IsSuspended)
                    sleep++;
                else
                    active++;
            }

            Dispatcher.Invoke(delegate {
                statusbar_item_active_servers.Text = $"Active Servers : {active}";
                statusbar_item_inactive_servers.Text = $"Inactive Servers : {sleep}";
                statusbar_item_created_servers.Text = $"Created Servers : {BaseServer.Servers.Count}";
            });
            
        }

        private void RefreshAndFillIPListView()
        {
            ls_ips.SelectedItem = null;
            ls_ips.Items.Clear();
            //ls_ips.Items.Add("Choose Ip");

            foreach (BaseServer s in BaseServer.Servers.Values)
                ls_ips.Items.Add(s.Address);

            server_ctrl_btn.Cursor = ls_ips.Items.Count > 0 ? Cursors.Hand : Cursors.No;
            kill_server_btn.Cursor = ls_ips.Items.Count > 0 ? Cursors.Hand : Cursors.No;
        }



        private void Image_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(apply_to_all_btn.IsChecked ?? false)
            {
                ChangeAllServersState();
                return;
            }
            /***********************************************************************
                        IN CASE SOMETHING WENT WRONG.
                        current server pointing at null - when no server is selected
            ****************************************************************************/
            if (current_server == null)
            {
                MessageBox.Show("select server to perform this operation", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
                
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
                .AddText("Server " + current_server.Address + " Resumed")
                .AddText("Press on button below to change state to 'suspend'")
                .AddButton(
                    new Microsoft.Toolkit.Uwp.Notifications.ToastButton()
                    .AddArgument("action", 0)
                    .AddArgument("si", current_server.Address)
                    .SetContent("Suspend")
                    .SetBackgroundActivation()
                )
                .Show(t => t.ExpirationTime = DateTime.Now.AddSeconds(4));
            }
            else
            {
                current_server?.Suspend();
                
                new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                .AddText("Server "+ current_server.Address + " Suspended")
                .AddText("Press on button below to change state to 'resume'")
                .AddButton(
                    new Microsoft.Toolkit.Uwp.Notifications.ToastButton()
                    .AddArgument("action", 1)
                    .AddArgument("si", current_server.Address)
                    .SetContent("Resume")
                    .SetBackgroundActivation()
                )
                //.AddButton("Start", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Background, "action")
                .Show(t => t.ExpirationTime = DateTime.Now.AddSeconds(4));
            }
                
        }

        private void ChangeAllServersState()
        {
            if(server_ctrl_btn.Source.Equals(stop_img))
            {
                /// STOP ALL SERVERS
                /// 
                foreach (BaseServer server in BaseServer.Servers.Values)
                    server.Suspend();

                server_ctrl_btn.Source = run_img;
            }
            else
            {
                /// RUN ALL SERVERS
                /// 
                foreach (BaseServer server in BaseServer.Servers.Values)
                    server.Resume();
                server_ctrl_btn.Source = stop_img;
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
                if (BaseServer.Servers.Count == 1)
                    timer.Enabled = true;

                if (wnd.Tag is BaseServer)
                {
                    RefreshAndFillIPListView();

                    BaseServer s = wnd.Tag as BaseServer;
                    s.Resumed += S_Resumed;
                    s.Suspended += S_Suspended;

                    if (Properties.Settings.Default.AutoSelectLastCreatedServer)
                        ls_ips.SelectedItem = s.Address;

                }
            }

        }

        private void S_Suspended(BaseServer server, EventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(server.Address + " is suspended!");
            server_ctrl_btn.Source = run_img;
        }

        private void S_Resumed(BaseServer server, EventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(server.Address + " is resumed!");
            server_ctrl_btn.Source = stop_img;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            kill_all_servers();
            
        }

        private void kill_server_btn_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(apply_to_all_btn.IsChecked ?? false)
            {
                //..logic here
                kill_all_servers();

            }
            else
            {
                if(current_server == null)
                {
                    MessageBox.Show("select server to perform this operation", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                current_server?.Shutdown();
                ls_ips.Items.Remove(current_server?.Address);
                BaseServer.Servers.Remove(current_server?.Address ?? string.Empty);
                current_server = null;
            }
        }

        private void kill_all_servers()
        {
            if (BaseServer.Servers.Count == 0) return;
            
            foreach (BaseServer server in BaseServer.Servers.Values)
            {
                server?.Shutdown();
                ls_ips.Items.Remove(server?.Address);
            }

            BaseServer.Servers.Clear();
            current_server = null;
            timer.Enabled = false;
            Timer_Elapsed(null, null);
        }

        private void ls_ips_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ls_ips.SelectedIndex == -1 || ls_ips.SelectedItem == null)
                return;

            current_server = BaseServer.Servers[ls_ips.SelectedItem.ToString()];
            server_ctrl_btn.Source = current_server.IsSuspended ? run_img : stop_img;
        }























        #region Window Events
        //Application.Current.Shutdown();
        private void CloseEvent(object sender, System.Windows.Input.MouseButtonEventArgs e) => SystemCommands.CloseWindow(this);
        private void MaximizeEvent(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                SystemCommands.RestoreWindow(this);
            else if (WindowState == WindowState.Normal)
                SystemCommands.MaximizeWindow(this);
            else if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);


        }
        private void MinimizeEvent(object sender, System.Windows.Input.MouseButtonEventArgs e) => SystemCommands.MinimizeWindow(this);
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                Titlebar.Height = 35;
            else if (WindowState == WindowState.Normal)
                Titlebar.Height = 32;
        }
        private void SizeButtons_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Controls.Label self = sender as System.Windows.Controls.Label;
            self.Background = Brushes.Transparent;
            if (self.Name == "CloseButton")
                self.Foreground = Brushes.Red;
            else
                self.Foreground = Brushes.Black;

            e.Handled = true;
        }
        private void SizeButtons_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Controls.Label self = sender as System.Windows.Controls.Label;
            if (self.Name == "CloseButton")
            {
                self.Background = Brushes.Red;
                self.Foreground = Brushes.White;
            }
            else
            {
                Color c = ((SolidColorBrush)SystemParameters.WindowGlassBrush).Color;
                //c.A = 200;
                c.R = (byte)(c.R * 2.5);
                c.G = (byte)(c.G * 2.5);
                c.B = (byte)(c.B * 2.5);
                self.Background = new SolidColorBrush(c);
                self.Foreground = Brushes.White;
            }

            e.Handled = true;
        }


        #endregion

    }
}
