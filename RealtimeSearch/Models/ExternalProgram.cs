using CommunityToolkit.Mvvm.ComponentModel;
using NeeLaboratory.Generators;
using NeeLaboratory.RealtimeSearch.TextResource;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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


    public partial class ExternalProgram : ObservableObject
    {
        public const string KeyFile = "$(file)";
        public const string KeyUri = "$(uri)";
        public const string KeyFileQuote = "\"$(file)\"";
        public const string KeyUriQuote = "\"$(uri)\"";

        private int _id;
        private ExternalProgramType _programType;
        private string _program = "";
        private string _parameter = KeyFileQuote;
        private string _protocol = "";
        private string _extensions = "";
        private bool _isMultiArgumentEnabled;
        private List<string> _extensionsList = new();

        [JsonInclude, JsonPropertyName(nameof(Name))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? _name;


        public ExternalProgram()
        {
        }


        [JsonIgnore]
        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
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
            set
            {
                if (SetProperty(ref _programType, value))
                {
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Program
        {
            get { return _program; }
            set
            {
                if (SetProperty(ref _program, value.Trim()))
                {
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Parameter
        {
            get { return _parameter; }
            set
            {
                value ??= "";
                var s = value.Trim();
                if (!s.Contains(KeyFile))
                {
                    s = (s + " " + KeyFileQuote).Trim();
                }
                _parameter = s;
                OnPropertyChanged();
            }
        }

        public string Protocol
        {
            get { return _protocol; }
            set
            {
                if (SetProperty(ref _protocol, value.Trim()))
                {
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Extensions
        {
            get { return _extensions; }
            set
            {
                if (_extensions != value)
                {
                    CreateExtensionsList(value);
                }
            }
        }

        public bool IsMultiArgumentEnabled
        {
            get { return _isMultiArgumentEnabled; }
            set { SetProperty(ref _isMultiArgumentEnabled, value); }
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

            _extensions = string.Join(' ', _extensionsList);
            OnPropertyChanged(nameof(Extensions));
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
            if (ProgramType == ExternalProgramType.Uri)
            {
                if (string.IsNullOrWhiteSpace(_protocol))
                {
                    return ResourceService.GetString("@App.DefaultApp");
                }
                return _protocol.Split(':').FirstOrDefault() ?? $"Protocol {_id}"; 
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_program))
                {
                    return ResourceService.GetString("@App.DefaultApp");
                }
                return Path.GetFileNameWithoutExtension(_program);
            }
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
