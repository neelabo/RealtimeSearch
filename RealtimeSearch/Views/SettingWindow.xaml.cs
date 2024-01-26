using NeeLaboratory.IO.Search.Files;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            this.RaisePropertyChanged(propertyName);
            return true;
        }

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void AddPropertyChanged(string propertyName, PropertyChangedEventHandler handler)
        {
            PropertyChanged += (s, e) => { if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == propertyName) handler?.Invoke(s, e); };
        }

        #endregion


        private CollectionViewSource? _collectionViewSource;
        public CollectionViewSource? CollectionViewSource
        {
            get { return _collectionViewSource; }
            set { _collectionViewSource = value; RaisePropertyChanged(); }
        }

        private FileArea? _selectedArea;
        public FileArea? SelectedArea
        {
            get { return _selectedArea; }
            set { SetProperty(ref _selectedArea, value); }
        }



        public static readonly RoutedCommand CloseCommand = new("CloseCommand", typeof(SettingWindow));
        public static readonly RoutedCommand HelpCommand = new("HelpCommand", typeof(SettingWindow));
        public static readonly RoutedCommand AddCommand = new("AddCommand", typeof(SettingWindow));
        public static readonly RoutedCommand DelCommand = new("DelCommand", typeof(SettingWindow));

        public AppConfig Setting { get; private set; }


        public SettingWindow(AppConfig setting)
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
            var readmeUri = "file://" + App.AppInfo.AssemblyLocation.Replace('\\', '/').TrimEnd('/') + $"/README.html";
            this.CommandBindings.Add(new CommandBinding(HelpCommand, (t, e) =>
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo(readmeUri) { UseShellExecute = true };
                    System.Diagnostics.Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}", "ヘルプを開けませんでした", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));

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
            var collectionViewSource = new CollectionViewSource
            {
                Source = Setting.SearchAreas
            };
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(FileArea.Path), System.ComponentModel.ListSortDirection.Ascending));

            CollectionViewSource = collectionViewSource;
            SelectedArea = null;
        }


        private void AddSearchPath(string path)
        {
            if (!System.IO.Directory.Exists(path)) return;

            var area = new FileArea(path, true);
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
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "検索フォルダーの追加",
                SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal)
            };

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
            if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] dropFiles) return;

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

            var kgc = new KeyGestureConverter();
            foreach (var item in control.Items.OfType<MenuItem>())
            {
                if (item.Command is RoutedCommand command)
                {
                    string? text = null;
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
