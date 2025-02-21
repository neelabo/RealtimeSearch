using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace NeeLaboratory.RealtimeSearch.Views
{
    /// <summary>
    /// ProgressPeriod.xaml の相互作用ロジック
    /// </summary>
    public partial class ProgressPeriod : UserControl
    {
        public ProgressPeriod()
        {
            InitializeComponent();

            this.Loaded += (s, e) => UpdateActivity();
            this.IsVisibleChanged += (s, e) => UpdateActivity();
        }


        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(ProgressPeriod), new PropertyMetadata(true, IsActiveProperty_Changed));

        private static void IsActiveProperty_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressPeriod control)
            {
                control.UpdateActivity();
            }
        }


        private void UpdateActivity()
        {
            if (IsActive && IsVisible)
            {
                var ani = new StringAnimationUsingKeyFrames();
                ani.Duration = TimeSpan.FromSeconds(2.5);
                ani.RepeatBehavior = RepeatBehavior.Forever;
                ani.KeyFrames.Add(new DiscreteStringKeyFrame(".", KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0))));
                ani.KeyFrames.Add(new DiscreteStringKeyFrame("..", KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))));
                ani.KeyFrames.Add(new DiscreteStringKeyFrame("...", KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0))));
                ani.KeyFrames.Add(new DiscreteStringKeyFrame("....", KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.5))));
                this.BusyText.BeginAnimation(TextBlock.TextProperty, ani);
            }
            else
            {
                this.BusyText.BeginAnimation(TextBlock.TextProperty, null, HandoffBehavior.SnapshotAndReplace);
            }
        }
    }

}
