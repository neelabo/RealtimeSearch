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
        public static Dictionary<ExternalProgramType, string> ExternalProgramTypeNames = new Dictionary<ExternalProgramType, string>
        {
            [ExternalProgramType.Normal] = "外部プログラム",
            [ExternalProgramType.Uri] = "プロトコル起動",
        };
    }


    [DataContract]
    public class ExternalProgram : INotifyPropertyChanged
    {
        public const string KeyFile = "$(file)";
        public const string KeyUri = "$(uri)";

        /// <summary>
        /// PropertyChanged event. 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        /// <summary>
        /// ProgramType property.
        /// </summary>
        private ExternalProgramType _programType;
        [DataMember]
        public ExternalProgramType ProgramType
        {
            get { return _programType; }
            set { if (_programType != value) { _programType = value; RaisePropertyChanged(); } }
        }



        /// <summary>
        /// Program property.
        /// </summary>
        private string _program;
        [DataMember]
        public string Program
        {
            get { return _program; }
            set { value = value ?? ""; _program = value.Trim(); RaisePropertyChanged(); }
        }


        /// <summary>
        /// Parameter property.
        /// </summary>
        private string _parameter = "";
        [DataMember]
        public string Parameter
        {
            get { return _parameter; }
            set
            {
                value = value ?? "";
                var s = value.Trim();
                if (!s.Contains("$(file)"))
                {
                    s = (s + " \"$(file)\"").Trim();
                }
                _parameter = s;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Protocol property.
        /// </summary>
        private string _protocol = "";
        [DataMember]
        public string Protocol
        {
            get { return _protocol; }
            set { _protocol = (value ?? "").Trim(); RaisePropertyChanged(); }
        }

        /// <summary>
        /// Extensions property.
        /// </summary>
        private string _extensions;
        [DataMember]
        public string Extensions
        {
            get { return _extensions; }
            set { if (_extensions != value) { _extensions = value; RaisePropertyChanged(); CreateExtensionsList(_extensions); } }
        }

        private List<string> _extensionsList { get; set; }

        //
        private void CreateExtensionsList(string exts)
        {
            if (exts == null) return;

            var reg = new Regex(@"^[\*\.\s]+");

            var list = new List<string>();
            foreach (var token in exts.Split(';', ' '))
            {
                //var ext = token.Trim().TrimStart('.').ToLower();
                var ext = reg.Replace(token.Trim(), "").ToLower();
                if (!string.IsNullOrWhiteSpace(ext)) list.Add("." + ext);
            }

            _extensionsList = list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool CheckExtensions(string input)
        {
            if (input == null) return false;
            if (_extensionsList == null || _extensionsList.Count == 0) return true;

            var ext = System.IO.Path.GetExtension(input).ToLower();
            return _extensionsList.Contains(ext);
        }


        ///
        private void Constructor()
        {
            Program = "";
            Parameter = "";
            Protocol = "";
            Extensions = "";
        }

        ///
        public ExternalProgram()
        {
            Constructor();
        }

        //
        [OnDeserializing]
        private void Deserializing(StreamingContext c)
        {
            Constructor();
        }

        //
        [OnDeserialized]
        private void Deserializied(StreamingContext c)
        {
        }



    }
}
