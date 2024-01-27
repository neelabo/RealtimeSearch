// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using NeeLaboratory.IO.Search.Diagnostics;
using NeeLaboratory.RealtimeSearch.Models;
using NeeLaboratory.RealtimeSearch.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
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
        private readonly PersistAndRestoreService _persistAndRestoreService;

        public App()
        {
            _persistAndRestoreService = new PersistAndRestoreService(new FileService());
        }

        public static ApplicationInfoService AppInfo { get; private set; } = new ApplicationInfoService();

        public static AppConfig AppConfig { get; private set; } = new AppConfig();


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppInfo.Initialize();

            try
            {
                // カレントフォルダ設定
                //System.Environment.CurrentDirectory = Config.LocalApplicationDataPath;

                // 設定ファイル読み込み
                var appConfig = _persistAndRestoreService.Load();
                appConfig?.Validate();

                if (appConfig is not null)
                {
                    AppConfig = appConfig;
                }

#if false
                // logger
                //var sw = new StreamWriter("TraceLog.txt");
                //sw.AutoFlush = true;
                //var tw = TextWriter.Synchronized(sw);
                //var twtl = new TextWriterTraceListener(tw, Development.Logger.TraceSource.Name);
                //Trace.Listeners.Add(twtl);

                var appTrace = Development.Logger.TraceSource;
                appTrace.Listeners.Remove("Default");
                var twtl = new TextWriterTraceListener("TraceLog.txt", "LogFile");
                appTrace.Listeners.Add(twtl);
                Development.Logger.SetLevel(SourceLevels.All);
                Development.Logger.Trace(System.Environment.NewLine + new string('=', 80));

                //Development.Logger.SetLevel(SourceLevels.All);
                //var twtl = new TextWriterTraceListener("TraceLog.txt", Development.Logger.TraceSource.Name);
                //Trace.Listeners.Add(twtl);
                //Trace.AutoFlush = true;
                //Trace.WriteLine(System.Environment.NewLine + new string('=', 80));
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppInfo.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                // 設定ファイル保存
                _persistAndRestoreService.Save(AppConfig);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppInfo.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
    }
}
