// Copyright (c) 2015 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;


namespace RealtimeSearch
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window, INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion
    
        #region Property: CollectionViewSource
        private CollectionViewSource _CollectionViewSource;
        public CollectionViewSource CollectionViewSource
        {
            get { return _CollectionViewSource; }
            set { _CollectionViewSource = value; OnPropertyChanged(); }
        }
        #endregion

        #region Property: SelectedPath
        private string _SelectedPath;
        public string SelectedPath
        {
            get { return _SelectedPath; }
            set { _SelectedPath = value; OnPropertyChanged(); }
        }
        #endregion


        public static readonly RoutedCommand CloseCommand = new RoutedCommand("CloseCommand", typeof(SettingWindow));
        public static readonly RoutedCommand HelpCommand = new RoutedCommand("HelpCommand", typeof(SettingWindow));
        public static readonly RoutedCommand AddCommand = new RoutedCommand("AddCommand", typeof(SettingWindow));
        public static readonly RoutedCommand DelCommand = new RoutedCommand("DelCommand", typeof(SettingWindow));

        public Setting Setting { get; private set; }


        public SettingWindow(Setting setting)
        {
            Setting = setting;
            UpdateCollectionViewSource();

            InitializeComponent();

            this.DataContext = this;

            // close command
            CloseCommand.InputGestures.Clear();
            CloseCommand.InputGestures.Add(new KeyGesture(Key.Escape));
            this.CommandBindings.Add(new CommandBinding(CloseCommand, (t, e) => Close()));

            // help command
            this.CommandBindings.Add(new CommandBinding(HelpCommand, (t, e) => System.Diagnostics.Process.Start("https://bitbucket.org/neelabo/realtimesearch/wiki")));

            // add command
            AddCommand.InputGestures.Clear();
            AddCommand.InputGestures.Add(new KeyGesture(Key.Insert));
            SearchPathPanel.CommandBindings.Add(new CommandBinding(AddCommand, AddCommand_Executed));

            // del command
            DelCommand.InputGestures.Clear();
            DelCommand.InputGestures.Add(new KeyGesture(Key.Delete));
            SearchPathPanel.CommandBindings.Add(new CommandBinding(DelCommand, DelCommand_Executed, DelCommand_CanExecute));

            SearchPathList.Focus();

            this.contextMenu01.UpdateInputGestureText();
        }


        private void AddCommand_Executed(object target, ExecutedRoutedEventArgs e)
        {
            AddPathWithDialog();
        }


        private void DelCommand_Executed(object target, ExecutedRoutedEventArgs e)
        {
            if (SelectedPath != null)
            {
                Setting.SearchPaths.Remove(SelectedPath);
                SelectedPath = null;
            }
        }


        private void DelCommand_CanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedPath != null;
        }


        private void UpdateCollectionViewSource()
        {
            var collectionViewSource = new CollectionViewSource();
            collectionViewSource.Source = Setting.SearchPaths;
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription(null, System.ComponentModel.ListSortDirection.Ascending));

            CollectionViewSource = collectionViewSource;
            SelectedPath = null;
        }


        private void AddSearchPath(string path)
        {
            if (!System.IO.Directory.Exists(path)) return;

            string existPath = Setting.SearchPaths.FirstOrDefault(p => p == path);

            if (existPath != null)
            {
                SelectedPath = existPath;
                return;
            }

            Setting.SearchPaths.Add(path);
            SelectedPath = path;
        }


        private void AddPathWithDialog()
        {
            // フォルダ選択
            var dlg = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
            dlg.Title = "検索フォルダーの追加";
            dlg.IsFolderPicker = true;
            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog(Window.GetWindow(this)) == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                AddSearchPath(dlg.FileName);
            }
        }


        private void ListBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }


        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            var dropFiles = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (dropFiles == null) return;

            foreach (var file in dropFiles)
            {
                AddSearchPath(file);
            }
        }
    }



    public static class Extensions
    {
        public static void UpdateInputGestureText(this ItemsControl control)
        {
            if (control == null) return;

            KeyGestureConverter kgc = new KeyGestureConverter();
            foreach (var item in control.Items.OfType<MenuItem>())
            {
                var command = item.Command as RoutedCommand;
                if (command != null)
                {
                    string text = null;
                    foreach (InputGesture gesture in command.InputGestures)
                    {
                        if (text == null)
                        {
                            text = kgc.ConvertToString(gesture);
                        }
                        else
                        {
                            text += ", " + kgc.ConvertToString(gesture);
                        }
                    }
                    item.InputGestureText = text;
                }

                UpdateInputGestureText(item);
            }
        }

    }
}
