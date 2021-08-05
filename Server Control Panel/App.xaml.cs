﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Server_Control_Panel
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ToastNotificationManagerCompat.OnActivated += t =>
            {
                ToastArguments args = ToastArguments.Parse(t.Argument);
                //t.Use
                Current.Dispatcher.Invoke(delegate
                {
                    MessageBox.Show(args.Get("action"));
                });
            };
        }

    }
}
