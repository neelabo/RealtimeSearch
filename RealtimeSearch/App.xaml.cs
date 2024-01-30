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
        private AppModel? _appModel;

        public App()
        {
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            _appModel = AppModel.Instance;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            _appModel?.Dispose();
        }
    }
}
