using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace xPlatformLukma
{
    internal class EjectMediaMacOS
    {
        public static string EjectDisk(string diskId)
        {
            string Errors = "";
            //Get drives and cross check drives against what was passed

            List<UsbDrive> FullListDrives = ListUsbDrives();

            List<string> driveNames = new();
            foreach (UsbDrive drive in FullListDrives) 
            {
                Debug.WriteLine($"Debug: Media Drive: {drive.Identifier}");
                driveNames.Add(drive.Identifier);
            }
            if (!driveNames.Contains(diskId))
            {
                Errors = "Unmount failed! Trying to unmount non-Media drive: " + diskId;
                return Errors;
            }

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/sbin/diskutil",
                        Arguments = $"eject /dev/{diskId}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    Errors = error;
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
                                VolumeName = string.IsNullOrWhiteSpace(name) ? "(no name)" : name,
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
