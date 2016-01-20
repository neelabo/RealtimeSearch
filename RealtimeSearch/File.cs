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
    // 数が尋常でないので、軽量にすべき
    public class File : INotifyPropertyChanged, IComparable
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion

        // パス
        private string _Path;
        public string Path
        {
            get { return _Path; }
            set
            {
                _Path = value;
                NormalizedWord = ToNormalisedWord(FileName);
                FileInfo = new FileInfo(_Path);
            }
        }

        // フォルダ表示名
        public string DirectoryName
        {
            get
            {
                string dir = System.IO.Path.GetDirectoryName(_Path);
                string parentDir = System.IO.Path.GetDirectoryName(dir);
                return (parentDir == null) ? dir : System.IO.Path.GetFileName(dir) + " (" + parentDir + ")";
            }
        }

        // ファイル名
        public string FileName { get { return System.IO.Path.GetFileName(_Path); } }

        // 検索用正規化ファイル名
        public string NormalizedWord { get; private set; }

        // ファイル情報
        public FileInfo FileInfo { get; private set; }

        // ディレクトリ？
        public bool IsDirectory { get; set; }

        public File()
        {
            //ToNormalisedWord("ＡＢＣ０１２");
            //ToNormalisedWord("ABCＡＢＣabc。｡　い ろはﾊﾞイロハｲﾛﾊ＃：");
        }


        // すべてのプロパティを更新
        public void NotifyAllPropertyCnanged()
        {
            OnPropertyChanged("Path");
            OnPropertyChanged("FileName");
            OnPropertyChanged("FileInfo");
        }


        // 正規化された文字列に変換する
        public static string ToNormalisedWord(string src)
        {
            string s = src.Normalize(NormalizationForm.FormKC); // 正規化
            s = s.Replace(" ", ""); // 空白を削除する

            s = s.ToUpper(); // アルファベットを大文字にする
            s = Microsoft.VisualBasic.Strings.StrConv(s, Microsoft.VisualBasic.VbStrConv.Katakana); // ひらがなをカタカナにする
            s = s.Replace("ー", "-"); // 長音をハイフンにする 

            return s;
        }


        // 表示文字列
        public override string ToString()
        {
            return FileName;
        }


        // CompareTo
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            File other = (File)obj;
            return Win32Api.StrCmpLogicalW(this.FileName, other.FileName);
        }
    }
}
