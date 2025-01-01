using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using NeeLaboratory.ComponentModel;
using NeeLaboratory.IO.Search.Files;

namespace NeeLaboratory.RealtimeSearch.Models
{
    public class ExternalProgramCollection : BindableBase, IEnumerable<ExternalProgram>
    {
        private readonly AppSettings _setting;
        private string _error = "";


        public ExternalProgramCollection(AppSettings setting)
        {
            _setting = setting;
        }


        public ExternalProgram this[int i]
        {
            get { return _setting.ExternalPrograms[i]; }
            set { _setting.ExternalPrograms[i] = value; }
        }

        public int Count
        {
            get { return _setting.ExternalPrograms.Count; }
        }


        public string Error
        {
            get { return _error; }
            set { SetProperty(ref _error, value); }
        }


        public void ClearError()
        {
            _error = "";
        }

        private ExternalProgram? FindExternalProgram(string path)
        {
            foreach (var program in _setting.ExternalPrograms)
            {
                if (program.CheckExtensions(path))
                {
                    return program;
                }
            }
            return null;
        }

        public void Execute(IEnumerable<FileContent> files)
        {
            try
            {
                foreach (var group in files.GroupBy(e => FindExternalProgram(e.Path)))
                {
                    if (group.Key == null)
                    {
                        ExecuteDefault(group);
                    }
                    else
                    {
                        Execute(group.AsEnumerable(), group.Key);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Error = e.Message;
            }
        }

        public void Execute(IEnumerable<FileContent> files, int programId)
        {
            if (_setting.ExternalPrograms.Count <= programId - 1) return;

            try
            {
                Execute(files, _setting.ExternalPrograms[programId - 1]);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Error = e.Message;
            }
        }

        private void Execute(IEnumerable<FileContent> files, ExternalProgram program)
        {
            if (program.IsMultiArgumentEnabled)
            {
                ExecuteProgram(files, program);
            }
            else
            {
                foreach (var file in files)
                {
                    ExecuteProgram(new List<FileContent>() { file }, program);
                }
            }
        }

        private void ExecuteProgram(IEnumerable<FileContent> files, ExternalProgram program)
        {
            if (program.ProgramType == ExternalProgramType.Normal)
            {
                if (string.IsNullOrWhiteSpace(program.Program))
                {
                    ExecuteDefault(files);
                }
                else
                {
                    var commandName = program.Program;
                    var arguments = ReplaceKeyword(program.Parameter, files);
                    var startInfo = new System.Diagnostics.ProcessStartInfo(commandName, arguments) { UseShellExecute = false };
                    System.Diagnostics.Process.Start(startInfo);
                }
            }

            else if (program.ProgramType == ExternalProgramType.Uri)
            {
                if (string.IsNullOrWhiteSpace(program.Protocol))
                {
                    ExecuteDefault(files);
                }
                else
                {
                    var protocol = ReplaceKeyword(program.Protocol, files);
                    var startInfo = new System.Diagnostics.ProcessStartInfo(protocol) { UseShellExecute = true };
                    System.Diagnostics.Process.Start(startInfo);
                }
            }
        }

        private static string ReplaceKeyword(string s, IEnumerable<FileContent> files)
        {
            if (files.Count() == 1)
            {
                var file = files.First();

                var uriData = Uri.EscapeDataString(file.Path);

                s = s.Replace(ExternalProgram.KeyUri, uriData);
                s = s.Replace(ExternalProgram.KeyFile, file.Path);
            }
            else
            {
                var uriData = string.Join(" ", files.Select(e => Uri.EscapeDataString(e.Path)));
                var uriDataQuote = string.Join(" ", files.Select(e => "\"" + Uri.EscapeDataString(e.Path) + "\""));

                var pathData = string.Join(" ", files.Select(e => e.Path));
                var pathDataQuote = string.Join(" ", files.Select(e => "\"" + e.Path + "\""));

                s = s.Replace(ExternalProgram.KeyUriQuote, uriDataQuote);
                s = s.Replace(ExternalProgram.KeyUri, uriData);
                s = s.Replace(ExternalProgram.KeyFileQuote, pathDataQuote);
                s = s.Replace(ExternalProgram.KeyFile, pathData);
            }

            return s;
        }

        public void ExecuteDefault(IEnumerable<FileContent> files)
        {
            try
            {
                foreach (var file in files)
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo(file.Path) { UseShellExecute = true };
                    System.Diagnostics.Process.Start(startInfo);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Error = e.Message;
            }
        }

        public IEnumerator<ExternalProgram> GetEnumerator()
        {
            return _setting.ExternalPrograms.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
