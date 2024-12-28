using System;
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

namespace NeeLaboratory.RealtimeSearch.Services
{
    /// <summary>
    /// アプリ全体の設定
    /// </summary>
    public class ApplicationInfo
    {
        public static ApplicationInfo Current { get; } = new ApplicationInfo();

        private string? _localApplicationDataPath;
        private bool? _isUseLocalApplicationDataFolder;
        private string? _packageType;


        private ApplicationInfo()
        {
            ProcessModule? module = Process.GetCurrentProcess().MainModule;
            if (module is null) throw new InvalidOperationException();

            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            ValidateProductInfo(assembly, module);
        }


        /// <summary>
        /// アセンブリの場所
        /// </summary>
        public string AssemblyLocation { get; private set; } = "";

        /// <summary>
        /// 会社名
        /// </summary>
        public string CompanyName { get; private set; } = "";

        /// <summary>
        /// プロダクト名
        /// </summary>
        public string ProductName { get; private set; } = "";

        /// <summary>
        /// プロダクトバージョン
        /// </summary>
        public string ProductVersion { get; private set; } = "";

        /// <summary>
        /// プロダクトフルバージョン
        /// </summary>
        public string ProductFullVersion { get; private set; } = "";

        /// <summary>
        /// プロダクトバージョン(int)
        /// </summary>
        public int ProductVersionNumber { get; private set; }

        /// <summary>
        /// ユーザデータフォルダ
        /// </summary>
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
                        _localApplicationDataPath = Path.Combine(AssemblyLocation, "Profile");
                        CreateFolder(_localApplicationDataPath);
                    }
                }
                return _localApplicationDataPath;
            }
        }

        // データ保存にアプリケーションデータフォルダを使用するか
        public bool IsUseLocalApplicationDataFolder
        {
            get
            {
                if (_isUseLocalApplicationDataFolder == null)
                {
                    _isUseLocalApplicationDataFolder = AppConfig.Current.UseLocalApplicationData;
                }
                return (bool)_isUseLocalApplicationDataFolder;
            }
        }

        // パッケージの種類(拡張子)
        public string PackageType
        {
            get
            {
                if (_packageType == null)
                {
                    _packageType = AppConfig.Current.PackageType ?? "";
                    if (_packageType != ".msi") _packageType = ".zip";
                }
                return _packageType;
            }
        }


        /// <summary>
        /// プロダクトバージョン生成
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public int GenerateProductVersionNumber(int major, int minor, int build)
        {
            return major << 16 | minor << 8 | build;
        }

        /// <summary>
        /// アセンブリ情報収集
        /// </summary>
        /// <param name="asm"></param>
        private void ValidateProductInfo(Assembly asm, ProcessModule module)
        {
            // パス
            AssemblyLocation = Path.GetDirectoryName(module.FileName) ?? "";

            // 会社名
            AssemblyCompanyAttribute? companyAttribute = Attribute.GetCustomAttribute(asm, typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
            CompanyName = companyAttribute?.Company ?? "NeeLaboratory";

            // タイトル
            AssemblyTitleAttribute? titleAttribute = Attribute.GetCustomAttribute(asm, typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute;
            ProductName = titleAttribute?.Title ?? "RealtimeSearch";

            // バージョンの取得
            var version = asm.GetName().Version ?? new Version();
            ProductVersion = $"{version.Major}.{version.Minor}";
            ProductFullVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            ProductVersionNumber = GenerateProductVersionNumber(version.Major, version.Minor, version.Build);
        }

        /// <summary>
        /// フォルダー生成
        /// </summary>
        private static void CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
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

        // 全ユーザデータ削除
        private bool RemoveApplicationDataCore()
        {
            // LocalApplicationDataフォルダを使用している場合のみ
            if (!IsUseLocalApplicationDataFolder) return false;

            Debug.WriteLine("RemoveAllApplicationData ...");

            //var productFolder = GetFileSystemPath(Environment.SpecialFolder.LocalApplicationData, false);
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

#if false
        //
        public event EventHandler? LocalApplicationDataRemoved;

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
                    LocalApplicationDataRemoved?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, $"{ProductName} - エラー", MessageBoxButton.OK, MessageBoxImage.Error);

                    // カレントフォルダ復帰
                    System.Environment.CurrentDirectory = currentFolder;
                }
            }
        }
#endif
    }

}
