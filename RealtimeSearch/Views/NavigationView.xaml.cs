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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NeeLaboratory.RealtimeSearch.Views
{
    /// <summary>
    /// NavigationView.xaml の相互作用ロジック
    /// </summary>
    public partial class NavigationView : UserControl
    {
        public NavigationView()
        {
            InitializeComponent();
        }


        public List<NavigationItem> ItemsSource
        {
            get { return (List<NavigationItem>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(List<NavigationItem>), typeof(NavigationView), new PropertyMetadata(null));


        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(NavigationView), new PropertyMetadata(-1));


        public object? SelectedItem
        {
            get { return (object?)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(NavigationView), new PropertyMetadata(null));
    }


    public class NavigationItem
    {
        public NavigationItem(string name, string fontIcon, FrameworkElement content)
        {
            Name = name;
            FontIcon = fontIcon;
            Content = content;
        }

        public string Name { get; init; }
        public string FontIcon { get; init; }
        public FrameworkElement Content { get; init; }
    }

}
