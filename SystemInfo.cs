using NAudio.CoreAudioApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace beforewindeploy
{
    static class SystemInfo
    {
        private static Dictionary<string, string> Specifications;

        public static Dictionary<string, string> Get()
        {
            if (Specifications != null) return Specifications;

            string cpuName = "";
            string gpuName = "";
            string ramInfo = "";
            string storageSize = "";
            string batteryHealth = "";

            //CPU
            ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
            foreach (ManagementObject mo in mos.Get())
            {
                cpuName = $"{(string)mo["Name"]}".Replace("(R)", "").Replace("(TM)", "").Trim();
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
                        gpuName = "No GPU drivers installed";
                    }
                    //Improved iGPU detector - should work theoretically though this requires testing
                    else if (obj["Name"].ToString() == "AMD Radeon(TM) Graphics" || obj["Name"].ToString().Contains("Intel") && !obj["Name"].ToString().Contains("Intel Arc") && obj["Name"].ToString() != "Intel(R) Arc(TM) Graphics")
                    {
                        hasiGPU = true;
                        iGPUName = obj["Name"].ToString().Replace("(R)", "").Replace("(TM)", "");
                    }
                    else
                    {
                        gpuName = "" + obj["Name"].ToString().Replace("(R)", "").Replace("(TM)", "");
                        break;
                    }
                }
                if (gpuName == "" && hasiGPU == true)
                {
                    gpuName = $"{iGPUName} (iGPU)";
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
                storageSize = $"{Math.Round((double)totalsize / 1000, 1)}TB";
            }
            else
            {
                storageSize = $"{totalsize}GB";
            }
            Task.Delay(200);

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
                batteryHealth = "No battery detected";
            }

            if (designCapacity == 0 || fullChargeCapacity == 0)
            {
                batteryHealth = "No battery detected";
            }
            else
            {
                double batteryHealthPercentage = Math.Round((double)fullChargeCapacity / designCapacity * 100, 1);
                batteryHealth = "" + batteryHealthPercentage + "%";
            }

            Specifications = new Dictionary<string, string>()
            {
                ["CPU"] = cpuName,
                ["GPU"] = gpuName,
                ["RAM"] = ramInfo,
                ["Storage"] = storageSize,
                ["Battery Health"] = batteryHealth,
                ["Design Capacity"] = designCapacity.ToString(),
                ["Full Charge Capacity"] = fullChargeCapacity.ToString(),
            };

            return Specifications;
        }
    }
}
