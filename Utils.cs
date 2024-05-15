using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace xPlatformLukma
{
    
    //Common structurs used by multiple classes
    public struct ConfigStruct
    {
        public string appDir;                   //variable for application directory
        public string configDir;                //Variable for the config directory
        public string categoryFile;             //variable for path and filename for categories list
        public string logoDir;                  //variable where logos are located
        public string customLogoFile;           //Variable for the custom logo file
        public string unconvertedVideoDir;      //variable for local unconverted video directory
        public string convertedVideosTopDir;    //variable for top level converted video folders directory
        public string generalCategoryDir;       //variable for converted file path, uses convertedVideosTopDir
        public string TeamUploadTopFolder;      //variable for converted file path for teamsuses convertedVideosTopDir
        public string PrivateTeamUploadTopFolder;      //variable for converted file path for teams, uses convertedVideosTopDir
        public string ffmpegLocation;           //variable for ffmpeg.exe location
        public string workingDirectoryVar;      //variable for working directory used by ffmpeg

        public Dictionary<string, string> customLogos;  //Used to house custom logos list

    }
    //Common structurs used by multiple classes
    public struct VideoData
    {
        public string SourcePath;
        public string UploadPath;
        public string Resolution;
        public string SecondaryLogoPath;
        public DateTime FileCreated;
    }

    //Class of common functions that are used by multiple classes or dialogs
    internal class Utils
    {
        //Replaces and old line for a new line in the config file
        public void UpdateConfigFile(ConfigStruct aConfigInfo, string oldline, string updatedLine)
        {
            string CatPath = Path.Combine(aConfigInfo.configDir, "config.txt");
            if (File.Exists(CatPath))
            {
                string[] lines = File.ReadLines(CatPath).ToArray();
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(oldline))
                    {
                        string newLine = oldline + "=" + updatedLine;
                        lines[i] = newLine;
                    }
                }

                //rewrite file
                File.WriteAllText(CatPath, string.Join(Environment.NewLine, lines));
            }
            else
            {
                Console.WriteLine("Couldn't find config file\n");
            }

        }
        
        //Reads the custom logos file and updates local structure
        public string ReadCustomLogos(ConfigStruct myConfigInfo)
        {

            string sCustomLogoConfig = System.IO.Path.Combine(myConfigInfo.configDir, myConfigInfo.customLogoFile);
            string returnError = "";
            string basePriimaryLogo = System.IO.Path.Combine(myConfigInfo.logoDir, "SDCLogo_1080.png");

            if (File.Exists(sCustomLogoConfig))
            {
                using (StreamReader sr = new(sCustomLogoConfig))
                {
                    while (!sr.EndOfStream)
                    {
                        string sLine = sr.ReadLine().Trim();
                        string[] aLine = sLine.Split('=', (char)StringSplitOptions.RemoveEmptyEntries);


                        if (aLine.Length == 2)
                        {
                            string sParameter = aLine[0];
                            string sValue = aLine[1].TrimEnd(Environment.NewLine.ToCharArray());
                            //May also have to replace slashes here
                            if (!File.Exists(sValue))
                                returnError = "Error, logo for " + sParameter + " is missing";

                            if (!myConfigInfo.customLogos.ContainsKey(sParameter) && File.Exists(sValue))
                            {
                                myConfigInfo.customLogos.Add(sParameter, sValue);
                            }
                        }
                    }
                }
            }
            
            //This is also the case if the file does not exist or is corrupted
            if (!myConfigInfo.customLogos.ContainsKey("Primary"))
            {
                myConfigInfo.customLogos.Add("Primary", basePriimaryLogo);
                ReWriteCustomLogosFile(myConfigInfo);
            }

            return returnError;
        }
        
        //Fully rewrites the custom log file
        public void ReWriteCustomLogosFile(ConfigStruct myConfigInfo)
        {
            string filePath = System.IO.Path.Combine(myConfigInfo.configDir, myConfigInfo.customLogoFile);
            string[] sArray = new string[myConfigInfo.customLogos.Count];
            for (int i = 0; i < sArray.Length; i++)
            {
                sArray[i] = myConfigInfo.customLogos.Keys.ElementAt(i) + "=" +
                    myConfigInfo.customLogos[myConfigInfo.customLogos.Keys.ElementAt(i)];
            }
            File.WriteAllText(filePath, string.Join(Environment.NewLine, sArray));

        }

        //Adds a new line to the custom logo file
        public void AppendCustomLogosFile(ConfigStruct myConfigInfo, string category, string logoLocation)
        {
            string filePath = System.IO.Path.Combine(myConfigInfo.configDir, myConfigInfo.customLogoFile);
            string addedString = category + "=" + logoLocation;
            if (!File.Exists(filePath)) File.Create(filePath).Close();

            if (new FileInfo(filePath).Length != 0)
            {
                File.AppendAllText(filePath, Environment.NewLine);
            }
            File.AppendAllText(filePath, addedString);

        }




    }
}
