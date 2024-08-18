using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            List<string> fileList = SearchFoldersForFiles(_baseFolder, _dayCount, _currentDate);

            if (fileList.Count > 0)
            {
                sErrors = DeleteOldFiles(fileList);
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
                    //Not sure if GetLastWriteTime is going to work
                    DateTime fileCreatedDate = File.GetLastAccessTime(f);

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
                    File.Delete(f);
                }
                catch (Exception e)
                {
                    sErrors += "File: "+ f + " Error: " + e.Message + Environment.NewLine;
                }
            }
            return sErrors;
        }


    }
}
