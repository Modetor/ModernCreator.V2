using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Windows.Media;

namespace ModernCreator_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public double DefaultWidthPercent { get; set; } = 0.65;
        public double DefaultHeightPercent { get; set; } = 0.80;
        public bool EnableWindowPercentBasedSize { get; set; } = true;


        public MainWindow()
        {
            

            ModernCreator_Server.Core.ConfigurationReader.Read("");
            InitializeComponent();
            webView.NavigationStarting += EnsureHttps;
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            InitializeAsync();

            //webView.CoreWebView2.NewWindowRequested += (e, s) => s.Handled = true;

            System.Diagnostics.Trace.WriteLine("\n\n\n\nFuck that!");
            
        }


        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            // in case i want to run some scripts...
            //webView.CoreWebView2.ExecuteScriptAsync($"alert('Initialized successfuly')");
            //loading_label.Visibility = Visibility.Hidden;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = true;

            webView.CoreWebView2.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
            };
            webView.CoreWebView2.NavigationStarting += (s, e) =>
            {
                if(!e.IsUserInitiated)
                {
                    webView.CoreWebView2.Stop();
                    webView.CoreWebView2.NavigateToString("<html><body><button>Click here..</button></body></html>");
                    //webView.CoreWebView2.ExecuteScriptAsync($"alert('No shit!!')");
                }
                //else
                    //webView.CoreWebView2.ExecuteScriptAsync($"alert('{e.Uri}  is mine :)')");
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
            await webView.EnsureCoreWebView2Async(null);
            //webView.CoreWebView2.Navigate("https://www.google.com/");
            webView.Visibility = Visibility.Visible;

            
            
            System.Console.WriteLine("Yesss");
        }

        async void InjectCodes()
        {
            await webView.CoreWebView2.ExecuteScriptAsync("window.addEventListener('dragover', e => e.preventDefault(), false);");

            await webView.CoreWebView2.ExecuteScriptAsync("window.addEventListener('drop',e => e.preventDefault(), false);");

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

            InjectCodes();
        }
        private void ButtonGo_Click(object sender, RoutedEventArgs e)
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                //webView.CoreWebView2.Navigate(addressBar.Text);
            }
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

        private void Button_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            webView.CoreWebView2.Navigate("http://127.0.0.1:80/");
        }
    }
}