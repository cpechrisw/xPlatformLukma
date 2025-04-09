using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace xPlatformLukma
{
    internal class FolderCleanup
    {
        private string _baseFolder;
        private int _dayCount;
        private DateTime _currentDate;
           
        public FolderCleanup(int days, string folderName) 
        { 
            SetDaysCount(days);
            SetCurrentDate(DateTime.Today);
            SetBaseFolder(folderName);

        }

        public void SetDaysCount(int days) { _dayCount = days; }

        public int GetDaysCount() { return _dayCount; }

        public void SetCurrentDate(DateTime date) { _currentDate = date; }

        public void SetBaseFolder(string folderPath) { _baseFolder = folderPath; }

        //-----Calls search then tries to delete the files in the list, returns any errors that are thrown
        //-----Returns an error list
        //-----
        public string CleanUpFolder() 
        {
            string sErrors = "";
            if (Directory.Exists(_baseFolder))
            {
                List<string> fileList = SearchFoldersForFiles(_baseFolder, _dayCount, _currentDate);
                
                if (fileList.Count > 0)
                {
                    sErrors = DeleteOldFiles(fileList);
                    sErrors += DeleteEmptyFolders(_baseFolder);
                }
            }
            else 
            {
                sErrors = "Files cannot be cleaned up. Folder doesn't exist: " + _baseFolder;
            }

            return sErrors;
        }


        private static List<string> SearchFoldersForFiles(string folderName, int days, DateTime currentDate)
        {
            List<string> aList = new();
            foreach (string f in Directory.EnumerateFiles(folderName, "*.*", SearchOption.AllDirectories))
            {
                string noCaseFileName = f.ToLowerInvariant();
                if (noCaseFileName.Contains("mp4"))
                {
                    //For debugging only
                    //DateTime fileCreatedDate = System.IO.File.GetLastWriteTime(f);
                    //Use GetLastAccessTime for actual program
                    DateTime fileCreatedDate = System.IO.File.GetLastAccessTime(f);

                    System.TimeSpan diff = currentDate - fileCreatedDate;
                    if (diff.Days > days)
                    {
                        aList.Add(f);
                    }
                }
            }
            return aList;
        }

        private static string DeleteOldFiles(List<string> files)
        {
            string sErrors = "";
            foreach (string f in files)
            {
                try
                {
                    System.IO.File.Delete(f);
                }
                catch (Exception e)
                {
                    sErrors += "Could not delete: "+ f + " Error: " + e.Message + Environment.NewLine;
                }
            }
            return sErrors;
        }
        
        private static string DeleteEmptyFolders(string startLocation)
        {
            string sErrors = "";
            if (!Directory.Exists(startLocation))
            {
                return sErrors;
            }
           
            var allDirs = Directory.GetDirectories(startLocation, "*", SearchOption.AllDirectories)
                       .OrderByDescending(d => d.Count(c => c == Path.DirectorySeparatorChar));

            foreach (string dir in allDirs)
            {
                try
                {
                    // Only delete if the directory is completely empty (no files or subdirs)
                    if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                    {
                        Directory.Delete(dir);
                    }
                }
                catch (Exception ex)
                {
                    sErrors += "Could not delete: " + dir + " Error: " + ex.Message + Environment.NewLine;
                }
            }
            return sErrors;
        }

    }
}
