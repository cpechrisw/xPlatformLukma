using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Shapes;
using Avalonia.Threading;

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
        public int bitRate;                     //variable for storing bitrate.
        public int cleanupAfterDays;            //How far back to look to delete old video files

        public Dictionary<string, string> customLogos;  //Used to house custom logos list

    }
    //Common structurs used by multiple classes
    public struct VideoData
    {
        public string SourcePath;
        public string UploadPath;
        public string Resolution;
        public string SecondaryLogoPath;
        public string PrimaryLogoPath;
        public DateTime FileCreated;
        public string ClipStartTime;
        public string ClipEndTime;
    }

    //
    //---Timer Class and other functions specific to Timer
    //
    //------ Timer and Points functions
    //
    //

    public class CountdownTimer
    {
        public Action TimeChanged;
        public Action CountDownFinished;

        public bool IsRunning => pointsTimer.IsEnabled;

        public double StepMs
        {
            get => pointsTimer.Interval.TotalMilliseconds;
            set => pointsTimer.Interval = TimeSpan.FromMilliseconds(value);
        }

        //
        //-----------Need to change this
        //
        private readonly DispatcherTimer pointsTimer = new();

        private DateTime _maxTime = new(1, 1, 1, 0, 0, 50);
        private readonly DateTime _minTime = new(1, 1, 1, 0, 0, 0);

        public DateTime TimeLeft { get; private set; }
        private long TimeLeftMs => TimeLeft.Ticks / TimeSpan.TicksPerMillisecond;

        public string TimeLeftStr => TimeLeft.ToString("mm:ss");
        public string TimeLeftMsStr => TimeLeft.ToString("ss.f");
        private void TimerTick(object sender, EventArgs e)
        {
            if (TimeLeftMs > pointsTimer.Interval.TotalMilliseconds)
            {
                TimeLeft = TimeLeft.AddMilliseconds(-pointsTimer.Interval.TotalMilliseconds);
                TimeChanged?.Invoke();
            }
            else
            {
                Stop();
                TimeLeft = _minTime;

                TimeChanged?.Invoke();
                CountDownFinished?.Invoke();
            }
        }
        public CountdownTimer(int min, int sec)
        {
            SetTime(min, sec);
            Init();
        }
        public CountdownTimer(DateTime dt)
        {
            SetTime(dt);
            Init();
        }
        public CountdownTimer()
        {
            Init();
        }
        private void Init()
        {
            TimeLeft = _maxTime;

            StepMs = 1000;
            pointsTimer.Tick += new EventHandler(TimerTick);
        }
        public void SetTime(DateTime dt)
        {
            TimeLeft = _maxTime = dt;
            TimeChanged?.Invoke();
        }
        public void SetTime(int min, int sec) => SetTime(new DateTime(1, 1, 1, 0, min, sec));
        public void Start() => pointsTimer.Start();
        public void Pause() => pointsTimer.Stop();
        public void Stop()
        {
            Pause();
            Reset();
        }
        public void Reset()
        {
            TimeLeft = _maxTime;
        }
        public void Restart()
        {
            Reset();
            Start();
        }

    }


    //Class of common functions that are used by multiple classes or dialogs
    internal class Utils
    {
        //Replaces and old line for a new line in the config file
        public void UpdateConfigFile(ConfigStruct aConfigInfo, string oldline, string updatedLine)
        {
            string CatPath = System.IO.Path.Combine(aConfigInfo.configDir, "config.txt");
            bool found = false;
            if (File.Exists(CatPath))
            {
                string[] lines = File.ReadLines(CatPath).ToArray();
                List<string> lineList = new();
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(oldline))
                    {
                        found = true;
                        string newLine = oldline + "=" + updatedLine;
                        lines[i] = newLine;
                    }
                    lineList.Add(lines[i]);
                }
                
                //if the line wasn't found, add it to the config file
                if (!found)
                {
                    string addLine = oldline + "=" + updatedLine;
                    lineList.Add(addLine);
                }

                //rewrite file
                File.WriteAllText(CatPath, string.Join(Environment.NewLine, lineList ));
            }
            else
            {
                Console.WriteLine("Couldn't find config file\n");
            }

        }

        public void CopyConfigsForMac(string MacDirectory, ConfigStruct aConfigInfo)
        {
            //string CatPath = System.IO.Path.Combine(aConfigInfo.configDir, "config.txt");
            string configDir = System.IO.Path.Combine(aConfigInfo.appDir, "config");
            string macConfigDir = System.IO.Path.Combine(MacDirectory, "config");
            if (!Directory.Exists(MacDirectory))
            {
                //create directory
                Directory.CreateDirectory(MacDirectory);
                
                //Copy config and logo directories
                
                string logoDir = System.IO.Path.Combine(aConfigInfo.appDir, "logos");
                
                string macLogoDir = System.IO.Path.Combine(MacDirectory, "logos");

                CopyAllFiles(configDir, macConfigDir);
                CopyAllFiles(logoDir, macLogoDir);
            }
            //Specifically check and copy the license file
            string lic = "license.ini";
            if (!File.Exists(System.IO.Path.Combine(macConfigDir, lic)))
            {
                FileInfo aFile = new(System.IO.Path.Combine(configDir, lic));
                aFile.CopyTo(System.IO.Path.Combine(macConfigDir, lic));
            }


        }
        
        public void CopyAllFiles(string baseDirectory, string destinationDir)
        {
            var dir = new DirectoryInfo(baseDirectory);
            
            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = System.IO.Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
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
                                returnError = " Error, logo for " + sParameter + " is missing";

                            if (!myConfigInfo.customLogos.ContainsKey(sParameter) && File.Exists(sValue))
                            {
                                myConfigInfo.customLogos.Add(sParameter, sValue);
                            }
                        }
                    }
                }
            }
            else
            {
                //This is also the case if the file does not exist or is corrupted
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
