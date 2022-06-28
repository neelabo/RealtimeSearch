using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NeeLaboratory.RealtimeSearch
{

    public class RenameClosingEventArgs : EventArgs
    {
        public RenameClosingEventArgs(TextBlock target, string oldValue, string newValue)
        {
            Target = target;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public TextBlock Target { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; private set; }
        public bool Cancel { get; set; }
    }

    public class RenameClosedEventArgs : EventArgs
    {
        public RenameClosedEventArgs(TextBlock target, string oldValue, string newValue)
        {
            Target = target;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public TextBlock Target { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; private set; }
        public int Navigate { get; init; }
    }

    /// <summary>
    /// RenameControl.xaml の相互作用ロジック
    /// </summary>
    public partial class RenameControl : UserControl, INotifyPropertyChanged
    {
        #region INotifyProertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion INotifyProertyChanged


        private TextBlock _target;
        private string _old = "";
        private string _new = "";
        private int _navigate;
        private int _keyCount;
        private int _closing;
        private Window _targetWindow;
        private Point _targetLocate;


        public RenameControl(TextBlock textBlock)
        {
            InitializeComponent();

            _target = textBlock;
            _targetWindow = Window.GetWindow(_target);
            _targetLocate = _target.TranslatePoint(default, _targetWindow);

            Text = _target.Text ?? "";
            _old = Text;
            _new = Text;

            this.RenameTextBox.FontFamily = _target.FontFamily;
            this.RenameTextBox.FontSize = _target.FontSize;

            this.RenameTextBox.DataContext = this;
        }


        public event EventHandler<RenameClosingEventArgs>? Closing;

        public event EventHandler? Close;

        public event EventHandler<RenameClosedEventArgs>? Closed;


        public TextBlock Target => _target;

        /// <summary>
        /// Text property.
        /// </summary>
        private string _text = "";
        public string Text
        {
            get { return _text; }
            set { if (_text != value) { _text = value; RaisePropertyChanged(); } }
        }

        public bool IsSelectedWithoutExtension { get; set; } = true;


        private void Target_LayoutUpdated(object? sender, EventArgs e)
        {
            var pos = _target.TranslatePoint(default, _targetWindow);
            if (pos != _targetLocate)
            {
                Stop(true);
            }
        }

        private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Stop(true);
        }


        public void Stop(bool isSuccess = true)
        {
            var closing = Interlocked.Increment(ref _closing);
            if (closing > 1) return;

            _new = isSuccess ? Text.Trim() : _old;

            var args = new RenameClosingEventArgs(_target, _old, _new);
            Closing?.Invoke(this, args);
            if (args.Cancel)
            {
                _new = _old;
            }

            Close?.Invoke(this, EventArgs.Empty);
        }


        private void RenameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            // 拡張子以外を選択状態にする
            string name = IsSelectedWithoutExtension ? System.IO.Path.GetFileNameWithoutExtension(Text) : Text;
            this.RenameTextBox.Select(0, name.Length);

            // 表示とともにフォーカスする
            this.RenameTextBox.Focus();

            _target.LayoutUpdated += Target_LayoutUpdated;
        }

        private void RenameTextBox_Unloaded(object sender, RoutedEventArgs e)
        {
            _target.LayoutUpdated -= Target_LayoutUpdated;

            Closed?.Invoke(this, new RenameClosedEventArgs(_target, _old, _new) { Navigate = _navigate });
        }

        private void RenameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 最初の方向入力に限りカーソル位置を固定する
            if (_keyCount == 0 && (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right))
            {
                this.RenameTextBox.Select(this.RenameTextBox.SelectionStart + this.RenameTextBox.SelectionLength, 0);
                _keyCount++;
            }
        }

        private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Stop(false);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                Stop(true);
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                _navigate = (Keyboard.Modifiers == ModifierKeys.Shift) ? -1 : +1;
                Stop(true);
                e.Handled = true;
            }
        }

        private void RenameTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Stop(true);
            e.Handled = true;
        }

        private void MeasureText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.RenameTextBox.MinWidth = this.MeasureText.ActualWidth + 30;
        }
    }
}
