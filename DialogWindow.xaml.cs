using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace beforewindeploy
{
    /// <summary>
    /// Interaction logic for DialogWindow.xaml
    /// </summary>
    public partial class DialogWindow : Window
    {
        private bool canClose = false;
        public enum DialogMessageBoxResult
        {
            TryAgain,
            OfflineInstall,
            Skip
        }

        public DialogMessageBoxResult Result { get; private set; }

        public DialogWindow(string message, string title, bool? isOffline)
        {
            InitializeComponent();
            this.BringIntoView();
            this.Title = title;
            messageContent.Text = message;
            if (isOffline == true)
            {
                offlineInstallButton.IsEnabled = false;
            }
        }

        private void tryAgainButton_Click(object sender, RoutedEventArgs e)
        {
            Result = DialogMessageBoxResult.TryAgain;
            canClose = true;
            this.Close();
        }

        private void offlineInstallButton_Click(object sender, RoutedEventArgs e)
        {
            Result = DialogMessageBoxResult.OfflineInstall;
            canClose = true;
            this.Close();
        }

        private void skipButton_Click(object sender, RoutedEventArgs e)
        {
            Result = DialogMessageBoxResult.Skip;
            canClose = true;
            this.Close();
        }

        private void messageContent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (messageContent.ActualHeight > 24)
            {
                messageContent.Margin = new Thickness(88, 25, 0, 0);
                double heightChange = e.NewSize.Height - e.PreviousSize.Height;

                this.Height += heightChange - 40;
                this.MaxHeight += heightChange - 40;
                this.MinHeight += heightChange - 40;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!canClose)
            {
                e.Cancel = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                tryAgainButton_Click(sender, e);
            }
        }
    }
}
