using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NeeLaboratory.RealtimeSearch
{
    public enum ExternalProgramType
    {
        Normal,
        Uri,
    }

    public static class ExternalProgramTypeExtensions
    {
        public static readonly Dictionary<ExternalProgramType, string> ExternalProgramTypeNames = new()
        {
            [ExternalProgramType.Normal] = "外部プログラム",
            [ExternalProgramType.Uri] = "プロトコル起動",
        };
    }


    public class ExternalProgram : INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion PropertyChanged


        public const string KeyFile = "$(file)";
        public const string KeyUri = "$(uri)";
        public const string KeyFileQuat = "\"$(file)\"";
        public const string KeyUriQuat = "\"$(uri)\"";

        private ExternalProgramType _programType;
        private string _program = "";
        private string _parameter = "";
        private string _protocol = "";
        private string _extensions = "";
        private bool _isMultiArgumentEnabled;
        private List<string> _extensionsList = new();


        public ExternalProgram()
        {
        }


        public ExternalProgramType ProgramType
        {
            get { return _programType; }
            set { if (_programType != value) { _programType = value; RaisePropertyChanged(); } }
        }

        public string Program
        {
            get { return _program; }
            set
            {
                value ??= ""; _program = value.Trim();
                RaisePropertyChanged();
            }
        }

        public string Parameter
        {
            get { return _parameter; }
            set
            {
                value ??= "";
                var s = value.Trim();
                if (!s.Contains("$(file)"))
                {
                    s = (s + " \"$(file)\"").Trim();
                }
                _parameter = s;
                RaisePropertyChanged();
            }
        }

        public string Protocol
        {
            get { return _protocol; }
            set { _protocol = (value ?? "").Trim(); RaisePropertyChanged(); }
        }

        public string Extensions
        {
            get { return _extensions; }
            set { if (_extensions != value) { _extensions = value; RaisePropertyChanged(); CreateExtensionsList(_extensions); } }
        }

        public bool IsMultiArgumentEnabled
        {
            get { return _isMultiArgumentEnabled; }
            set { if (_isMultiArgumentEnabled != value) { _isMultiArgumentEnabled = value; RaisePropertyChanged(); } }
        }


        private void CreateExtensionsList(string extensions)
        {
            if (extensions == null) return;

            var reg = new Regex(@"^[\*\.\s]+");

            var list = new List<string>();
            foreach (var token in extensions.Split(';', ' '))
            {
                //var ext = token.Trim().TrimStart('.').ToLower();
                var ext = reg.Replace(token.Trim(), "").ToLower();
                if (!string.IsNullOrWhiteSpace(ext)) list.Add("." + ext);
            }

            _extensionsList = list;
        }


        public bool CheckExtensions(string input)
        {
            if (input == null) return false;
            if (_extensionsList == null || _extensionsList.Count == 0) return true;

            var ext = System.IO.Path.GetExtension(input).ToLower();
            return _extensionsList.Contains(ext);
        }
    }
}
