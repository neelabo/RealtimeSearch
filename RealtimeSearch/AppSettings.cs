using NeeLaboratory.RealtimeSearch.Services;
using NeeLaboratory.Text.Json;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace NeeLaboratory.RealtimeSearch
{
    public class AppSettings : IAppSettings
    {
        private static AppSettings? _current;
        public static AppSettings Current
        {
            get
            {
                if (_current == null)
                {
                    var fileName = "/RealtimeSearch.settings.json";
                    var resource_uri = new Uri(fileName, UriKind.Relative);
                    var info = Application.GetContentStream(resource_uri) ?? throw new FileNotFoundException($"File not found: {fileName}");
                    using var stream = info.Stream;
                    _current = JsonSerializer.Deserialize<AppSettings>(stream, JsonSerializerTools.GetSerializerOptions());
                    if (_current is null) throw new FormatException($"Cannot read: {fileName}");
                }
                return _current;
            }
        }


        /// <summary>
        /// パッケージタイプ
        /// </summary>
        /// <remarks>
        /// Dev, Canary, Beta, Zip, Msi, Appx
        /// </remarks>
        public string PackageType { get; set; } = "Dev";

        /// <summary>
        /// 自己完結型アプリか
        /// </summary>
        /// <remarks>
        /// false のときはフレームワーク依存型 (-fd)
        /// </remarks>
        public bool SelfContained { get; set; }

        /// <summary>
        /// [開発用] パッケージタイプウォーターマークを表示する
        /// </summary>
        public bool Watermark { get; set; }

        /// <summary>
        /// LocalApplicationData にデータを保存するか
        /// </summary>
        /// <remarks>
        /// Msi, Appx は true。
        /// false のときはアプリの場所に保存する
        /// </remarks>
        public bool UseLocalApplicationData { get; set; }

        /// <summary>
        /// Git のリビジョン番号
        /// </summary>
        public string Revision { get; set; } = "??";

        /// <summary>
        /// ビルド日時バージョン "0000"
        /// </summary>
        public string DateVersion { get; set; } = "??";

        /// <summary>
        /// ログファイル名
        /// </summary>
        /// <remarks>
        /// 指定されていればログを出力する
        /// </remarks>
        public string? LogFile { get; set; }

        /// <summary>
        /// [開発用] バージョンチェック用のバージョンを指定
        /// </summary>
        /// <remarks>
        /// e.g. 36.2
        /// </remarks>
        public string? CheckVersion { get; set; }

        /// <summary>
        /// [開発用] バージョンチェック用のパッケージ置き場を指定
        /// </summary>
        /// <remarks>
        /// e.g. https://neelabo.bitbucket.io/NeeViewUpdateCheck.html
        /// </remarks>
        public string? DistributionUrl { get; set; }

        /// <summary>
        /// JSON保存データにデフォルト値を出力しないようにして軽量化する
        /// </summary>
        public bool TrimSaveData { get; set; } = true;
    }

}
