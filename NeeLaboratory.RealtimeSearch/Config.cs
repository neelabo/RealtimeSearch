﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace NeeLaboratory.RealtimeSearch
{
    /// <summary>
    /// アプリ全体の設定
    /// </summary>
    public class Config
    {
        /// <summary>
        /// 
        /// </summary>
        public string AssemblyLocation { get; private set; }

        /// <summary>
        /// 会社名
        /// </summary>
        public string CompanyName { get; private set; }

        /// <summary>
        /// プロダクト名
        /// </summary>
        public string ProductName { get; private set; }

        /// <summary>
        /// プロダクトバージョン
        /// </summary>
        public string ProductVersion { get; private set; }

        /// <summary>
        /// プロダクトバージョン(int)
        /// </summary>
        public int ProductVersionNumber { get; private set; }

        //
        public static int GenerateProductVersionNumber(int major, int minor, int build)
        {
            return major << 16 | minor << 8 | build;
        }

        /// <summary>
        /// いろいろ初期化
        /// </summary>
        public void Initialize(Assembly assembly)
        {
            //var assembly = Assembly.GetEntryAssembly();
            ValidateProductInfo(assembly);
        }

        /// <summary>
        /// アセンブリ情報収集
        /// </summary>
        /// <param name="asm"></param>
        private void ValidateProductInfo(Assembly asm)
        {
            // パス
            AssemblyLocation = Path.GetDirectoryName(asm.Location);

            // 会社名
            AssemblyCompanyAttribute companyAttribute = Attribute.GetCustomAttribute(asm, typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
            CompanyName = companyAttribute.Company;

            // タイトル
            AssemblyTitleAttribute titleAttribute = Attribute.GetCustomAttribute(asm, typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute;
            ProductName = titleAttribute.Title;

            // バージョンの取得
            var version = asm.GetName().Version;
            if (version.Build == 0)
            {
                ProductVersion = $"{version.Major}.{version.Minor}";
                ProductVersionNumber = GenerateProductVersionNumber(version.Major, version.Minor, 0);
            }
            else
            {
                ProductVersion = $"{version.Major}.{version.Minor}.{version.Build}";
                ProductVersionNumber = GenerateProductVersionNumber(version.Major, version.Minor, version.Build);
            }
        }


        /// <summary>
        /// ユーザデータフォルダ
        /// </summary>
        private string _localApplicationDataPath;
        public string LocalApplicationDataPath
        {
            get
            {
                if (_localApplicationDataPath == null)
                {
                    // configファイルの設定で LocalApplicationData を使用するかを判定。インストール版用
                    if (IsUseLocalApplicationDataFolder)
                    {
                        _localApplicationDataPath = GetFileSystemPath(Environment.SpecialFolder.LocalApplicationData, true);
                    }
                    else
                    {
                        _localApplicationDataPath = AssemblyLocation;
                    }
                }
                return _localApplicationDataPath;
            }
        }

        /// <summary>
        /// フォルダパス生成(特殊フォルダ用)
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private string GetFileSystemPath(Environment.SpecialFolder folder, bool createFolder)
        {
            string path = System.IO.Path.Combine(Environment.GetFolderPath(folder), CompanyName, ProductName);
            if (createFolder && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private string GetFileSystemCompanyPath(Environment.SpecialFolder folder, bool createFolder)
        {
            string path = System.IO.Path.Combine(Environment.GetFolderPath(folder), CompanyName);
            if (createFolder && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        // データ保存にアプリケーションデータフォルダを使用するか
        private bool? _isUseLocalApplicationDataFolder;
        public bool IsUseLocalApplicationDataFolder
        {
            get
            {
                if (_isUseLocalApplicationDataFolder == null)
                {
                    _isUseLocalApplicationDataFolder = System.Configuration.ConfigurationManager.AppSettings["UseLocalApplicationData"] == "True";
                }
                return (bool)_isUseLocalApplicationDataFolder;
            }
        }

        // パッケージの種類(拡張子)
        private string _packageType;
        public string PackageType
        {
            get
            {
                if (_packageType == null)
                {
                    _packageType = ConfigurationManager.AppSettings["PackageType"];
                    if (_packageType != ".msi") _packageType = ".zip";
                }
                return _packageType;
            }
        }


        // 全ユーザデータ削除
        private bool RemoveApplicationDataCore()
        {
            // LocalApplicationDataフォルダを使用している場合のみ
            if (!IsUseLocalApplicationDataFolder) return false;

            Debug.WriteLine("RemoveAllApplicationData ...");

            var productFolder = GetFileSystemPath(Environment.SpecialFolder.LocalApplicationData, false);
            Directory.Delete(LocalApplicationDataPath, true);
            System.Threading.Thread.Sleep(500);

            var companyFolder = GetFileSystemCompanyPath(Environment.SpecialFolder.LocalApplicationData, false);
            if (Directory.GetFileSystemEntries(companyFolder).Length == 0)
            {
                Directory.Delete(companyFolder);
            }

            Debug.WriteLine("RemoveAllApplicationData done.");
            return true;
        }

        //
        public event EventHandler LocalApplicationDataRemoved;

        //
        public void RemoveApplicationData()
        {
            if (!this.IsUseLocalApplicationDataFolder)
            {
                MessageBox.Show("--removeオプションはインストーラー版でのみ機能します", "起動オプションエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var text = "ユーザデータを削除します。よろしいですか？";
            var result = MessageBox.Show(text, $"{ProductName} - データ削除確認", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            if (result == MessageBoxResult.OK)
            {
                // 削除できないのでカレントフォルダ移動
                var currentFolder = System.Environment.CurrentDirectory;
                System.Environment.CurrentDirectory = this.AssemblyLocation;

                try
                {
                    this.RemoveApplicationDataCore();
                    MessageBox.Show($"ユーザデータを削除しました。{ProductName}を終了します。", $"{ProductName} - 完了");
                    LocalApplicationDataRemoved?.Invoke(this, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, $"{ProductName} - エラー", MessageBoxButton.OK, MessageBoxImage.Error);

                    // カレントフォルダ復帰
                    System.Environment.CurrentDirectory = currentFolder;
                }
            }
        }
    }
}
