using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static beforewindeploy.DialogWindow;
using System.Windows;

namespace beforewindeploy
{
    public class OnException
    {
        public enum ErrorSelection
        {
            OfflineInstall,
            Skip,
            TryAgain
        }

        public ErrorSelection ExceptionHandler(string errorMessage, ProcessingChanges processingChanges)
        {
            DialogWindow dialogWindow = new DialogWindow($"{errorMessage}\nTry again?", "Error", false);
            dialogWindow.ShowDialog();

            // Move to offline install
            if (dialogWindow.Result == DialogMessageBoxResult.OfflineInstall)
            {
                var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to move to offline install?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (message == MessageBoxResult.Yes)
                {
                    processingChanges.MoveToOfflineInstall();
                    throw new Exception("Moving to offline install!");
                }
                else
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return ErrorSelection.TryAgain;
                }
            } // Skip
            else if (dialogWindow.Result == DialogMessageBoxResult.Skip)
            {
                var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to skip?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (message == MessageBoxResult.Yes)
                {
                    return ErrorSelection.Skip;
                }
                else
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return ErrorSelection.TryAgain;
                }
            }
            return ErrorSelection.TryAgain;
        }

        public ErrorSelection ExceptionHandlerOffline(string errorMessage, ProcessingChanges processingChanges)
        {
            DialogWindow dialogWindow = new DialogWindow($"{errorMessage}\nTry again?", "Error", true);
            dialogWindow.ShowDialog();
            if (dialogWindow.Result == DialogMessageBoxResult.Skip)
            {
                var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to skip?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (message == MessageBoxResult.Yes)
                {
                    return ErrorSelection.Skip;
                }
                else
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    processingChanges.MoveToOfflineInstall();
                    throw new Exception("Moving to offline install!");
                }
            }
            else
            {
                processingChanges.MoveToOfflineInstall();
                throw new Exception("Moving to offline install!");
            }
        }
    }
}
