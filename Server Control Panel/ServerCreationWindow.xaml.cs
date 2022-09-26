using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Server_Control_Panel
{
    /// <summary>
    /// Interaction logic for ServerCreationWindow.xaml
    /// </summary>
    public partial class ServerCreationWindow : Window
    {
        public ServerCreationWindow()
        {
            InitializeComponent();

            string[] IPs = Modetor.Net.Server.Core.HttpServers.BaseServer.GetNeworkIPs();
            if (IPs == null)
                return;
            ips_cbox.SelectedItem = null;
            ips_cbox.Items.Clear();
            ips_cbox.Items.Add("Choose Ip");

            foreach (string s in IPs)
                ips_cbox.Items.Add(s);
        }

        private void create_btn_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!int.TryParse(port_input.Text, out int Iport))
            {
                MessageBox.Show("Invalid Port number", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string port = port_input.Text.Trim();
            string ip;
            if(manual_toggle.IsChecked ?? false)
                ip = manual_input.Text;
            else
            {
                if (ips_cbox.SelectedItem == null || ips_cbox.SelectedIndex <= 0)
                {
                    MessageBox.Show("Select IP firts!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ip = ips_cbox.SelectedItem.ToString();
            }


            if (!System.Net.IPAddress.TryParse(ip, out _))
                MessageBox.Show("Invalid IP value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                if(Modetor.Net.Server.Core.HttpServers.BaseServer.Servers.ContainsKey(ip + ":" + port))
                {
                    MessageBox.Show("There's a server running with the same address, consider using different Port number", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Modetor.Net.Server.Core.HttpServers.BaseServer s = Modetor.Net.Server.Core.HttpServers.BaseServer.InitializeServer(ip, Iport);
                
                if (null != s)
                {
                    s.StartError += S_StartError;
                    s.Start();
                    if(s.Active)
                    {
                        s.StartError -= S_StartError;
                       /* new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                        .AddText("Server started")
                        .AddText(s.Address)
                        .Show(t => t.ExpirationTime = DateTime.Now.AddSeconds(5));
*/
                        Tag = s;
                        DialogResult = true;
                        Close();
                    }
                    
                }
                else
                {
                    MessageBox.Show("Server encoutered an error while initializing.. consider using a valid IP", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
        }

        private void S_StartError(Modetor.Net.Server.Core.HttpServers.BaseServer server, EventArgs args)
        {
            MessageBox.Show("Server failed to start. Read log file at : \n"+ Modetor.Net.Server.Core.Backbone.ErrorLogger.LogFile, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void manual_toggle_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = manual_toggle.IsChecked ?? false;
            if (isChecked)
            {
                ips_cbox.IsEnabled = false;
                manual_input.IsEnabled = true;
            }
            else
            {
                ips_cbox.IsEnabled = true;
                manual_input.IsEnabled = false;
            }
        }
    }
}
