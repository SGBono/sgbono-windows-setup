using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace beforewindeploy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SoundPlayer soundPlayer = new SoundPlayer();
        private MMDeviceEnumerator deviceEnumerator;
        private MMDevice defaultDevice;
        public MainWindow()
        {
            InitializeComponent();
            this.Topmost = true;
            try
            {
                soundPlayer.SoundLocation = @"C:\Windows\System32\oobe\Automation\title.wav";
                soundPlayer.PlayLooping();
            }
            catch { }
            deviceEnumerator = new MMDeviceEnumerator();
            defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (defaultDevice == null)
            {
                muteButton.IsEnabled = false;
            }
            if (defaultDevice.AudioEndpointVolume.Mute == true)
            {
                muteButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/beforewindeploy;component/speakerMute.png")),
                    Width = 30,
                    Height = 30,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };
            }
            else
            {
                muteButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/beforewindeploy;component/speakerUnmute.png")),
                    Width = 30,
                    Height = 30,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };
            }
            defaultDevice.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void usbButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessingChanges processingChanges = new ProcessingChanges(true, false, false);
            processingChanges.Show();
            this.Hide();
        }

        private void serverButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessingChanges processingChanges = new ProcessingChanges(false, true, false);
            processingChanges.Show();
            this.Hide();
        }

        private void skipButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessingChanges processingChanges = new ProcessingChanges(false, false, true);
            processingChanges.Show();
            soundPlayer.Stop();
            this.Hide();
        }

        private void muteButton_Click(object sender, RoutedEventArgs e)
        {
            defaultDevice.AudioEndpointVolume.Mute = !defaultDevice.AudioEndpointVolume.Mute;
            if (defaultDevice.AudioEndpointVolume.Mute == true)
            {
                muteButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/beforewindeploy;component/speakerMute.png")),
                    Width = 30,
                    Height = 30,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };
            } else
            {
                muteButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/beforewindeploy;component/speakerUnmute.png")),
                    Width = 30,
                    Height = 30,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };
            }
        }

        private void OnVolumeNotification(AudioVolumeNotificationData data)
        {
            // Check if invoking is required (i.e., if we're not on the UI thread)
            if (!Dispatcher.CheckAccess())
            {
                // Invoke the method on the UI thread
                Dispatcher.Invoke(() => OnVolumeNotification(data));
                return;
            }

            if (defaultDevice.AudioEndpointVolume.Mute == true)
            {
                muteButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/beforewindeploy;component/speakerMute.png")),
                    Width = 30,
                    Height = 30,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };
            }
            else
            {
                muteButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/beforewindeploy;component/speakerUnmute.png")),
                    Width = 30,
                    Height = 30,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };
            }
        }
    }
}
