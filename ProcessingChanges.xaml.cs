using IWshRuntimeLibrary;
using Microsoft.CSharp;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
        [DllImport("user32.dll")]
        private static extern IntPtr DestroyMenu(IntPtr hWnd);

        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint SC_CLOSE = 0xF060;

        IntPtr menuHandle;

        private MMDeviceEnumerator deviceEnumerator;
        private MMDevice defaultDevice;
        public ProcessingChanges(bool usbConfig, bool serverConfig, bool skip)
        {
            InitializeComponent();
            deviceEnumerator = new MMDeviceEnumerator();
            defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (defaultDevice == null) muteButton.IsEnabled = false;
            processChanges(usbConfig, serverConfig, skip);
        }

        static async Task<bool> IsWiFiConnected()
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = await ping.SendPingAsync("192.168.0.1");
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

        private async void processChanges(bool usbConfig, bool serverConfig, bool skip)
        {
            iNKORE.UI.WPF.Modern.ThemeManager.Current.ApplicationTheme = iNKORE.UI.WPF.Modern.ApplicationTheme.Dark;
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

            if (usbConfig == false && serverConfig == false)
            {
                /*try
                {
                    processingChangesLabel.Content = "Disabling SMBv1...";
                    await Delay(500);
                    await Task.Run(() =>
                    {
                        Process disableSMB = new Process();
                        disableSMB.StartInfo.FileName = "dism.exe";
                        disableSMB.StartInfo.Arguments = "/online /disable-feature /featurename:SMB1Protocol /norestart";
                        disableSMB.StartInfo.CreateNoWindow = true;
                        disableSMB.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        disableSMB.Start();
                        disableSMB.WaitForExit();
                    });
                }
                catch
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("There was an error disabling SMBv1. You will need to disable it manually.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }*/
                processingChangesLabel.Content = "Cleaning up...";
                File.Delete(@"C:\Windows\System32\TaskbarLayoutModifcation.xml");
                File.Delete(@"C:\Users\Public\Desktop\Shutdown PC.lnk");
                RegistryKey localMachine = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", true);
                localMachine.DeleteValue("LayoutXMLPath");
                localMachine.Dispose();
                Process process3 = new Process();
                process3.StartInfo.FileName = "cmd.exe";
                process3.StartInfo.Arguments = @"/c timeout /t 10 && rd ""C:\Windows\System32\oobe\Automation"" /s /q";
                process3.StartInfo.CreateNoWindow = true;
                process3.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process3.Start();
                await Delay(5000);
                Process.GetCurrentProcess().Kill();
            }
            if (usbConfig == true)
            {
                USBConfig();
            }
            else
            if (serverConfig == true)
            {
                processingChangesLabel.Content = "Terminating Explorer...";
                await Delay(500);
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
                            await Delay(500);
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
                                Directory.CreateDirectory(@"C:\SGBono\Windows 11 Debloated");
                                File.WriteAllText(@"C:\SGBono\Windows 11 Debloated\SGBono Internal.xml", Properties.Resources.SGBono);
                                process.StandardInput.WriteLine("wlan add profile filename=\"C:\\SGBono\\Windows 11 Debloated\\SGBono Internal.xml\"");
                                process.StandardInput.WriteLine($"wlan connect name=\"SGBono Internal\" ssid=\"SGBono Internal\" interface=\"Wi-Fi\"");
                                process.StandardInput.Close();

                                ProcessStartInfo connectInfo = new ProcessStartInfo
                                {
                                    FileName = "netsh",
                                    Arguments = "wlan connect name=\"SGBono Internal\"",
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };

                                Process connectProcess = new Process { StartInfo = connectInfo };
                                connectProcess.Start();
                                connectProcess.WaitForExit();
                                processingChangesLabel.Content = "Waiting for the network...";
                                networkDone = true;

                                int attempts = 0;
                                while (!await IsWiFiConnected())
                                {
                                    if (attempts == 31)
                                    {
                                        throw new Exception("The network connection timed out.");
                                    }
                                    else
                                    {
                                        await Delay(500);
                                        attempts++;
                                    }
                                }

                                process.WaitForExit();
                                break;


                            }
                        }
                        catch (Exception ex)
                        {
                            DialogWindow dialogWindow = new DialogWindow("There was an error connecting to the network. " + ex.Message + "\nTry again?", "Error", false);
                            dialogWindow.ShowDialog();
                            if (dialogWindow.Result == DialogMessageBoxResult.OfflineInstall)
                            {
                                var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to move to offline install?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                if (message == MessageBoxResult.Yes)
                                {
                                    MoveToOfflineInstall();
                                    throw new Exception("Moving to offline install!");
                                }
                                else
                                {
                                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                            else if (dialogWindow.Result == DialogMessageBoxResult.Skip)
                            {
                                var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to skip?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                if (message == MessageBoxResult.Yes)
                                {
                                    networkDone = true;
                                    break;
                                }
                                else
                                {
                                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                        }
                    } catch (Exception ex)
                    {
                        if (ex.Message == "Moving to offline install!")
                        {
                            return;
                        }
                    }
                }

                bool driversDone = false;

                while (driversDone == false)
                {
                    try
                    {
                        try
                        {
                            processingChangesLabel.Content = "Connecting to the server...";
                            await Delay(500);
                            XDocument serverCredentials = XDocument.Load(Environment.SystemDirectory + @"\oobe\Automation\Credentials.xml");
                            await Task.Run(() =>
                            {
                                var serverCredential = serverCredentials.Root;
                                var serverUsername = serverCredential.Element("Username").Value;
                                var serverPassword = serverCredential.Element("Password").Value;
                                Process mountNetworkDrive = new Process();
                                mountNetworkDrive.StartInfo.FileName = "net.exe";
                                mountNetworkDrive.StartInfo.Arguments = $@"use Z: \\SGBonoServ\Drivers /user:{serverUsername} {serverPassword}";
                                mountNetworkDrive.StartInfo.UseShellExecute = false;
                                mountNetworkDrive.StartInfo.RedirectStandardOutput = true;
                                mountNetworkDrive.StartInfo.CreateNoWindow = true;
                                mountNetworkDrive.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                mountNetworkDrive.Start();
                                mountNetworkDrive.WaitForExit();
                            });


                            processingChangesLabel.Content = "Launching driver installation utility...";
                            await Delay(500);
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

                            var result = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Did the driver installation utility launch correctly?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result == MessageBoxResult.No)
                            {
                                if (Process.GetProcessesByName("SDI_x64_R2309.exe").Length > 0)
                                {
                                    Process.GetProcessesByName("SDI_x64_R2309.exe").First().Kill();
                                }
                                processingChangesLabel.Content = "Connecting to the server (2nd attempt)...";
                                await Delay(500);
                                //XDocument serverCredentials = XDocument.Load(@"C:\Windows\System32\oobe\Automation\Credentials.xml");
                                var serverCredential = serverCredentials.Root;
                                var serverUsername = serverCredential.Element("Username").Value;
                                var serverPassword = serverCredential.Element("Password").Value;
                                Process mountNetworkDrive2 = new Process();
                                mountNetworkDrive2.StartInfo.FileName = "net.exe";
                                mountNetworkDrive2.StartInfo.Arguments = $@"use Z: \\SGBonoServ\Drivers /user:{serverUsername} {serverPassword}";
                                mountNetworkDrive2.StartInfo.UseShellExecute = false;
                                mountNetworkDrive2.StartInfo.RedirectStandardOutput = true;
                                mountNetworkDrive2.StartInfo.CreateNoWindow = true;
                                mountNetworkDrive2.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                mountNetworkDrive2.Start();

                                processingChangesLabel.Content = "Launching driver installation utility (2nd attempt)...";
                                await Delay(500);
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
                            }
                            else
                            {
                                driversDone = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            DialogWindow dialogWindow = new DialogWindow("There was an error accessing the server used to host the drivers. " + ex.Message + "\nTry again?", "Error", false);
                            dialogWindow.ShowDialog();
                            if (dialogWindow.Result == DialogMessageBoxResult.OfflineInstall)
                            {
                                var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to move to offline install?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                if (message == MessageBoxResult.Yes)
                                {
                                    MoveToOfflineInstall();
                                    throw new Exception("Moving to offline install!");
                                }
                                else
                                {
                                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                            else if (dialogWindow.Result == DialogMessageBoxResult.Skip)
                            {
                                var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to skip?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                if (message == MessageBoxResult.Yes)
                                {
                                    driversDone = true;
                                }
                                else
                                {
                                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                        }
                    } catch (Exception ex)
                    {
                        if (ex.Message == "Moving to offline install!")
                        {
                            return;
                        }
                    }
                }

                // Install apps
                XDocument credentials = XDocument.Load(Environment.SystemDirectory + @"\oobe\Automation\Credentials.xml");
                var credential = credentials.Root;
                var username = credential.Element("Username").Value;
                var password = credential.Element("Password").Value;
                Process mountNetworkDrive3 = new Process();
                mountNetworkDrive3.StartInfo.FileName = "net.exe";
                mountNetworkDrive3.StartInfo.Arguments = $@"use Y: \\SGBonoServ\Software /user:{username} {password}";
                mountNetworkDrive3.StartInfo.UseShellExecute = false;
                mountNetworkDrive3.StartInfo.RedirectStandardOutput = true;
                mountNetworkDrive3.StartInfo.CreateNoWindow = true;
                mountNetworkDrive3.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                mountNetworkDrive3.Start();
                mountNetworkDrive3.WaitForExit();
                XDocument programsList = XDocument.Load(@"Y:\ProgramsList.xml");
                try
                {
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
                                await Delay(500);
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
                                DialogWindow dialogWindow = new DialogWindow($"There was an error installing {name}. " + ex.Message + "\nTry again?", "Error", false);
                                dialogWindow.ShowDialog();
                                if (dialogWindow.Result == DialogMessageBoxResult.OfflineInstall)
                                {
                                    var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to move to offline install?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                    if (message == MessageBoxResult.Yes)
                                    {
                                        MoveToOfflineInstall();
                                        throw new Exception("Moving to offline install!");
                                    }
                                    else
                                    {
                                        iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                    }
                                }
                                else if (dialogWindow.Result == DialogMessageBoxResult.Skip)
                                {
                                    var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to skip?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                    if (message == MessageBoxResult.Yes)
                                    {
                                        programInstalled = true;
                                    }
                                    else
                                    {
                                        iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                    }
                                }
                            }
                            processingChangesLabel.Content = $"Running package scripts for {name}...";
                            await Delay(500);
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
                                    parameters.ReferencedAssemblies.Add(Environment.SystemDirectory + @"\oobe\Automation\Interop.IWshRuntimeLibrary.dll");
                                    parameters.GenerateInMemory = true;
                                    CompilerResults results = new CSharpCodeProvider().CompileAssemblyFromSource(parameters, customcscode);
                                    if (results.Errors.Count > 0)
                                    {
                                        foreach (CompilerError error in results.Errors)
                                        {
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                DialogWindow dialogWindow = new DialogWindow($"There was an error running package scripts for {name}. " + error.ErrorText + ". \nTry again?", "Error", false);
                                                dialogWindow.ShowDialog();
                                                if (dialogWindow.Result == DialogMessageBoxResult.OfflineInstall)
                                                {
                                                    var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to move to offline install?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                                    if (message == MessageBoxResult.Yes)
                                                    {
                                                        MoveToOfflineInstall();
                                                        throw new Exception("Moving to offline install!");
                                                    }
                                                    else
                                                    {
                                                        iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                                    }
                                                }
                                                else if (dialogWindow.Result == DialogMessageBoxResult.Skip)
                                                {
                                                    var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to skip?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                                    if (message == MessageBoxResult.Yes)
                                                    {
                                                        programInstalled = true;
                                                    }
                                                    else
                                                    {
                                                        iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                                    }
                                                }
                                            });
                                        }
                                    }
                                    else
                                    {
                                        Type customType = results.CompiledAssembly.GetType("CustomCode");
                                        MethodInfo method = customType.GetMethod("Execute");
                                        method.Invoke(null, null);
                                        programInstalled = true;
                                    }
                                }
                            });
                        }
                    };
                } catch (Exception ex)
                {
                    if (ex.Message == "Moving to offline install!")
                    {
                        return;
                    }
                }

                try
                {
                    processingChangesLabel.Content = "Generating system report...";
                    await Delay(500);

                    string cpuName = "";
                    string gpuName = "";
                    string ramInfo = "";
                    string storageSize = "";
                    string batteryHealth = "";

                    //CPU
                    ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
                    foreach (ManagementObject mo in mos.Get())
                    {
                        cpuName = "CPU: " + (string)mo["Name"];
                    }

                    //GPU
                    bool hasiGPU = false;
                    string iGPUName = "";
                    using (var searcher1 = new ManagementObjectSearcher("select * from Win32_VideoController"))
                    {
                        foreach (ManagementObject obj in searcher1.Get())
                        {
                            if (obj["Name"].ToString() == "Microsoft Basic Display Adapter")
                            {
                                gpuName = "GPU: No GPU drivers installed";
                            }
                            //Improved iGPU detector - should work theoretically though this requires testing
                            else if (obj["Name"].ToString() == "AMD Radeon(TM) Graphics" || obj["Name"].ToString().Contains("Intel") && !obj["Name"].ToString().Contains("Intel Arc") && obj["Name"].ToString() != "Intel(R) Arc(TM) Graphics")
                            {
                                hasiGPU = true;
                                iGPUName = obj["Name"].ToString();
                            }
                            else
                            {
                                gpuName = "GPU: " + (string)obj["Name"];
                                break;
                            }
                        }
                        if (gpuName == "" && hasiGPU == true)
                        {
                            gpuName = "GPU: " + iGPUName + "(iGPU)";
                        }
                    }

                    //RAM
                    ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                    ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(objectQuery);

                    ManagementObjectSearcher searcher2 = new ManagementObjectSearcher("Select * from Win32_PhysicalMemory");
                    var ramspeed = "";
                    var newram = 0;
                    var newMemoryType = "";
                    foreach (ManagementObject obj in searcher2.Get())
                    {
                        try
                        {
                            ramspeed = Convert.ToString(obj["ConfiguredClockSpeed"]);
                        }
                        catch { }
                    }
                    foreach (ManagementObject managementObject in managementObjectSearcher.Get())
                    {
                        newram = Convert.ToInt32(managementObject["TotalVisibleMemorySize"]) / 1000 / 1000;
                    }
                    foreach (ManagementObject managementObject in searcher2.Get())
                    {
                        string memoryType = managementObject["MemoryType"].ToString();
                        switch (memoryType)
                        {
                            case "20":
                                newMemoryType = "DDR";
                                break;
                            case "21":
                                newMemoryType = "DDR2";
                                break;
                            case "24":
                                newMemoryType = "DDR3";
                                break;
                            case "26":
                                newMemoryType = "DDR4";
                                break;
                            case "34":
                                newMemoryType = "DDR5";
                                break;
                            case "0":
                                string memoryType2 = managementObject["SMBIOSMemoryType"]?.ToString() ?? "0";
                                if (memoryType2 == "34")
                                {
                                    newMemoryType = "DDR5";
                                }
                                else if (memoryType2 == "20")
                                {
                                    newMemoryType = "DDR";
                                }
                                else if (memoryType2 == "21")
                                {
                                    newMemoryType = "DDR2";
                                }
                                else if (memoryType2 == "24")
                                {
                                    newMemoryType = "DDR3";
                                }
                                else if (memoryType2 == "26")
                                {
                                    newMemoryType = "DDR4";
                                }
                                else
                                {
                                    newMemoryType = "Unknown";
                                }
                                break;
                            default:
                                newMemoryType = "Unknown";
                                break;
                        }
                    }
                    if (ramspeed == null || ramspeed == "" || ramspeed == "0")
                    {
                        ramspeed = "Unknown ";
                    }
                    else if (newMemoryType == "Unknown")
                    {
                        //Last last resort RAM type check
                        //Banking on nobody being able to reach 4800 MT/s on DDR4 (DDR5 JEDEC = 4800 MT/s)
                        //Also not considering the LPDDR5/LPDDR5x users
                        if (Convert.ToInt32(ramspeed) >= 4800)
                        {
                            newMemoryType = "DDR5";
                        }
                    }

                    ramInfo = "RAM: " + newram + "GB " + ramspeed + "MT/s " + newMemoryType;

                    //Storage
                    DriveInfo mainDrive = new DriveInfo(System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)));
                    var totalsize = mainDrive.TotalSize / 1000 / 1000 / 1000;
                    storageSize = "Storage on Windows drive: " + totalsize + "GB";
                    await Delay(200);

                    //Battery health
                    ManagementObjectSearcher batteryStaticData = new ManagementObjectSearcher("root/WMI", "SELECT * FROM BatteryStaticData");
                    ManagementObjectSearcher batteryFullChargedCapacity = new ManagementObjectSearcher("root/WMI", "SELECT * FROM BatteryFullChargedCapacity");

                    int designCapacity = 0;
                    int fullChargeCapacity = 0;

                    try
                    {
                        foreach (ManagementObject queryObj in batteryStaticData.Get())
                        {
                            designCapacity = Convert.ToInt32(queryObj["DesignedCapacity"]);
                        }

                        foreach (ManagementObject queryObj in batteryFullChargedCapacity.Get())
                        {
                            fullChargeCapacity = Convert.ToInt32(queryObj["FullChargedCapacity"]);
                        }
                    }
                    catch
                    {
                        batteryHealth = "Battery health: No battery detected";
                    }

                    if (designCapacity == 0 || fullChargeCapacity == 0)
                    {
                        batteryHealth = "Battery health: No battery detected";
                    }
                    else
                    {
                        double batteryHealthPercentage = Math.Round((double)fullChargeCapacity / designCapacity * 100, 1);
                        batteryHealth = "Battery health: " + batteryHealthPercentage + "%";
                    }

                    File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\System Report.txt", $"======System Report======\n\n<Note the following down>\n{cpuName}\n{gpuName}\n{ramInfo}\n{storageSize}\n{batteryHealth}\n\n<Additional information>\nOriginal battery capacity: {Math.Round((double)designCapacity / 1000)}Wh\nFull charge capacity: {Math.Round((double)fullChargeCapacity / 1000)}Wh");
                    Process.Start("notepad.exe", Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\System Report.txt").WaitForExit();
                    //iNKORE.UI.WPF.Modern.Controls.MessageBox.Show($"The system report has been saved to {Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\System Report.txt"}.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("There was an error generating the system report.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                /*try
                {
                    processingChangesLabel.Content = "Disabling SMBv1...";
                    await Delay(500);
                    await Task.Run(() =>
                    {
                        Process disableSMB = new Process();
                        disableSMB.StartInfo.FileName = "dism.exe";
                        disableSMB.StartInfo.Arguments = "/online /disable-feature /featurename:SMB1Protocol /norestart";
                        disableSMB.StartInfo.CreateNoWindow = true;
                        disableSMB.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        disableSMB.Start();
                        disableSMB.WaitForExit();
                    });
                }
                catch
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("There was an error disabling SMBv1. You will need to disable it manually.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }*/

                try
                {
                    processingChangesLabel.Content = "Cleaning Up...";
                    await Delay(500);
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
                    process2.StartInfo.Arguments = "wlan delete profile name=\"SGBono Internal\"";
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
                        await Delay(500);
                    }
                }
                catch
                { }
                await Delay(5000);
                Process process3 = new Process();
                process3.StartInfo.FileName = "cmd.exe";
                process3.StartInfo.Arguments = @"/c timeout /t 10 && rd ""C:\Windows\System32\oobe\Automation"" /s /q";
                process3.StartInfo.CreateNoWindow = true;
                process3.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process3.Start();
                await Delay(5000);
                defaultDevice.AudioEndpointVolume.Mute = false;
                await Delay(500);
                Process.GetCurrentProcess().Kill();
            }
        }

        private async Task Delay(int howlong)
        {
            await Task.Delay(howlong);
        }

        private IntPtr _windowHandle;
        IntPtr handle;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _windowHandle = new WindowInteropHelper(this).Handle;
            if (_windowHandle == null)
                throw new InvalidOperationException("The window has not yet been completely initialized");

            menuHandle = GetSystemMenu(_windowHandle, false);
            if (menuHandle != IntPtr.Zero)
            {
                EnableMenuItem(menuHandle, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
            }
        }

        private void MoveToOfflineInstall()
        {
            bool validDriverUSB = false;
            while (validDriverUSB == false)
            {
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
                        await Delay(500);
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
                                    await Delay(10000);
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
                                        //iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("An unknown error occurred while attempting to launch the driver install utility.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        DialogWindow dialogWindow = new DialogWindow($"An error occurred while attempting to launch the driver install utility. "+ex.Message+"\nTry again?", "Error", true);
                                        dialogWindow.ShowDialog();
                                        if (dialogWindow.Result == DialogMessageBoxResult.Skip)
                                        {
                                            var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to skip?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                            if (message == MessageBoxResult.Yes)
                                            {
                                                driversDone = true;
                                            }
                                            else
                                            {
                                                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                                MoveToOfflineInstall();
                                                ignoreError = true;
                                                throw new Exception("Moving to offline install!");
                                            }
                                        } else
                                        {
                                            MoveToOfflineInstall();
                                            ignoreError = true;
                                            throw new Exception("Moving to offline install!");
                                        }
                                    }
                                }
                                else if (c == 'Z')
                                {
                                    if (ignoreError == true)
                                    {
                                        break;
                                    }
                                    //iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Your installation USB does not come with drivers. Please find a driver installation USB and install the drivers manually.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    DialogWindow dialogWindow = new DialogWindow($"Your installation USB does not come with drivers.", "Error", true);
                                    dialogWindow.ShowDialog();
                                    if (dialogWindow.Result == DialogMessageBoxResult.TryAgain)
                                    {
                                        MoveToOfflineInstall();
                                        throw new Exception("Moving to offline install!");
                                    }
                                    else if (dialogWindow.Result == DialogMessageBoxResult.Skip)
                                    {
                                        var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to skip?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                        if (message == MessageBoxResult.Yes)
                                        {
                                            driversDone = true;
                                        }
                                        else
                                        {
                                            iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                            MoveToOfflineInstall();
                                            throw new Exception("Moving to offline install!");
                                        }
                                    }
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
                                    //iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Your installation USB does not come with drivers. Please find a driver installation USB and install the drivers manually.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    DialogWindow dialogWindow = new DialogWindow($"Your installation USB does not come with drivers.", "Error", true);
                                    dialogWindow.ShowDialog();
                                    if (dialogWindow.Result == DialogMessageBoxResult.TryAgain)
                                    {
                                        MoveToOfflineInstall();
                                        throw new Exception("Moving to offline install!");
                                    }
                                    else if (dialogWindow.Result == DialogMessageBoxResult.Skip)
                                    {
                                        var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to skip?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                        if (message == MessageBoxResult.Yes)
                                        {
                                            driversDone = true;
                                        }
                                        else
                                        {
                                            iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                            MoveToOfflineInstall();
                                            throw new Exception("Moving to offline install!");
                                        }
                                    }
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
                        //iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Your installation USB does not come with drivers. Please find a driver installation USB and install the drivers manually, or use Windows Update.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogWindow dialogWindow = new DialogWindow($"Your installation USB does not come with drivers.", "Error", true);
                        dialogWindow.ShowDialog();
                        if (dialogWindow.Result == DialogMessageBoxResult.TryAgain)
                        {
                            MoveToOfflineInstall();
                            throw new Exception("Moving to offline install!");
                        }
                        else if (dialogWindow.Result == DialogMessageBoxResult.Skip)
                        {
                            var message = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Are you sure you want to skip?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (message == MessageBoxResult.Yes)
                            {
                                driversDone = true;
                            }
                            else
                            {
                                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("We will now try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }

                    try
                    {
                        processingChangesLabel.Content = "Generating system report...";
                        await Delay(500);

                        string cpuName = "";
                        string gpuName = "";
                        string ramInfo = "";
                        string storageSize = "";
                        string batteryHealth = "";

                        //CPU
                        ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
                        foreach (ManagementObject mo in mos.Get())
                        {
                            cpuName = $"CPU: {(string)mo["Name"]}".Replace("(R)", "").Replace("(TM)", "");
                        }

                        //GPU
                        bool hasiGPU = false;
                        string iGPUName = "";
                        using (var searcher1 = new ManagementObjectSearcher("select * from Win32_VideoController"))
                        {
                            foreach (ManagementObject obj in searcher1.Get())
                            {
                                if (obj["Name"].ToString() == "Microsoft Basic Display Adapter")
                                {
                                    gpuName = "GPU: No GPU drivers installed";
                                }
                                //Improved iGPU detector - should work theoretically though this requires testing
                                else if (obj["Name"].ToString() == "AMD Radeon(TM) Graphics" || obj["Name"].ToString().Contains("Intel") && !obj["Name"].ToString().Contains("Intel Arc") && obj["Name"].ToString() != "Intel(R) Arc(TM) Graphics")
                                {
                                    hasiGPU = true;
                                    iGPUName = obj["Name"].ToString().Replace("(R)", "").Replace("(TM)", "");
                                }
                                else
                                {
                                    gpuName = "GPU: " + obj["Name"].ToString().Replace("(R)", "").Replace("(TM)", "");
                                    break;
                                }
                            }
                            if (gpuName == "" && hasiGPU == true)
                            {
                                gpuName = $"GPU: {iGPUName} (iGPU)";
                            }
                        }

                        //RAM
                        ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                        ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(objectQuery);

                        ManagementObjectSearcher searcher2 = new ManagementObjectSearcher("Select * from Win32_PhysicalMemory");
                        var ramspeed = "";
                        var newram = 0L;
                        var newMemoryType = "";
                        foreach (ManagementObject obj in searcher2.Get())
                        {
                            try
                            {
                                ramspeed = Convert.ToString(obj["ConfiguredClockSpeed"]);
                            }
                            catch { }
                        }
                        foreach (ManagementObject managementObject in managementObjectSearcher.Get())
                        {
                            long remainder = 0;
                            newram = Convert.ToInt64(managementObject["TotalVisibleMemorySize"]) / 1000 / 1000;
                            remainder = newram % 4;

                            if (remainder == 0)
                            {
                                newram = Convert.ToInt64(managementObject["TotalVisibleMemorySize"]) / 1000 / 1000;
                            }
                            else if (remainder < 2)
                            {
                                newram -= remainder;
                            }
                            else
                            {
                                newram += 4 - remainder;
                            }

                        }
                        foreach (ManagementObject managementObject in searcher2.Get())
                        {
                            string memoryType = managementObject["MemoryType"].ToString();
                            switch (memoryType)
                            {
                                case "20":
                                    newMemoryType = "DDR";
                                    break;
                                case "21":
                                    newMemoryType = "DDR2";
                                    break;
                                case "24":
                                    newMemoryType = "DDR3";
                                    break;
                                case "26":
                                    newMemoryType = "DDR4";
                                    break;
                                case "34":
                                    newMemoryType = "DDR5";
                                    break;
                                case "0":
                                    string memoryType2 = managementObject["SMBIOSMemoryType"]?.ToString() ?? "0";
                                    if (memoryType2 == "34")
                                    {
                                        newMemoryType = "DDR5";
                                    }
                                    else if (memoryType2 == "20")
                                    {
                                        newMemoryType = "DDR";
                                    }
                                    else if (memoryType2 == "21")
                                    {
                                        newMemoryType = "DDR2";
                                    }
                                    else if (memoryType2 == "24")
                                    {
                                        newMemoryType = "DDR3";
                                    }
                                    else if (memoryType2 == "26")
                                    {
                                        newMemoryType = "DDR4";
                                    }
                                    else
                                    {
                                        newMemoryType = "Unknown";
                                    }
                                    break;
                                default:
                                    newMemoryType = "Unknown";
                                    break;
                            }
                        }
                        if (ramspeed == null || ramspeed == "" || ramspeed == "0")
                        {
                            ramspeed = "Unknown ";
                        }
                        else if (newMemoryType == "Unknown")
                        {
                            //Last last resort RAM type check
                            //Banking on nobody being able to reach 4800 MT/s on DDR4 (DDR5 JEDEC = 4800 MT/s)
                            //Also not considering the LPDDR5/LPDDR5x users
                            if (Convert.ToInt32(ramspeed) >= 4800)
                            {
                                newMemoryType = "DDR5";
                            }
                        }

                        ramInfo = $"RAM: {newram} GB {newMemoryType}-{ramspeed}";

                        //Storage
                        DriveInfo mainDrive = new DriveInfo(System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)));
                        var totalsize = mainDrive.TotalSize / 1000 / 1000 / 1000;
                        if (totalsize >= 1000)
                        {
                            storageSize = $"Storage on Windows drive: {Math.Round((double)totalsize / 1000, 1)}TB";
                        }
                        else
                        {
                            storageSize = $"Storage on Windows drive: {totalsize}GB";
                        }
                        await Delay(200);

                        //Battery health
                        ManagementObjectSearcher batteryStaticData = new ManagementObjectSearcher("root/WMI", "SELECT * FROM BatteryStaticData");
                        ManagementObjectSearcher batteryFullChargedCapacity = new ManagementObjectSearcher("root/WMI", "SELECT * FROM BatteryFullChargedCapacity");

                        int designCapacity = 0;
                        int fullChargeCapacity = 0;

                        try
                        {
                            foreach (ManagementObject queryObj in batteryStaticData.Get())
                            {
                                designCapacity = Convert.ToInt32(queryObj["DesignedCapacity"]);
                            }

                            foreach (ManagementObject queryObj in batteryFullChargedCapacity.Get())
                            {
                                fullChargeCapacity = Convert.ToInt32(queryObj["FullChargedCapacity"]);
                            }
                        }
                        catch
                        {
                            batteryHealth = "Battery health: No battery detected";
                        }

                        if (designCapacity == 0 || fullChargeCapacity == 0)
                        {
                            batteryHealth = "Battery health: No battery detected";
                        }
                        else
                        {
                            double batteryHealthPercentage = Math.Round((double)fullChargeCapacity / designCapacity * 100, 1);
                            batteryHealth = "Battery health: " + batteryHealthPercentage + "%";
                        }

                        File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\System Report.txt", $"======System Report======\n\n<Note the following down>\n{cpuName}\n{gpuName}\n{ramInfo}\n{storageSize}\n{batteryHealth}\n\n<Additional information>\nOriginal battery capacity: {Math.Round((double)designCapacity / 1000)}Wh\nFull charge capacity: {Math.Round((double)fullChargeCapacity / 1000)}Wh");
                        Process.Start("notepad.exe", Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\System Report.txt").WaitForExit();
                    } catch
                    {
                        iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("There was an error generating the system report.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    try
                    {
                        processingChangesLabel.Content = "Starting Explorer...";
                        await Delay(500);
                        Process.Start("explorer.exe");
                        await Delay(5000);
                    }
                    catch
                    {
                        iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("There was an error launching Explorer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    try
                    {
                        processingChangesLabel.Content = "Cleaning Up...";
                        await Delay(500);
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
                        process2.StartInfo.Arguments = "wlan delete profile name=\"SGBono-Hotspot\"";
                        process2.StartInfo.RedirectStandardOutput = true;
                        process2.StartInfo.UseShellExecute = false;
                        process2.StartInfo.CreateNoWindow = true;
                        process2.Start();
                        process2.WaitForExit();
                        Directory.Delete(@"C:\SGBono", true);
                        while (Directory.Exists(@"C:\SGBono"))
                        {
                            await Delay(500);
                        }
                    }
                    catch
                    { }
                    await Delay(5000);
                    Process process3 = new Process();
                    process3.StartInfo.FileName = "cmd.exe";
                    process3.StartInfo.Arguments = @"/c timeout /t 10 && rd ""C:\Windows\System32\oobe\Automation"" /s /q";
                    process3.StartInfo.CreateNoWindow = true;
                    process3.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process3.Start();
                    await Delay(5000);
                    defaultDevice.AudioEndpointVolume.Mute = false;
                    Process.GetCurrentProcess().Kill();
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
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
