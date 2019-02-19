// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using NeeLaboratory.IO.Search;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;


namespace NeeLaboratory.RealtimeSearch
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Support

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            this.RaisePropertyChanged(propertyName);
            return true;
        }

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void AddPropertyChanged(string propertyName, PropertyChangedEventHandler handler)
        {
            PropertyChanged += (s, e) => { if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == propertyName) handler?.Invoke(s, e); };
        }

        #endregion


        private CollectionViewSource _collectionViewSource;
        public CollectionViewSource CollectionViewSource
        {
            get { return _collectionViewSource; }
            set { _collectionViewSource = value; RaisePropertyChanged(); }
        }

        private SearchArea _selectedArea;
        public SearchArea SelectedArea
        {
            get { return _selectedArea; }
            set { SetProperty(ref _selectedArea, value); }
        }



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
            var readmeUri = "file://" + Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace('\\', '/').TrimEnd('/') + $"/README.html";
            this.CommandBindings.Add(new CommandBinding(HelpCommand, (t, e) => System.Diagnostics.Process.Start(readmeUri)));

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
            if (SelectedArea != null)
            {
                Setting.SearchAreas.Remove(SelectedArea);
                SelectedArea = null;
            }
        }


        private void DelCommand_CanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedArea != null;
        }


        private void UpdateCollectionViewSource()
        {
            var collectionViewSource = new CollectionViewSource();
            collectionViewSource.Source = Setting.SearchAreas;
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(SearchArea.Path), System.ComponentModel.ListSortDirection.Ascending));

            CollectionViewSource = collectionViewSource;
            SelectedArea = null;
        }


        private void AddSearchPath(string path)
        {
            if (!System.IO.Directory.Exists(path)) return;

            var area = new SearchArea(path, true);
            var existArea = Setting.SearchAreas.FirstOrDefault(p => p.Path == area.Path);

            if (existArea != null)
            {
                SelectedArea = existArea;
                return;
            }

            Setting.SearchAreas.Add(area);
            SelectedArea = area;
        }


        private void AddPathWithDialog()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "検索フォルダーの追加";
            dialog.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                AddSearchPath(dialog.SelectedPath);
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
