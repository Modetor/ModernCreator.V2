using Microsoft.Web.WebView2.Core;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Modetor_MCC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            InitializeAsync();


            Closing += (s, e) =>
            {
                webView.Dispose();
            };
        }


        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = true;

            webView.CoreWebView2.NavigationStarting += (s, e) =>
            {
                Console.WriteLine("URI = " + e.Uri);
                if (!e.IsUserInitiated)
                {
                    //webView.CoreWebView2.Stop();
                    //webView.Source = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"assest\initf.html"));
                }
               
            };

            webView.CoreWebView2.ProcessFailed += (s, e) =>
            {
                System.Diagnostics.Trace.WriteLine("Stock shit!");
            };
            webView.CoreWebView2.PermissionRequested += (s, e) =>
            {
                e.State = e.PermissionKind == CoreWebView2PermissionKind.UnknownPermission ? CoreWebView2PermissionState.Deny : CoreWebView2PermissionState.Allow;
            };
        }

        async void InitializeAsync()
        {
            Core.ClientConnection.Initialize();
            await webView.EnsureCoreWebView2Async(null);

            Core.ClientConnection conn = Core.ClientConnection.Reference;
            if(conn.ErrorMessage.Equals(string.Empty))
            {
                
                (bool, string) task = await conn.SendStringAsync(0, "check state", string.Empty);
                if (task.Item1)
                {
                    Dispatcher.Invoke(delegate
                    {
                        //
                        webView.Source = new Uri($"http://{conn.ServerIP}:{Properties.Settings.Default.ServerPort}/");
                    });

                }
                else
                {
                    Dispatcher.Invoke(delegate
                    {
                        webView.Source = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"assest\initf.html"));
                    });
                }
            }
            else
            {
                Core.ErrorLogger.WithTrace(conn.ErrorMessage, GetType());
                Dispatcher.Invoke(delegate
                {
                    string path = System.IO.Path.Combine(Environment.CurrentDirectory, @"assest\initfd.html");
                    webView.Source = new UriBuilder(path) { Query = "msg=" + conn.ErrorMessage }.Uri;
                });
            }

        }

        private async void InjectCodes()
        {
            //await webView.CoreWebView2.ExecuteScriptAsync("window.addEventListener('dragover', e => e.preventDefault(), false);");

            //await webView.CoreWebView2.ExecuteScriptAsync("window.addEventListener('drop',e => e.preventDefault(), false);");

            //await webView.CoreWebView2.ExecuteScriptAsync("window.addEventListener('contextmenu', window => {window.preventDefault();});");
        }

        private void EnsureHttps(object sender, CoreWebView2NavigationStartingEventArgs args)
        {
            string uri = args.Uri;
            if (!uri.StartsWith("https://"))
            {
                //webView.CoreWebView2.ExecuteScriptAsync($"alert('{uri} is not safe, try an https link')");
                //args.Cancel = true;
            }

            //InjectCodes();
        }

        private void WebView_PreviewDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
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

        private void x2_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            menu.PlacementTarget = x2;
            menu.IsOpen = true;
            e.Handled = true;
        }
        private void menu_Exit_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void menu_Settings_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AppSettings settings = new();
            settings.Owner = this;
            settings.ShowDialog();
        }
        private void menu_ViewSettings_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MessageBox.Show(Properties.Settings.Default.AppVersion, "App version", MessageBoxButton.OK);
        }
    }
}
