using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace xPlatformLukma
{
    internal class EjectMediaMacOS
    {
        public string EjectDisk(string diskMountPoint)
        {
            string diskId="";
            string Errors = "";
            //Get drives and cross check drives against what was passed

            List<UsbDrive> FullListDrives = ListUsbDrives();

            foreach (UsbDrive drive in FullListDrives) 
            {
                Debug.WriteLine($"Debug: Media Drive: {drive.Identifier}");
                if( drive.MountPoint == diskMountPoint )
                {
                    diskId = drive.Identifier;

                }
                
            }
            if (diskId == "")
            {
                Errors = "Unmount failed! Trying to unmount non-Media drive: " + diskId + " " + diskMountPoint;
                return Errors;
            }

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "diskutil",
                        Arguments = $"eject /dev/{diskId}",
                        //FileName = "/bin/bash",
                        //Arguments = $"-c \"diskutil eject /dev/{diskId}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    Errors = error + " " + output;
                }

                return Errors;
            }
            catch
            {
                return Errors;
            }
        }

        class UsbDrive
        {
            public string Identifier { get; set; } = "";
            public string VolumeName { get; set; } = "";
            public string MountPoint { get; set; } = "";
        }

        private static List<UsbDrive> ListUsbDrives()
        {
            string listJson = GetDiskListJson();
            if (string.IsNullOrWhiteSpace(listJson))
                return new List<UsbDrive>();

            var usbDrives = new List<UsbDrive>();
            var root = JsonNode.Parse(listJson);
            var allDisks = root?["AllDisksAndPartitions"]?.AsArray();
            if (allDisks == null)
                return usbDrives;

            foreach (var disk in allDisks)
            {
                string content = disk?["Content"]?.ToString() ?? "";
                string deviceIdentifier = disk?["DeviceIdentifier"]?.ToString() ?? "";
                string internalFlag = disk?["Internal"]?.ToString() ?? "";
                var partitions = disk?["Partitions"]?.AsArray();


                // Skip internal system disks
                if (internalFlag == "true") continue;

                //This only happens if the disk is directly mounted (some USBs)
                string diskMountPoint = disk?["MountPoint"]?.ToString() ?? "";
                string diskVolumeName = disk?["VolumeName"]?.ToString() ?? "";

                if (!string.IsNullOrWhiteSpace(diskMountPoint) )
                {
                    usbDrives.Add(new UsbDrive
                    {
                        Identifier = deviceIdentifier,
                        VolumeName = string.IsNullOrWhiteSpace(diskVolumeName) ? "(NO NAME)" : diskVolumeName,
                        MountPoint = diskMountPoint
                    });
                    continue;
                }


                if (partitions != null)
                {
                    foreach (var part in partitions)
                    {
                        string mp = part?["MountPoint"]?.ToString() ?? "";
                        string name = part?["VolumeName"]?.ToString() ?? "";
                        string id = part?["DeviceIdentifier"]?.ToString() ?? "";

                        if (!string.IsNullOrWhiteSpace(mp) && !string.IsNullOrWhiteSpace(id))
                        {
                            usbDrives.Add(new UsbDrive
                            {
                                Identifier = deviceIdentifier,
                                VolumeName = string.IsNullOrWhiteSpace(name) ? "(NO NAME)" : name,
                                MountPoint = mp
                            });
                            break; // Only need the first mounted partition
                        }
                    }
                }
            }

            return usbDrives;
        }

        private static string GetDiskListJson()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = "-c \"diskutil list -plist | plutil -convert json -o - -\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output;
            }
            catch
            {
                return "";
            }
        }

    }
}
