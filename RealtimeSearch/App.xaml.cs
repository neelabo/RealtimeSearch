// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;


namespace NeeLaboratory.RealtimeSearch
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static Config Config { get; private set; }

        public void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string message = string.Format(
                "システムエラーが発生しました。\n ({0} {1})\n\n{2}",
                e.Exception.GetType(), e.Exception.Message, e.Exception.StackTrace);
            MessageBox.Show(message);
            e.Handled = true;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Config = new Config();
            Config.Initialize(Assembly.GetEntryAssembly());

            // カレントフォルダ設定
            System.Environment.CurrentDirectory = Config.LocalApplicationDataPath;
        }
    }
}
