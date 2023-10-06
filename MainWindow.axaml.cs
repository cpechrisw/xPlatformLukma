using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using LibVLCSharp.Shared;     //pre 4.0 version
using System.IO;
using System.Linq;

namespace xPlatformLukma
{
    public partial class MainWindow : Window
    {
        //
        //-----Global Variables
        //
        private SettingsWindow _settingsWindow;
        private LogoWindow _logoWindow;
        private CategoriesWindow _categoriesWindow;

        //----used in ShowPercentComplete
        int percent;

        //----used in ConvertVideo

        Process ffmpeg;
        bool FlagConverting = false;
        List<VideoData> ListOfFiles = new List<VideoData>();
        VideoData BillvideoData;				//Does NOT need to be Global
        VideoData videoDataConverting;			//Doesn't seem like it needs to be Global

        //----VLC variables
        public LibVLC _libVLC;
        public MediaPlayer _mp;
        public Media _media;
        public bool vlc_isFullscreen = false;
        //public Size vlc_oldVideoSize;
        //public Size vlc_oldFormSize;
        //public Point vlc_oldVideoLocation;

        //----used in ReadConfig
        ConfigStruct configInfo;    //Structure to contain all config info
        Dictionary<string, string[]> categoriesDic;    //Variable for storing categories and sub categories

        //----Points and Timer variables
        //readonly CountdownTimer timer = new CountdownTimer();
        int counter = 0; //tracks number of points when using timer
        decimal Timeset = 35;


        //-----End of Global Variables

        //
        //-----Main function
        //
        public MainWindow()
        {
            LukmaStartup();
                        
        }

        //-----
        //-----Helper functions
        //-----

        public void LukmaStartup()
        {
            InitializeComponent();

            //Screen related intializations
            int screenWidth = Screens.Primary.WorkingArea.Width;
            int screenHeight = Screens.Primary.WorkingArea.Height;
            lbl_ScreenRes.Content = "Monitor Resolution: " + screenWidth.ToString() + "x" + screenHeight.ToString();

            //Form stuff
            //this.KeyPreview = true;
            //this.FormClosing += Form1_FormClosing;

            //VLC initialization
            Core.Initialize();
            //vlc_oldVideoSize = videoView.Size;
            //vlc_oldFormSize = this.Size;
            //vlc_oldVideoLocation = videoView.Location;
            _libVLC = new LibVLC("--input-repeat=2");   //"--verbose=2"
            _mp = new MediaPlayer(_libVLC);
            //_mp.EndReached += MediaEndReached;
            //_mp.EnableHardwareDecoding = true;          //Not sure if this is needed or helps
            //videoView.MediaPlayer = _mp;
            //
            configInfo = new ConfigStruct();
            categoriesDic = new Dictionary<string, string[]> { };
            ReadConfig();
            ReadCategoryFiles();
            Load_ComboBoxes();

        }
        //-----Reads the config files
        public void ReadConfig()  //How to read configuration from config file
        {

            string sConfigFile;
            //clear any values from these variables
            configInfo.appDir =
            configInfo.categoryFile =
            configInfo.divePoolFile =
            configInfo.logoDir =
            configInfo.convertedVideosTopDir =
            configInfo.unconvertedVideoDir =
            configInfo.generalCategoryDir =
            configInfo.TeamUploadTopFolder =
            configInfo.PrivateTeamUploadTopFolder =
            configInfo.ffmpegLocation =
            configInfo.workingDirectoryVar =
            "";

            //Setting defaults that should not change
            configInfo.appDir = AppContext.BaseDirectory.ToString();
            configInfo.configDir = Path.Combine(configInfo.appDir, "config");
            sConfigFile = Path.Combine(configInfo.configDir, "config.txt");
            configInfo.categoryFile = Path.Combine(configInfo.configDir, "Categories.txt");
            configInfo.divePoolFile = Path.Combine(configInfo.configDir, "Letters_And_Numbers.txt");
            configInfo.ffmpegLocation = Path.Combine(configInfo.appDir, "ffmpeg.exe");
            configInfo.logoDir = Path.Combine(configInfo.appDir, "logos");
            configInfo.customLogoFile = "customLogos.ini";
            string aboveAppDir = Path.Combine(configInfo.appDir, "..");

            //possible that these change
            configInfo.unconvertedVideoDir = Path.Combine(aboveAppDir, "videos");
            configInfo.convertedVideosTopDir = aboveAppDir;
            configInfo.customLogos = new Dictionary<string, string>();

            using (StreamReader sr = new StreamReader(sConfigFile))
            {
                while (!sr.EndOfStream)
                {
                    string sLine = sr.ReadLine().Trim();
                    string[] aLine = sLine.Split('=', (char)StringSplitOptions.RemoveEmptyEntries);
                    if (aLine.Count() == 2)
                    {
                        string sParameter = aLine[0];
                        string sValue = aLine[1].TrimEnd(Environment.NewLine.ToCharArray());
                        sValue = sValue.Replace("\"", "");
                        switch (sParameter)
                        {

                            case "catPathVar":
                                configInfo.categoryFile = Path.Combine(configInfo.configDir, sValue);
                                break;

                            case "NLPathVar":
                                configInfo.divePoolFile = Path.Combine(configInfo.configDir, sValue);
                                break;

                            case "localVideoDir":
                                configInfo.unconvertedVideoDir = sValue;
                                break;

                            case "convertedVideosTopDir":
                                configInfo.convertedVideosTopDir = sValue;
                                break;

                            case "workingDirectoryVar":
                                configInfo.workingDirectoryVar = sValue;
                                break;
                            default: break;

                        }

                    }
                    //
                    else
                    {
                        Console.WriteLine("Something is wrong in config file: " + sLine);
                    }

                }
            }
            UpdateConfigPaths();

            Console.WriteLine("done reading config file");
        }

        //-----Updates calculated paths
        //  general, team and private team uploads
        public void UpdateConfigPaths()
        {
            //These have the ability to be changed.
            configInfo.generalCategoryDir = Path.Combine(configInfo.convertedVideosTopDir, "uploadVideos");
            configInfo.TeamUploadTopFolder = Path.Combine(configInfo.convertedVideosTopDir, "teamuploadvideos");
            configInfo.PrivateTeamUploadTopFolder = Path.Combine(configInfo.convertedVideosTopDir, "privateteamuploadvideos");
        }


        //-----Reads the Category file
        private void ReadCategoryFiles()
        {
            try
            {
                string CatPath = configInfo.categoryFile;
                using (StreamReader sr = new StreamReader(CatPath))
                {
                    while (!sr.EndOfStream)
                    {

                        string sListItem = sr.ReadLine();
                        if (!String.IsNullOrEmpty(sListItem))
                        {
                            string[] tmpArray = { };
                            categoriesDic.Add(sListItem, tmpArray);
                        }
                    }
                }

                //For each category, reach the corresponding category file
                for (int i = 0; i < categoriesDic.Count; i++)
                {
                    string keyValue = categoriesDic.ElementAt(i).Key;
                    string filePath = Path.Combine(configInfo.configDir, keyValue + ".txt");
                    if (File.Exists(filePath))
                    {
                        using (StreamReader sr = new StreamReader(filePath))
                        {
                            List<string> arrString = new List<string>();
                            while (!sr.EndOfStream)
                            {

                                string sListItem = sr.ReadLine();
                                if (!String.IsNullOrEmpty(sListItem))
                                {
                                    arrString.Add(sListItem);
                                }
                            }
                            categoriesDic[keyValue] = arrString.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message.ToString()); }
        }


        //Helper function to load the combo boxes
        private void Load_ComboBoxes()
        {
            

            /*Load_first_ComboBox(categoriesDic);
            string[] divePool = ReadDivePoolFile(configInfo.divePoolFile);

            Load_NL_Boxes(divePool, NL1);
            Load_NL_Boxes(divePool, NL2);
            Load_NL_Boxes(divePool, NL3);
            Load_NL_Boxes(divePool, NL4);
            Load_NL_Boxes(divePool, NL5);
            Load_NL_Boxes(divePool, NL6);
            Load_NL_Boxes(divePool, NLexit);*/
        }



        //
        //---------Helper Events
        //
        private void ReloadComboBoxes(object sender, EventArgs e)
        {
            //Load_ComboBoxes();
        }

        //
        //---------Button Click Events
        //


        //
        //---------Menu Click Events
        //
        public void Menu_AboutClick()
        {
            AboutWindow aboutDialog = new();
            aboutDialog.Show();
        }

        public void Menu_CategoryNamesClick()
        {
            if(_categoriesWindow == null)
            {
                _categoriesWindow = new(configInfo, categoriesDic);
                _categoriesWindow.Closing += ReloadComboBoxes;
            }
            _categoriesWindow.Show();
        }

        public void Menu_SettingsClick()
        {
            _settingsWindow ??= new();
            _settingsWindow.Show();
        }
        public void Menu_LogoClick()
        {
            _logoWindow ??= new();
            _logoWindow.Show();
        }

    }

    //
    // ---------Data Classes and DataStructures
    //
    //---Timer Class
    //

    /*public class CountdownTimer : IDisposable  //Defines timer
    {
        public Action TimeChanged;
        public Action CountDownFinished;

        public bool IsRunning => pointsTimer.Enabled;

        public int StepMs
        {
            get => pointsTimer.Interval;
            set => pointsTimer.Interval = value;
        }
        
        //
        //-----------Need to change this
        //
        private readonly System.Windows.Forms.Timer pointsTimer = new System.Windows.Forms.Timer();

        private DateTime _maxTime = new DateTime(1, 1, 1, 0, 0, 50);
        private readonly DateTime _minTime = new DateTime(1, 1, 1, 0, 0, 0);

        public DateTime TimeLeft { get; private set; }
        private long TimeLeftMs => TimeLeft.Ticks / TimeSpan.TicksPerMillisecond;

        public string TimeLeftStr => TimeLeft.ToString("mm:ss");
        public string TimeLeftMsStr => TimeLeft.ToString("ss.f");
        private void TimerTick(object sender, EventArgs e)
        {
            if (TimeLeftMs > pointsTimer.Interval)
            {
                TimeLeft = TimeLeft.AddMilliseconds(-pointsTimer.Interval);
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
        public void Dispose() => pointsTimer.Dispose();
    }*/

    public struct ConfigStruct
    {
        public string appDir;                   //variable for application directory
        public string configDir;                //Variable for the config directory
        public string categoryFile;             //variable for path and filename for categories list
        public string divePoolFile;             //variable for path and filename for Numers and Letters list
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
    public struct VideoData
    {
        public string SourcePath;
        public string UploadPath;
        public string Resolution;
        public string SecondaryLogoPath;
        public DateTime FileCreated;
    }

}