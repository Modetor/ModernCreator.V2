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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeliveryServiceManagement
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

            ExtraInterfaceInitialization();
            
        }

        private void ExtraInterfaceInitialization()
        {
            Color c = ((SolidColorBrush)SystemParameters.WindowGlassBrush).Color;
            //c.A = 200;
            c.R = (byte)(c.R * 2.5);
            c.G = (byte)(c.G * 2.5);
            c.B = (byte)(c.B * 2.5);
            seperator.Background = new SolidColorBrush(c);
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
            menu.Focus();
            e.Handled = true;
        }

        private void Menu_LostFocus(object sender, RoutedEventArgs e)
        {
            menu.IsOpen = false;
        }

        private void menu_Exit_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void menu_Settings_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MessageBox.Show("Cock motherfucker");
        }

        private void Button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            UIElement element = (UIElement)sender;
            seperator.SetValue(Grid.ColumnProperty, (int)element.GetValue(Grid.ColumnProperty));
        }
    }
}
