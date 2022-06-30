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
        private PersistAndRestoreService _persistAndRestoreService;

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
                if (appConfig is not null)
                {
                    AppConfig = appConfig;
                }
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
