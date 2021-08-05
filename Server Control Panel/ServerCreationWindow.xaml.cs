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
                ip = manual_input.Text + ":" + port;
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
                Modetor.Net.Server.Core.HttpServers.BaseServer s = Modetor.Net.Server.Core.HttpServers.BaseServer.InitializeServer(ip, Iport);
                s.Resumed += (s, e) =>
                {
                    MessageBox.Show(s.Address + " is resumed!");
                };
                s.Suspended += (s, e) =>
                {
                    MessageBox.Show(s.Address + " is suspended!");
                };
                if (null != s)
                {
                    s.Start();
                    new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                    .AddArgument("action", "viewConversation")
                    .AddArgument("conversationId", 9813)
                    .AddText("Server started")
                    .AddText(s.Address)
                    .Show();

                    DialogResult = true;
                    Close();
                }
                
            }
        }

    }
}
