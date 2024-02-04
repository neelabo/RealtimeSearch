using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RealtimeSearchWinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            this.KeywordComboBox.Loaded += KeywordComboBox_Loaded;

            //this.WinUI3DataGrid.ItemsSource = Customer.Customers();
        }

        private void KeywordComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = this.KeywordComboBox.FindDescendant<TextBox>();
            if (textBox is not null)
            {
                textBox.TextChanged += KeywordComboBox_TextChanged;
            }
        }

        private void KeywordComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Debug.WriteLine(this.KeywordComboBox.Text);
        }

        private void TestTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }

    public class Customer
    {
        public int Id { get; private set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; private set; }

        public Customer(int id, string firstName, string lastName, string address, string postalcode)
        {
            this.Id = id;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.Address = address;
            this.PostalCode = postalcode;
        }

        public static List<Customer> Customers()
        {
            return new List<Customer>()
        {
            new Customer(1, "‡R", "‘¾˜Y", "å‘äsò‹æ‡R", "981-3205"),
            new Customer(2, "›‰ª", "Ÿ˜Y", "å‘äsò‹æ›‰ª", "981-3204"),
            new Customer(3, "‚X", "O˜Y", "å‘äsò‹æ‚X", "981-3203" ),
            new Customer(4, "Œj", "l˜Y", "å‘äsò‹æŒj", "981-3134" )
        };
        }
    }


    public static class DependencyObjectExtensions
    {
        internal static T FindDescendant<T>(this DependencyObject startNode)
          where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(startNode);
            for (int i = 0; i < count; i++)
            {
                DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
                if ((current.GetType()).Equals(typeof(T)) || (current.GetType().GetTypeInfo().IsSubclassOf(typeof(T))))
                {
                    return (T)current;
                }
                var findNode = FindDescendant<T>(current);
                if (findNode is not null)
                {
                    return findNode;
                }
            }
            return null;
        }
    }
}