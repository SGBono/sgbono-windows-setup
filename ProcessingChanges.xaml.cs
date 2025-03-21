using Microsoft.CSharp;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using static beforewindeploy.DialogWindow;
using File = System.IO.File;

namespace beforewindeploy
{
    /// <summary>
    /// Interaction logic for ProcessingChanges.xaml
    /// </summary>
    public partial class ProcessingChanges : Window
    {
        // Audio device initialisation
        private MMDeviceEnumerator deviceEnumerator;
        private MMDevice defaultDevice;

        // Load credentials from XML file
        private static XDocument credentials = XDocument.Load(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\Credentials.xml");

        public ProcessingChanges(bool usbConfig, bool serverConfig, bool skip)
        {
            InitializeComponent();

            // Audio device initialisation
            deviceEnumerator = new MMDeviceEnumerator();
            defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (defaultDevice == null) muteButton.IsEnabled = false;

            // Pass to actual installation code
            processChanges(usbConfig, serverConfig, skip);
        }

        // Determines if device has successfully connected to Wi-Fi by pinging local IP
        static async Task<bool> IsWiFiConnected()
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = await ping.SendPingAsync(credentials.Root.Element("VNCPath").Value);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        // Handles which profiles to execute based on user choice
        private async void processChanges(bool usbConfig, bool serverConfig, bool skip)
        {
            iNKORE.UI.WPF.Modern.ThemeManager.Current.ApplicationTheme = iNKORE.UI.WPF.Modern.ApplicationTheme.Dark;

            // Initialise audio status
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

            // Disable password expiry
            processingChangesLabel.Content = "Disabling password expiry on all users...";
            await Task.Delay(500);
            ProcessStartInfo disablePasswordExpiry = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = "Get-LocalUser | ForEach-Object { Set-LocalUser -Name $_.Name -PasswordNeverExpires 1 }",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            await Task.Run(() => Process.Start(disablePasswordExpiry).WaitForExit());

            // Skip button pressed
            if (usbConfig == false && serverConfig == false)
            {
                processingChangesLabel.Content = "Cleaning up...";
                File.Delete(Environment.SystemDirectory + @"\TaskbarLayoutModifcation.xml");
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory) + @"\Shutdown PC.lnk");
                RegistryKey localMachine = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", true);
                CleanUp();
            }

            // USB Config button pressed
            if (usbConfig == true)
            {
                USBConfig();
            }

            // Server Config button pressed
            else if (serverConfig == true)
            {
                processingChangesLabel.Content = "Terminating Explorer...";
                await Task.Delay(500);
                Process terminateExplorer = new Process();
                terminateExplorer.StartInfo.FileName = "taskkill.exe";
                terminateExplorer.StartInfo.Arguments = "/im explorer.exe /f";
                terminateExplorer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                terminateExplorer.StartInfo.CreateNoWindow = true;
                terminateExplorer.Start();

                bool networkDone = false;
                while (networkDone == false)
                {
                    try
                    {
                        try
                        {
                            processingChangesLabel.Content = "Connecting to the network...";
                            await Task.Delay(500);
                            using (Process process = new Process())
                            {
                                ProcessStartInfo startInfo = new ProcessStartInfo
                                {
                                    FileName = "netsh",
                                    RedirectStandardInput = true,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    RedirectStandardError = true
                                };

                                process.StartInfo = startInfo;
                                process.Start();

                                // Create temp directory and write WiFiTemplate.xml
                                Directory.CreateDirectory(@"C:\SGBono\Windows 11 Debloated");
                                File.WriteAllText(@"C:\SGBono\Windows 11 Debloated\WiFiTemplate.xml", Properties.Resources.WiFiTemplate);

                                // Get reference values from Credentials.xml
                                var ssid = credentials.Root.Element("Router").Element("SSID").Value;
                                var routerpassword = credentials.Root.Element("Router").Element("Password").Value;
                                var securityprotocol = credentials.Root.Element("Router").Element("SecurityProtocol").Value;

                                // Writes reference values to WiFiTemplate.xml
                                XNamespace xmlNamespace = "http://www.microsoft.com/networking/WLAN/profile/v1";
                                XDocument wifiTemplate = XDocument.Load(@"C:\SGBono\Windows 11 Debloated\WiFiTemplate.xml");
                                wifiTemplate.Root.Element(xmlNamespace + "name").Value = ssid;
                                wifiTemplate.Root.Element(xmlNamespace + "SSIDConfig").Element(xmlNamespace + "SSID").Element(xmlNamespace + "name").Value = ssid;
                                wifiTemplate.Root.Element(xmlNamespace + "MSM").Element(xmlNamespace + "security").Element(xmlNamespace + "sharedKey").Element(xmlNamespace + "keyMaterial").Value = routerpassword;
                                wifiTemplate.Root.Element(xmlNamespace + "MSM").Element(xmlNamespace + "security").Element(xmlNamespace + "authEncryption").Element(xmlNamespace + "authentication").Value = securityprotocol;
                                wifiTemplate.Save(@"C:\SGBono\Windows 11 Debloated\WiFiTemplate.xml");

                                // Connect to network by importing WiFiTemplate.xml
                                process.StandardInput.WriteLine("wlan add profile filename=\"C:\\SGBono\\Windows 11 Debloated\\WiFiTemplate.xml\"");
                                process.StandardInput.Close();

                                ProcessStartInfo connectInfo = new ProcessStartInfo
                                {
                                    FileName = "netsh",
                                    Arguments = $"wlan connect name=\"{ssid}\"",
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };

                                // Ensures network connection is successful
                                Process connectProcess = new Process { StartInfo = connectInfo };
                                connectProcess.Start();
                                connectProcess.WaitForExit();
                                processingChangesLabel.Content = "Waiting for the network...";

                                int attempts = 0;
                                while (!await IsWiFiConnected())
                                {
                                    if (attempts == 31)
                                    {
                                        throw new Exception("The network connection timed out.");
                                    }
                                    else
                                    {
                                        await Task.Delay(500);
                                        attempts++;
                                    }
                                }

                                networkDone = true;
                                process.WaitForExit();
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (new OnException().ExceptionHandler($"There was an error connecting to the network. {ex.Message}", this) == OnException.ErrorSelection.Skip) networkDone = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Dummy try-catch block to stop execution of code and move to offline install
                        if (ex.Message == "Moving to offline install!")
                        {
                            return;
                        }
                    }
                }

                // Launch driver installation utility
                bool driversDone = false;
                while (driversDone == false)
                {
                    try
                    {
                        try
                        {
                            processingChangesLabel.Content = "Connecting to the server...";
                            await Task.Delay(500);
                            await Task.Run(() =>
                            {
                                var serverCredential = credentials.Root;
                                var serverUsername = credentials.Root.Element("Username").Value;
                                var serverPassword = credentials.Root.Element("Password").Value;
                                Process mountNetworkDrive = new Process();
                                mountNetworkDrive.StartInfo.FileName = "net.exe";
                                mountNetworkDrive.StartInfo.Arguments = $@"use Z: \\{credentials.Root.Element("VNCPath").Value}\Drivers /user:{serverUsername} {serverPassword}";
                                mountNetworkDrive.StartInfo.UseShellExecute = false;
                                mountNetworkDrive.StartInfo.RedirectStandardOutput = true;
                                mountNetworkDrive.StartInfo.CreateNoWindow = true;
                                mountNetworkDrive.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                mountNetworkDrive.Start();
                                mountNetworkDrive.WaitForExit();
                            });


                            processingChangesLabel.Content = "Launching driver installation utility...";
                            await Task.Delay(500);
                            await Task.Run(() =>
                            {
                                ProcessStartInfo driverInstallServer = new ProcessStartInfo();
                                driverInstallServer.FileName = "cmd.exe";
                                driverInstallServer.Arguments = @"/c SDI_x64_R2309.exe";
                                driverInstallServer.CreateNoWindow = true;
                                driverInstallServer.WindowStyle = ProcessWindowStyle.Hidden;
                                driverInstallServer.UseShellExecute = false;
                                driverInstallServer.WorkingDirectory = @"Z:";

                                Process.Start(driverInstallServer).WaitForExit();
                            });

                            // There are cases in which the driver utility does not show the missing drivers list correctly when first launched for unknown reasons.
                            // This block of code will ask the user if the driver utility has launched correctly and prompt the user to relaunch if it has not.
                            var result = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Did the driver installation utility launch correctly?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result == MessageBoxResult.Yes)
                            {
                                driversDone = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (new OnException().ExceptionHandler($"There was an error accessing the server used to host the drivers. {ex.Message}", this) == OnException.ErrorSelection.Skip) driversDone = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Dummy try-catch block to stop execution of code and move to offline install
                        if (ex.Message == "Moving to offline install!")
                        {
                            return;
                        }
                    }
                }

                // Install apps
                try
                {
                    var credential = credentials.Root;
                    var username = credential.Element("Username").Value;
                    var password = credential.Element("Password").Value;
                    Process mountNetworkDrive3 = new Process();
                    mountNetworkDrive3.StartInfo.FileName = "net.exe";
                    mountNetworkDrive3.StartInfo.Arguments = $@"use Y: \\{credentials.Root.Element("VNCPath").Value}\Software /user:{username} {password}";
                    mountNetworkDrive3.StartInfo.UseShellExecute = false;
                    mountNetworkDrive3.StartInfo.RedirectStandardOutput = true;
                    mountNetworkDrive3.StartInfo.CreateNoWindow = true;
                    mountNetworkDrive3.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    mountNetworkDrive3.Start();
                    mountNetworkDrive3.WaitForExit();
                    XDocument programsList = XDocument.Load(@"Y:\ProgramsList.xml");

                    // Iterate through XML file on server and install every program
                    foreach (var program in programsList.Root.Elements())
                    {
                        bool programInstalled = false;
                        var name = program.Element("name").Value;
                        var path = program.Element("path").Value;
                        var run = program.Element("run").Value;
                        var arguments = program.Element("arguments").Value;
                        var customcscode = program.Element("customcscode").Value;
                        while (programInstalled == false)
                        {
                            try
                            {
                                processingChangesLabel.Content = $"Installing {name}...";
                                await Task.Delay(500);

                                // If path is not empty, run the installer using the path element
                                if (!string.IsNullOrEmpty(path))
                                {
                                    Process setup = new Process();
                                    setup.StartInfo.FileName = $@"Y:\{path}";
                                    setup.StartInfo.Arguments = arguments;
                                    setup.StartInfo.CreateNoWindow = true;
                                    setup.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    await Task.Run(() =>
                                    {
                                        setup.Start();
                                        setup.WaitForExit();
                                    });
                                }
                                else
                                {
                                    // If path is empty, run the installer using the run element
                                    if (string.IsNullOrEmpty(run))
                                    {
                                        throw new Exception("The XML file was not configured correctly - both path and run elements are missing.");
                                    }
                                    Process setup = new Process();
                                    setup.StartInfo.FileName = $@"{run}";
                                    setup.StartInfo.Arguments = arguments;
                                    setup.StartInfo.CreateNoWindow = true;
                                    setup.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    await Task.Run(() =>
                                    {
                                        setup.Start();
                                        setup.WaitForExit();
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                if (new OnException().ExceptionHandler($"There was an error installing {name}. {ex.Message}", this) == OnException.ErrorSelection.Skip) programInstalled = true;
                            }

                            // Run custom C# code if it exists
                            processingChangesLabel.Content = $"Running package scripts for {name}...";
                            await Task.Delay(500);
                            await Task.Run(() =>
                            {
                                if (string.IsNullOrEmpty(customcscode))
                                {
                                    programInstalled = true;
                                }
                                else
                                {
                                    CompilerParameters parameters = new CompilerParameters();
                                    parameters.ReferencedAssemblies.Add("System.dll");
                                    parameters.ReferencedAssemblies.Add("System.Core.dll");
                                    parameters.ReferencedAssemblies.Add(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\Interop.IWshRuntimeLibrary.dll");
                                    parameters.GenerateInMemory = true;
                                    CompilerResults results = new CSharpCodeProvider().CompileAssemblyFromSource(parameters, customcscode);

                                    // Error handling
                                    if (results.Errors.Count > 0)
                                    {
                                        foreach (CompilerError error in results.Errors)
                                        {
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                if (new OnException().ExceptionHandler($"There was an error running package scripts for {name}. {error.ErrorText}.", this) == OnException.ErrorSelection.Skip) programInstalled = true;
                                            });
                                        }
                                    }
                                    else
                                    {
                                        // Run custom C# code if there are no errors
                                        Type customType = results.CompiledAssembly.GetType("CustomCode");
                                        MethodInfo method = customType.GetMethod("Execute");
                                        method.Invoke(null, null);
                                        programInstalled = true;
                                    }
                                }
                            });
                        }
                    };
                }
                catch (Exception ex)
                {
                    // Dummy try-catch block to stop execution of code and move to offline install
                    if (ex.Message == "Moving to offline install!")
                    {
                        return;
                    }
                }

                GenerateSystemReport();

                try
                {
                    // Clean up
                    CleanUp();
                }
                catch
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("There was an error cleaning up. Please do so manually.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // USB Config
        private async void USBConfig()
        {
            try
            {
                bool driversDone = false;
                bool ignoreError = false;
                while (driversDone == false)
                {
                    try
                    {
                        processingChangesLabel.Content = "Launching driver install utility...";
                        await Task.Delay(500);

                        // Iterate through all drive letters and search for drivers disk
                        for (char c = 'D'; c <= 'Z'; c++)
                        {
                            try
                            {
                                if (File.Exists(c + @":\Drivers\SDI_x64_R2309.exe"))
                                {
                                    Process process = new Process();
                                    process.StartInfo.FileName = "cmd.exe";
                                    process.StartInfo.Arguments = "/c SDI_x64_R2309.exe";
                                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    process.StartInfo.CreateNoWindow = true;
                                    process.StartInfo.WorkingDirectory = c + @":\Drivers";
                                    process.Start();
                                    await Task.Delay(10000);
                                    try
                                    {
                                        await Task.Run(() =>
                                        {
                                            Process sdiProcess = Process.GetProcessesByName("SDI_x64_R2309").FirstOrDefault();
                                            sdiProcess.WaitForExit();
                                            driversDone = true;
                                        });
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        if (new OnException().ExceptionHandlerOffline($"An error occurred while attempting to launch the driver install utility. {ex.Message}", this) == OnException.ErrorSelection.Skip) driversDone = true;
                                        else ignoreError = true;
                                    }
                                }
                                else if (c == 'Z')
                                {
                                    if (ignoreError == true)
                                    {
                                        break;
                                    }

                                    if (new OnException().ExceptionHandler("Your installation USB does not come with drivers.", this) == OnException.ErrorSelection.Skip) driversDone = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (c == 'Z')
                                {
                                    if (ex.Message == "Moving to offline install!")
                                    {
                                        return;
                                    }

                                    if (new OnException().ExceptionHandler("Your installation USB does not come with drivers.", this) == OnException.ErrorSelection.Skip) driversDone = true;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "Moving to offline install!")
                        {
                            return;
                        }

                        if (new OnException().ExceptionHandler("Your installation USB does not come with drivers.", this) == OnException.ErrorSelection.Skip) driversDone = true;
                    }

                    // System report generation
                    GenerateSystemReport();

                    try
                    {
                        processingChangesLabel.Content = "Starting Explorer...";
                        await Task.Delay(500);
                        Process.Start("explorer.exe");
                        await Task.Delay(5000);
                    }
                    catch
                    {
                        iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("There was an error launching Explorer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    try
                    {
                        // Clean up
                        CleanUp();
                    }
                    catch
                    {
                        iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("There was an error cleaning up. Please do so manually.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "Moving to offline install!")
                {
                    return;
                }
            }

        }

        private async void GenerateSystemReport()
        {
            try
            {
                // System report generation
                processingChangesLabel.Content = "Generating system report...";
                await Task.Delay(500);

                Dictionary<string, string> specifications = await Task.Run(() => SystemInfo.Get());
                string cpuName = specifications["CPU"];
                string gpuName = specifications["GPU"];
                string ramInfo = specifications["RAM"];
                string storageSize = specifications["Storage"];
                string batteryHealth = specifications["Battery Health"];
                string designCapacity = specifications["Design Capacity"];
                string fullChargeCapacity = specifications["Full Charge Capacity"];

                // Save system report to desktop
                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\System Report.txt", $"======System Report======\n\n<Note the following down>\nCPU: {cpuName}\nGPU: {gpuName}\nRAM: {ramInfo}\nStorage on Windows drive: {storageSize}\nBattery health: {batteryHealth}\n\n<Additional information>\nOriginal battery capacity: {Math.Round(Convert.ToDouble(designCapacity) / 1000)}Wh\nFull charge capacity: {Math.Round(Convert.ToDouble(fullChargeCapacity) / 1000)}Wh");
                Process.Start("notepad.exe", Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\System Report.txt");
            }
            catch
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("There was an error generating the system report.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal void MoveToOfflineInstall()
        {
            bool validDriverUSB = false;
            while (validDriverUSB == false)
            {
                // Check if driver disk and required files are present
                var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Please insert the disk labelled Drivers and click OK.", "Insert Disk", MessageBoxButton.OK, MessageBoxImage.Information);
                if (message == MessageBoxResult.OK)
                {
                    for (char c = 'D'; c <= 'Z'; c++)
                    {
                        try
                        {
                            if (File.Exists(c + @":\Drivers\SDI_x64_R2309.exe"))
                            {
                                USBConfig();
                                validDriverUSB = true;
                                break;
                            }
                            else if (c == 'Z')
                            {
                                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("You have not inserted a valid Drivers disk.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        catch
                        {
                            if (c == 'Z')
                            {
                                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("You have not inserted a valid Drivers disk.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
        }

        private async void CleanUp()
        {
            processingChangesLabel.Content = "Cleaning Up...";
            await Task.Delay(500);
            Process process = new Process();
            process.StartInfo.FileName = "netsh";
            process.StartInfo.Arguments = "wlan delete profile name=\"SST-External\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            Process process2 = new Process();
            process2.StartInfo.FileName = "netsh";
            process2.StartInfo.Arguments = $"wlan delete profile name=\"{credentials.Root.Element("Router").Element("SSID").Value}\"";
            process2.StartInfo.RedirectStandardOutput = true;
            process2.StartInfo.UseShellExecute = false;
            process2.StartInfo.CreateNoWindow = true;
            process2.Start();
            process2.WaitForExit();
            Process process4 = new Process();
            process4.StartInfo.FileName = "cmd.exe";
            process4.StartInfo.Arguments = "/c net use /delete Z:";
            process4.StartInfo.RedirectStandardOutput = true;
            process4.StartInfo.UseShellExecute = false;
            process4.StartInfo.CreateNoWindow = true;
            process4.Start();
            process4.WaitForExit();
            Directory.Delete(@"C:\SGBono", true);
            while (Directory.Exists(@"C:\SGBono"))
            {
                await Task.Delay(500);
            }
            await Task.Delay(5000);
            Process process3 = new Process();
            process3.StartInfo.FileName = "cmd.exe";
            process3.StartInfo.Arguments = $@"/c timeout /t 10 && rd ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}"" /s /q";
            process3.StartInfo.CreateNoWindow = true;
            process3.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process3.Start();
            await Task.Delay(5000);
            defaultDevice.AudioEndpointVolume.Mute = false;
            Process.GetCurrentProcess().Kill();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            var result = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to exit?", "Exit", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var result2 = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Do you want to restart the app?", "Exit", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result2 == MessageBoxResult.Yes)
                {
                    Process process = new Process();
                    process.StartInfo.FileName = Assembly.GetExecutingAssembly().Location;
                    process.StartInfo.Verb = "runas";
                    process.Start();
                }
                Process.GetCurrentProcess().Kill();
            }
        }

        // Event handler for when the mute button is clicked
        private void muteButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle the mute state of the default audio device
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

        // Event handler for when the default device volume changes
        private void OnVolumeNotification(AudioVolumeNotificationData data)
        {
            // Check if invoking is required (i.e., if we're not on the UI thread)
            if (!Dispatcher.CheckAccess())
            {
                // Invoke the method on the UI thread
                Dispatcher.Invoke(() => OnVolumeNotification(data));
                return;
            }

            // Toggle the mute state of the default audio device
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
