using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QRCoder;
using QRCoder.Xaml;

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
            GenerateQRCode();
        }

        private class EmbeddedData
        {
            public double Version { get; set; }

            public string SerialNumber { get; set; }

            public Dictionary<string, string> Specifications { get; set; }

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
            GenerateQRCode();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (embeddedData.Issues.FirstOrDefault(x => x == checkBox.Content.ToString()) == null)
            {
                embeddedData.Issues.Add(checkBox.Name.ToString());
            }
            GenerateQRCode();
        }

        private void GenerateQRCode()
        {
            embeddedData.Specifications = SystemInfo.Get();

            // Serial Number
            ManagementObjectSearcher bios = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BIOS");
            foreach (ManagementObject obj in bios.Get())
            {
                embeddedData.SerialNumber = obj["SerialNumber"].ToString();
            }

            string data = JsonSerializer.Serialize(embeddedData);

            QRCodeGenerator qrCodeGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrCodeGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Default);
            DrawingImage qrCodeImage = new XamlQRCode(qrCodeData).GetGraphic(20);

            qrCode.Source = qrCodeImage;

            qrCodeGenerator.Dispose();
            qrCodeData.Dispose();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (embeddedData.Issues.FirstOrDefault(x => x == checkBox.Content.ToString()) != null)
            {
                embeddedData.Issues.Remove(checkBox.Name.ToString());
            }
            GenerateQRCode();
        }
    }
}
