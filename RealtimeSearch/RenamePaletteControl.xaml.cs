using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

namespace RealtimeSearch
{
    /// <summary>
    /// 
    /// </summary>
    public class PopupTextBoxClosedEventArgs
    {
        public bool Result { get; set; }
        public string Text { get; set; }
        public FrameworkElement Target { get; set; }

        public bool Cancel { get; set; }
    }

    /// <summary>
    /// RenameControl.xaml の相互作用ロジック
    /// </summary>
    public partial class RenamePaletteControl : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        #region Property: Target
        private FrameworkElement _target;
        public FrameworkElement Target
        {
            get { return _target; }
            set { _target = value; OnPropertyChanged(); UpdatePlacement(); }
        }
        #endregion

        /// <summary>
        /// Text Property
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(RenamePaletteControl), new PropertyMetadata(null));



        /// <summary>
        /// IsExplorerLike Property
        /// </summary>
        public bool IsExplorerLike
        {
            get { return (bool)GetValue(IsExplorerLikeProperty); }
            set { SetValue(IsExplorerLikeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsExplorerLike.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsExplorerLikeProperty =
            DependencyProperty.Register("IsExplorerLike", typeof(bool), typeof(RenamePaletteControl), new PropertyMetadata(false));


        /// <summary>
        /// Constructor
        /// </summary>
        public RenamePaletteControl()
        {
            InitializeComponent();

            this.DataContext = this;

            // クリックイベントをここでストップ
            this.MouseLeftButtonDown += (s, e) => e.Handled = true;

            // Loaded
            this.NameTextBox.Loaded += PopupTextBox_Loaded;

            // キー入力処理
            this.NameTextBox.PreviewKeyDown += PopupTextBox_PreviewKeyDown;

            // LostFocus to close.
            this.NameTextBox.LostFocus += (s, e) =>
            {
                /*
                if (App.RemameableManager.IsSkipOnce)
                {
                    App.RemameableManager.IsSkipOnce = false;
                    return;
                }
                */

                Close(true);
            };
        }

        /// <summary>
        /// 初回キーフラグ
        /// </summary>
        private bool _isFirstKeyDowned;

        // キー入力を事前処理する
        private void PopupTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close(false);
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Close(true);
            }

            // 最初の方向入力に限りカーソル位置を固定する
            else if (IsExplorerLike)
            {
                if (!_isFirstKeyDowned && (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Up))
                {
                    int pos = this.NameTextBox.SelectionStart + this.NameTextBox.SelectionLength;
                    this.NameTextBox.Select(pos, 0);
                }
                _isFirstKeyDowned = true;
            }
        }

        /// <summary>
        /// ターゲット表示状態退避
        /// </summary>
        private Visibility _targetVisibility;

        /// <summary>
        /// ターゲットの表示状態を元に戻す
        /// </summary>
        public void RepareTargetVisibility()
        {
            if (Target != null)
            {
                Target.Visibility = _targetVisibility;
            }
        }


        /// <summary>
        /// Loaded event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PopupTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsExplorerLike)
            {
                this.NameTextBox.SelectAll();
            }

            this.NameTextBox.Focus();
        }

        /// <summary>
        /// パレットが閉じられたイベント
        /// </summary>
        public event EventHandler<PopupTextBoxClosedEventArgs> Closing;
        public event EventHandler<PopupTextBoxClosedEventArgs> Closed;

        /// <summary>
        /// 終了処理を１回だけにするためのフラグ
        /// </summary>
        private bool _isClosed;

        /// <summary>
        /// 終了処理
        /// </summary>
        /// <param name="result"></param>
        public void Close(bool result)
        {
            if (_isClosed) return;
            _isClosed = true;

            var closeEventArgs = new PopupTextBoxClosedEventArgs()
            {
                Result = result,
                Text = this.NameTextBox.Text.Trim(),
                Target = this.Target
            };

            //
            Closing?.Invoke(this, closeEventArgs);

            RepareTargetVisibility();

            //this.ReleaseMouseCapture();
            this.Visibility = Visibility.Collapsed;

            //
            if (closeEventArgs.Cancel) closeEventArgs.Result = false;
            Closed?.Invoke(this, closeEventArgs);
        }


        /// <summary>
        /// テキストボックスの左上座標
        /// </summary>
        private Point _leftTop;

        /// <summary>
        /// ターゲット変更による座標設定等の初期化
        /// </summary>
        private void UpdatePlacement()
        {
            if (Target == null) return;

            _targetVisibility = Target.Visibility;

            if (Target is TextBlock)
            {
                var targetTextBlock = (TextBlock)Target;
                this.NameTextBox.FontFamily = targetTextBlock.FontFamily;
                this.NameTextBox.FontSize = targetTextBlock.FontSize;
                this.NameTextBox.Height = targetTextBlock.Height;

                targetTextBlock.Visibility = Visibility.Hidden;
            }

            var parent = (UIElement)this.Parent;
            _leftTop = Target.TranslatePoint(new Point(-4, -2), parent);

            this.NameTextBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.NameTextBox.VerticalAlignment = VerticalAlignment.Top;

            UpdateMargin();
        }

        /// <summary>
        /// テキストボックスの表示幅更新
        /// </summary>
        private void UpdateMargin()
        {
            if (Target == null) return;

            var right = this.Root.ActualWidth - _leftTop.X - (this.MeasureText.ActualWidth + 40);
            if (right < 10) right = 10;
            this.NameTextBox.Margin = new Thickness(_leftTop.X, _leftTop.Y, right, 0);
        }

        /// <summary>
        /// サイズ変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeasureText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                UpdateMargin();
            }
        }
    }
}
