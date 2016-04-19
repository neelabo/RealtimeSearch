// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealtimeSearch
{
    [Flags]
    public enum NodeFlag
    {
        Added = (1 << 0),
        Removed = (1 << 1),
        PushPin = (1 << 2),
    }


    /// <summary>
    /// Nodeコンテンツ
    /// </summary>
    public class NodeContent : INotifyPropertyChanged, IComparable
    {
        #region NotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(name));
            }
        }
        #endregion

        public string Path { get; set; }

        public string Name => System.IO.Path.GetFileName(Path);

        // フォルダ表示名
        public string DirectoryName
        {
            get
            {
                string dir = System.IO.Path.GetDirectoryName(Path);
                string parentDir = System.IO.Path.GetDirectoryName(dir);
                return (parentDir == null) ? dir : System.IO.Path.GetFileName(dir) + " (" + parentDir + ")";
            }
        }

        // 詳細表示
        public string Detail
        {
            get
            {
                string sizeText = (FileInfo.Size > 0) ? $"サイズ: {(FileInfo.Size + 1024 - 1) / 1024:#,0} KB\n" : "";
                return $"{Name}\n種類: {FileInfo.TypeName}\n{sizeText}更新日時: {FileInfo.LastWriteTime.ToString("yyyy/MM/dd HH:mm")}\nフォルダー: {DirectoryName}";
            }
        }

        // ファイル情報
        public FileInfo FileInfo { get; private set; }

        // flag control
        private NodeFlag Flags { get; set; }

        private bool IsFlag(NodeFlag flag)
        {
            return (Flags & flag) == flag;
        }

        private void SetFlag(NodeFlag flag, bool state)
        {
            if (state)
                Flags = Flags | flag;
            else
                Flags = Flags & ~flag;
        }

        // 追加されたフラグ
        public bool IsAdded
        {
            get { return IsFlag(NodeFlag.Added); }
            set { SetFlag(NodeFlag.Added, value); }
        }

        // 削除されたフラグ
        public bool IsRemoved
        {
            get { return IsFlag(NodeFlag.Removed); }
            set { SetFlag(NodeFlag.Removed, value); }
        }

        // 検索結果に残す
        public bool IsPushPin
        {
            get { return IsFlag(NodeFlag.PushPin); }
            set { SetFlag(NodeFlag.PushPin, value); OnPropertyChanged(); }
        }


        // コンストラクタ
        public NodeContent(string path)
        {
            Path = path;
            FileInfo = new FileInfo(Path);
        }

        // ファイル情報更新
        public void Reflesh()
        {
            FileInfo = new FileInfo(Path);
            OnPropertyChanged(nameof(Path));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(FileInfo));
            OnPropertyChanged(nameof(DirectoryName));
            OnPropertyChanged(nameof(Detail));
        }

        // プロパティウィンドウを開く
        public void OpenProperty(System.Windows.Window window)
        {
            FileInfo.OpenProperty(window, Path);
        }

        // 表示文字列
        public override string ToString()
        {
            return Name;
        }

        // CompareTo
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            NodeContent other = (NodeContent)obj;
            return Win32Api.StrCmpLogicalW(this.Name, other.Name);
        }
    }
}
