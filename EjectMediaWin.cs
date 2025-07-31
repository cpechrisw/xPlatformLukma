using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace xPlatformLukma
{
    internal class EjectMediaWin
    {
        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint FILE_SHARE_READ = 0x00000001;
        const uint FILE_SHARE_WRITE = 0x00000002;
        const uint OPEN_EXISTING = 3;

        const uint FSCTL_LOCK_VOLUME = 0x00090018;
        const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;
        const uint IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        public string EjectDisk(string drivePath)
        {
            char driveLetter = drivePath[0];
            char systemDriveLetter = Path.GetPathRoot(Environment.SystemDirectory)[0];
            if (char.ToUpper(driveLetter) == char.ToUpper(systemDriveLetter))
            {
                string Errors = "Cannot eject system drive!";
                return Errors;
            }

            var removableDrives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
                .Where(d => char.ToUpper(d.Name[0]) != char.ToUpper(systemDriveLetter))
                .ToList();
            List<string> drives = new();
            foreach (var drive in removableDrives) 
            {
                drives.Add(drive.Name);
            }
            if (!drives.Contains(drivePath))
            {
                string Errors = "Drive " + driveLetter + " is not removable";
                return Errors;
            }
            

            string path = $"\\\\.\\{driveLetter}:";
            Debug.WriteLine($"Trying to eject: {path}");
            

            using (SafeFileHandle handle = CreateFile(path, GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero,
                OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {

                    string msg = "Failed to get handle to drive. Make sure you have admin rights.";
                    Debug.WriteLine(msg);
                    return msg;
                }

                
                // Lock volume
                if ( !TryLockVolume(handle) )
                {
                    string msg = "Drive is still in Use. Failed to lock volume. Close drive folders before trying to eject";
                    Debug.WriteLine(msg);
                    return msg;
                }

                // Dismount volume
                if (!DeviceIoControl(handle, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero))
                {
                    string msg = "Drive is still in Use. Failed to dismount volume. Close drive folders before trying to eject";
                    Debug.WriteLine(msg);
                    return msg;
                }

                // Eject media
                if (!DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero))
                {
                    string msg = "Failed to eject media.";
                    Debug.WriteLine(msg);
                    return msg;
                }

                Debug.WriteLine("Ejection successful!");
                return "";
            }

        }
        
        bool TryLockVolume(SafeFileHandle handle, int retries = 4)
        {
            for (int i = 0; i < retries; i++)
            {
                bool success = DeviceIoControl(handle, FSCTL_LOCK_VOLUME,
                    IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
                if (success)
                    return true;

                Thread.Sleep(500); // Wait and retry
            }
            return false;
        }
    }

}
