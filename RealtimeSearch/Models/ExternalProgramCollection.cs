using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using NeeLaboratory.IO.Search.FileNode;

namespace NeeLaboratory.RealtimeSearch
{
    public class ExternalProgramCollection : BindableBase
    {
        private readonly AppConfig _setting;
        private string _error = "";


        public ExternalProgramCollection(AppConfig setting)
        {
            _setting = setting;
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

        public void Execute(IEnumerable<NodeContent> files)
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

        public void Execute(IEnumerable<NodeContent> files, int programId)
        {
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

        private void Execute(IEnumerable<NodeContent> files, ExternalProgram program)
        {
            if (program.IsMultiArgumentEnabled)
            {
                ExecuteProgram(files, program);
            }
            else
            {
                foreach (var file in files)
                {
                    ExecuteProgram(new List<NodeContent>() { file }, program);
                }
            }
        }

        private void ExecuteProgram(IEnumerable<NodeContent> files, ExternalProgram program)
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
                if (string.IsNullOrWhiteSpace(program.Protocol)) throw new InvalidOperationException("プロトコルが指定されていません。設定を見直してください。");

                var protocol = ReplaceKeyword(program.Protocol, files);
                var startInfo = new System.Diagnostics.ProcessStartInfo(protocol) { UseShellExecute = true };
                System.Diagnostics.Process.Start(startInfo);
            }
        }

        private static string ReplaceKeyword(string s, IEnumerable<NodeContent> files)
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
                var uriDataQuat = string.Join(" ", files.Select(e => "\"" + Uri.EscapeDataString(e.Path) + "\""));

                var pathData = string.Join(" ", files.Select(e => e.Path));
                var pathDataQuat = string.Join(" ", files.Select(e => "\"" + e.Path + "\""));

                s = s.Replace(ExternalProgram.KeyUriQuat, uriDataQuat);
                s = s.Replace(ExternalProgram.KeyUri, uriData);
                s = s.Replace(ExternalProgram.KeyFileQuat, pathDataQuat);
                s = s.Replace(ExternalProgram.KeyFile, pathData);
            }

            return s;
        }

        public void ExecuteDefault(IEnumerable<NodeContent> files)
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
    }
}
