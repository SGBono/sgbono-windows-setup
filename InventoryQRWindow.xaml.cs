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
using System.Windows.Shapes;

namespace beforewindeploy
{
    /// <summary>
    /// Interaction logic for InventoryQRWindow.xaml
    /// </summary>
    public partial class InventoryQRWindow : Window
    {
        public InventoryQRWindow()
        {
            InitializeComponent();
        }

        private class EmbeddedData
        {
            public double Version { get; set; }

            public List<string> Issues { get; set; } = new List<string>();

            public string Remarks { get; set; }
        }

        private EmbeddedData embeddedData = new EmbeddedData();

        private void issueCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            fixList.IsEnabled = true;
        }

        private void issueCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            fixList.IsEnabled = false;
            embeddedData.Issues.Clear();
            foreach (CheckBox checkBox in fixList.Items)
            {
                checkBox.IsChecked = false;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (embeddedData.Issues.FirstOrDefault(x => x == checkBox.Content.ToString()) == null)
            {
                embeddedData.Issues.Add(checkBox.Content.ToString());
            }
        }
    }
}
