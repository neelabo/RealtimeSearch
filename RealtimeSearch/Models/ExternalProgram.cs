using NeeLaboratory.Generators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NeeLaboratory.RealtimeSearch.Models
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


    [NotifyPropertyChanged]
    public partial class ExternalProgram : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;


        public const string KeyFile = "$(file)";
        public const string KeyUri = "$(uri)";
        public const string KeyFileQuote = "\"$(file)\"";
        public const string KeyUriQuote = "\"$(uri)\"";

        private int _id;
        private ExternalProgramType _programType;
        private string _program = "";
        private string _parameter = "";
        private string _protocol = "";
        private string _extensions = "";
        private bool _isMultiArgumentEnabled;
        private List<string> _extensionsList = new();

        [JsonInclude, JsonPropertyName("Name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? _name;


        public ExternalProgram()
        {
        }


        [JsonIgnore]
        public int Id
        {
            get { return _id; }
            set
            {
                if (SetProperty(ref _id, value))
                {
                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

        [JsonIgnore]
        public string Name
        {
            get { return _name ?? GetDefaultName(); }
            set { SetProperty(ref _name, ValidateName(value)); }
        }

        public ExternalProgramType ProgramType
        {
            get { return _programType; }
            set { SetProperty(ref _programType, value); }
        }

        public string Program
        {
            get { return _program; }
            set { SetProperty(ref _program, value.Trim()); }
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
            set { SetProperty(ref _protocol, value.Trim()); }
        }

        public string Extensions
        {
            get { return _extensions; }
            set
            {
                if (SetProperty(ref _extensions, value))
                {
                    CreateExtensionsList(_extensions);
                }
            }
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


        private string GetDefaultName()
        {
            return $"Program {_id}";
        }

        private string? ValidateName(string value)
        {
            var s = value.Trim();
            if (string.IsNullOrWhiteSpace(value) || s == GetDefaultName())
            {
                return null;
            }
            return s;
        }
    }
}
