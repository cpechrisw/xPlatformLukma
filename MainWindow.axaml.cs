using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using System.Runtime.InteropServices;
using LibVLCSharp.Avalonia;
using System.Text;
using System.Collections.Concurrent;


namespace xPlatformLukma
{
    public partial class MainWindow : Window
    {
        //
        //-----Global Variables
        //
        private static Mutex mutex = null;
        private SettingsWindow _settingsWindow;
        private LogoWindow _logoWindow;
        private CategoriesWindow _categoriesWindow;
        private bool winPlatform;               //1 if windows, 0 if not windows
        private string myErrorsOnLoad = "";      //specifically for load to show errors
        //private Utils newUtil;                  //used for common functions
        

        private readonly bool ENABLEVIDEOCUT = true; //Global varialble to enable/disable chopping of the video

        //----used in ShowPercentComplete when files are being converted
        private DispatcherTimer completionTimer;
        int percent;

        //----used in ConvertVideo

        //Process ffmpeg;
        bool FlagConverting = false;
        ConcurrentQueue<VideoData> ListOfFiles = new();
        //VideoData videoDataConverting;			//Doesn't seem like it needs to be Global
        TimeSpan[] gVideoClip;
        Avalonia.Collections.AvaloniaList<double> gSliderTickList;



        //----Video VLC variables
        public LibVLC _libVLC;
        public MediaPlayer _mp;
        public Media _media;
        private string initVideoDirectory;
        private string currentVideoPath;
        //private VideoView _videoViewer;
        
        //----used in ReadConfig
        ConfigStruct configInfo;    //Structure to contain all config info
        SortedDictionary<string, string[]> categoriesDic;    //Variable for storing categories and sub categories

        //----Points and Timer variables
        readonly CountdownTimer timer = new();
        int counter = 0;    //Keeps track of the points
        int timeset = 35;   //Keeps track of the initial state of timer


        //-----End of Global Variables

        //
        //-----Main function
        //
        public MainWindow()
        {
            LukmaStartup();
                        
        }

        //-----Verifying only 1 instance is running
        protected override void OnOpened(EventArgs e)
        {
            const string appName = "xPlatformLukma";
            mutex = new Mutex(true, appName, out bool createdNew);

            if (!createdNew)
            {
                //app is already running! Exiting the application
                if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
                {
                    string msg = "One instance of application is already open. Closing new instance";
                    //Debug.Write(msg);
                    ShowErrorMessageAndClose(msg);
                    //lifetime.Shutdown();
                }
            }


            base.OnOpened(e);
        }
        
        //---------
        //--------- functions
        //---------

        public void LukmaStartup()
        {
            InitializeComponent();
            winPlatform = IsPlatformWindows();
            this.Closing += AppClosingTest;
            gSliderTickList = new(0, 0);
            gVideoClip = new TimeSpan[] { TimeSpan.Zero, TimeSpan.Zero };

            //Screen related intializations
            int screenWidth = Screens.Primary.WorkingArea.Width;
            int screenHeight = Screens.Primary.WorkingArea.Height;
            lbl_ScreenRes.Content = "Monitor Resolution: " + screenWidth.ToString() + "x" + screenHeight.ToString();

            
            //VLC initialization and View controls
            Core.Initialize();
            _libVLC = new LibVLC("--input-repeat=2");   //"--verbose=2"
            _mp = new MediaPlayer(_libVLC);
            //_videoViewer = this.Get<VideoView>("VideoViewer");
            //_mp.EndReached += MediaEndReached;
            VideoViewer.MediaPlayer = _mp;


            //Structure Intializations
            //newUtil = new Utils();
            configInfo = new ConfigStruct();
            categoriesDic = new SortedDictionary<string, string[]> { };
            ReadConfig();
            ReadCategoryFiles();
            myErrorsOnLoad += "" + Utils.ReadCustomLogos(configInfo);
            Load_ComboBoxes();
            InitializeButtonEventsLabels();
            this.Opened += CheckLicense;
            //Check for cleanup
            RunCleanup();
            
        }
        
        public static bool IsPlatformWindows()
        {
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        //-----Reads the config files
        public void ReadConfig()  //How to read configuration from config file
        {

            string sConfigFile;
            //clear any values from these variables
            configInfo.appDir =
            configInfo.categoryFile =
            configInfo.logoDir =
            configInfo.convertedVideosTopDir =
            configInfo.unconvertedVideoDir =
            configInfo.generalCategoryDir =
            configInfo.TeamUploadTopFolder =
            configInfo.PrivateTeamUploadTopFolder =
            configInfo.ffmpegLocation =
            configInfo.workingDirectoryVar =
            "";
            configInfo.bitRate = -1;
            configInfo.cleanupAfterDays = -1;

            //Setting defaults that should not change
            SetDefaultConfigPaths();
            
            sConfigFile = Path.Combine(configInfo.configDir, "config.txt");
            bool errorWithConfigFile = false;

            if (File.Exists(sConfigFile))
            {
                List<string[]> updateList = new();
                using (StreamReader sr = new(sConfigFile))
                {
                    while (!sr.EndOfStream)
                    {
                        string sLine = sr.ReadLine().Trim();
                        string[] aLine = sLine.Split('=', (char)StringSplitOptions.RemoveEmptyEntries);
                        if (aLine.Length == 2)
                        {
                            string sParameter = aLine[0].Trim();
                            string sValue = aLine[1].TrimEnd(Environment.NewLine.ToCharArray()).Trim();
                            sValue = sValue.Replace("\"", "");
                            
                            switch (sParameter)
                            {
                                case "bitrate":
                                    int tmpInt = 0;
                                    try
                                    {
                                        tmpInt = Int32.Parse(sValue);
                                    }
                                    catch(FormatException)
                                    {
                                        string[] tmpStringArray = { "bitrate", tmpInt.ToString() };
                                        updateList.Add(tmpStringArray);
                                        errorWithConfigFile = true;
                                    }
                                    configInfo.bitRate = tmpInt;
                                    break; 
                                
                                case "cleanupAfterDays":
                                    int tmpDays = 0;
                                    try
                                    {
                                        tmpDays = Int32.Parse(sValue);
                                    }
                                    catch (FormatException)
                                    {
                                        string[] tmpStringArray = { "cleanupAfterDays", tmpDays.ToString() };
                                        updateList.Add(tmpStringArray);
                                        errorWithConfigFile = true;
                                    }
                                    configInfo.cleanupAfterDays = tmpDays;
                                    break;
                                case "catPathVar":
                                    if (File.Exists(Path.Combine(configInfo.configDir, sValue)))
                                    {
                                        configInfo.categoryFile = Path.Combine(configInfo.configDir, sValue);
                                    }
                                    else
                                    {
                                        //update this to rewrite this after it is done reading
                                        string[] tmpStringArray = { "catPathVar", configInfo.categoryFile };
                                        updateList.Add(tmpStringArray);
                                        //newUtil.UpdateConfigFile(configInfo, "catPathVar", configInfo.categoryFile);
                                        errorWithConfigFile = true;
                                    }
                                    break;

                                case "localVideoDir":
                                    
                                    string tmpVar = Path.GetPathRoot(sValue);
                                    if ( Directory.Exists(tmpVar) )
                                    {
                                        configInfo.unconvertedVideoDir = sValue;
                                    }
                                    else
                                    {
                                        //update this to rewrite this after it is done reading
                                        string[] tmpStringArray = { "localVideoDir", configInfo.unconvertedVideoDir };
                                        updateList.Add(tmpStringArray);
                                        //newUtil.UpdateConfigFile(configInfo, "localVideoDir", configInfo.unconvertedVideoDir);
                                        errorWithConfigFile = true;
                                    }

                                    break;

                                case "convertedVideosTopDir":
                                    string tmpString = Path.GetPathRoot(sValue);
                                    if (Directory.Exists(tmpString))
                                    {
                                        configInfo.convertedVideosTopDir = sValue;
                                    }
                                    else
                                    {
                                        //update this to rewrite this after it is done reading
                                        string[] tmpStringArray = { "convertedVideosTopDir", configInfo.convertedVideosTopDir };
                                        updateList.Add(tmpStringArray);
                                        //newUtil.UpdateConfigFile(configInfo, "convertedVideosTopDir", configInfo.convertedVideosTopDir);
                                        errorWithConfigFile = true;
                                    }
                                    break;

                                case "workingDirectoryVar":
                                    if (Directory.Exists(sValue))
                                    {
                                        configInfo.workingDirectoryVar = sValue;
                                    }
                                    break;
                                default: break;

                            }
                        }
                        //
                        else
                        {
                            Debug.WriteLine("Something is wrong in config file: " + sLine);
                            Console.WriteLine("Something is wrong in config file: " + sLine);
                        }
                        if (errorWithConfigFile)
                        {
                            myErrorsOnLoad = "Errors found with settings file. Some settings were reverted back to defaults";
                        }

                    }
                }
                if (updateList.Count > 0)
                {
                    foreach (string[] tmpString in updateList)
                    {
                        Utils.UpdateConfigFile(configInfo, tmpString[0], tmpString[1]);
                    }
                }

            }
            else        //File doesn't exist, write it out
            {
                myErrorsOnLoad += "Error: Settings file doesn't exist. Close and reopen Lukma";
                Utils.UpdateConfigFile(configInfo, "localVideoDir", configInfo.unconvertedVideoDir);
                Utils.UpdateConfigFile(configInfo, "convertedVideosTopDir", configInfo.convertedVideosTopDir);
                Utils.UpdateConfigFile(configInfo, "bitrate", configInfo.bitRate.ToString());
                Utils.UpdateConfigFile(configInfo, "cleanupAfterDays", configInfo.cleanupAfterDays.ToString());

            }

            UpdateConfigPaths();

            //-------
            //Checking new config info variables and write them out if
            //-------
            if (configInfo.bitRate < 0)
            {
                configInfo.bitRate = 0;
                Utils.UpdateConfigFile(configInfo, "bitrate", configInfo.bitRate.ToString());
            }
            if (configInfo.cleanupAfterDays < 0)
            {
                configInfo.cleanupAfterDays = 0;
                Utils.UpdateConfigFile(configInfo, "cleanupAfterDays", configInfo.bitRate.ToString());
            }


            //Debug.WriteLine("done reading config file");
        }

        //Sets a few defautls
        //   sets different defaults based on windows or Mac
        public void SetDefaultConfigPaths()
        {
            configInfo.appDir = AppContext.BaseDirectory.ToString();
            string baseConfigAndLogoDir = configInfo.appDir;
            if (!winPlatform)
            {
                //I really want to store everything in /Users/Shared/Lukma
                //Environment.SpecialFolder.CommonDocuments
                baseConfigAndLogoDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Public","Lukma");

                Utils.CopyConfigsForMac(baseConfigAndLogoDir, configInfo);
            }
            
            configInfo.configDir = Path.Combine(baseConfigAndLogoDir, "config");
            configInfo.categoryFile = Path.Combine(configInfo.configDir, "Categories.txt");
            configInfo.ffmpegLocation = Path.Combine(configInfo.appDir, "Assets", "ffmpeg.exe");
            configInfo.logoDir = Path.Combine(baseConfigAndLogoDir, "logos");
            configInfo.customLogoFile = "customLogos.ini";

            string aboveAppDir = Directory.GetParent(baseConfigAndLogoDir).Parent.FullName;
                //Path.Combine(baseConfigAndLogoDir, "..");
            //Conditional for Mac
            if (!winPlatform)
            {
                string tmpPath = Path.Combine(configInfo.appDir, "Assets", "ffmpeg");
                if (File.Exists(tmpPath))
                {
                    configInfo.ffmpegLocation = tmpPath;
                }
                else
                {
                    tmpPath = Path.Combine(configInfo.appDir, "../Resources/Assets", "ffmpeg");
                    if (File.Exists(tmpPath))
                    {
                        configInfo.ffmpegLocation = tmpPath;
                    }
                    else
                    {
                        myErrorsOnLoad += " Error: ffmpeg could NOT be found. Install is corrupt!";
                    }

                }
                
                aboveAppDir = baseConfigAndLogoDir;
            }

            //possible that these change
            configInfo.unconvertedVideoDir = Path.Combine(aboveAppDir, "videos");
            configInfo.convertedVideosTopDir = aboveAppDir;
            configInfo.customLogos = new Dictionary<string, string>();
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
                using (StreamReader sr = new(CatPath))
                {
            
                    while (!sr.EndOfStream)
                    {
                        string sListItem = sr.ReadLine();
                        if (!String.IsNullOrEmpty(sListItem))
                        {
                            string[] tmpArray = Array.Empty<string>();
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
                        using StreamReader sr = new(filePath);
                        List<string> arrString = new();
                        while (!sr.EndOfStream)
                        {

                            string sListItem = sr.ReadLine();
                            if (!String.IsNullOrEmpty(sListItem))
                            {
                                arrString.Add(sListItem);
                            }
                        }
                        //------sort the Array
                        arrString.Sort();
                        categoriesDic[keyValue] = arrString.ToArray();
                    }
                }
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message.ToString());
                Console.WriteLine(ex.Message.ToString()); }
        }


        //Helper function to load the combo boxes
        private void Load_ComboBoxes()
        {
            
            Load_Category_ComboBox(categoriesDic);
            Load_VideoQuality_comboBox();
            
        }

        private void Load_Category_ComboBox(SortedDictionary<string, string[]> myDictionary)                                   //Populate categories ComboBox
        {
            combo_CategoryComboBox.Items.Clear();
            for (int i = 0; i < myDictionary.Count; i++)
            {
                combo_CategoryComboBox.Items.Add(myDictionary.ElementAt(i).Key);

            }
        }

        private void Load_VideoQuality_comboBox()
        {
            //combo_VideoQuality
            combo_VideoQuality.Items.Clear();
            combo_VideoQuality.Items.Add("1080");
            combo_VideoQuality.Items.Add("720");
            combo_VideoQuality.SelectedIndex = 0;
        }

        private void PlayVideo(string file)
        {
            try
            {
                MediaPlayerPlayVideo(file, 0);
                //Dispatcher.UIThread.Post(() => UpdateVideoButtons(true), DispatcherPriority.Normal);      //not sure this is needed

            }
            catch (Exception ex)
            {
                Debug.Write("During play video " + ex.Message);
                Console.Write("During play video " + ex.Message);
            }
            
        }

        private void MediaPlayerPlayVideo(string filename, long videoTime)
        {

            if (File.Exists(filename))
            {
               
                MediaPlayerStopVideo();
                _media = new Media(_libVLC, filename);

                //Video slider initialization
                _mp.Play(_media);
                
                _mp.TimeChanged += MP_TimeChanged;
                _mp.Time = videoTime;
                slider_VideoSlider.ValueChanged += SL_TimeChanged;
                _mp.Mute = true;
                _media?.Dispose();
                Dispatcher.UIThread.Post(() => UpdateVideoButtons(true), DispatcherPriority.Normal);
                Dispatcher.UIThread.Post(() => UpdateClipLabels(), DispatcherPriority.Normal);
                
            }
            else
            {
                Debug.WriteLine("File not found: " + filename);
                Console.WriteLine("File not found: " + filename);
            }
        }

        private void MediaPlayerStopVideo()
        {
            if (_mp.IsPlaying)
            {
                //_mp.Stop();         //Causing a deadlock
                _mp.Pause();
                _mp.TimeChanged -= MP_TimeChanged;
                slider_VideoSlider.ValueChanged -= SL_TimeChanged;
                
                //_mp?.Dispose();       //This is causing the popout
                //_mp = new MediaPlayer(_libVLC);
                //VideoViewer.MediaPlayer = _mp;
                Dispatcher.UIThread.Post(() => UpdateVideoButtons(false), DispatcherPriority.Normal);
            }
            
        }

        private void InitializeButtonEventsLabels()
        {
            btn_Search.Click += SearchButton_Click;
            combo_CategoryComboBox.IsEnabled = false;
            combo_CategoryComboBox.SelectionChanged += CategoriesBox_SelectedIndexChanged;
            combo_CatSubName.IsEnabled = false;
            combo_CatSubName.SelectionChanged += NamesComboBox_SelectedIndexChanged;
            txtb_Description.IsEnabled = false;

            slider_VideoSlider.IsEnabled = false;
            UpdateVideoButtons(false);

            txtb_Description.TextChanged += DescriptionTextBox_TextChanged;
            btn_Clear.Click += ClearButton_Click;
            btn_Save.Click += SaveButton_Click;

            btn_startTimer.Click += StartTimerButton_Click;
            btn_addPoint.Click += PointButton_Click;
            btn_stopTimer.Click += StopTimerButton_Click;
            btn_resetTimer.Click += ResetTimerButton_Click;


            btn_VideoPlay.Click += VideoPlayPause_Click;
            btn_VideoRewind.Click += VideoRwd_Click;
            btn_VideoFF.Click += VideoFwd_Click;
            btn_VideoRestart.Click += VideoRestart_Click;

            cnt_TimerValue.Value = timeset;
            date_DatePicker1.SelectedDate = DateTime.Now;
            lbl_VideoFile.Text = "";

            completionTimer = new()
            {
                Interval = new TimeSpan(0, 0, 1)
            };
            completionTimer.Tick += PercentCompleteTracker;
            completionTimer.Start();
            this.Opened += ShowErrorAfterLoad;


            //Video Triming buttons
            if(!ENABLEVIDEOCUT)
            {
                pnl_TimeClipping.IsVisible = false;
                pnl_ClipButtons.IsVisible = false;
            }
            else
            {
                btn_VideoTrimStart.IsEnabled = false;
                btn_VideoTrimEnd.IsEnabled = false;
                btn_VideoTrimReset.IsEnabled = false;

                btn_VideoTrimStart.Click += TrimStart_Click;
                btn_VideoTrimEnd.Click += TrimEnd_Click;
                btn_VideoTrimReset.Click += TrimReset_Click;
            }



            RandomQuote();
        }

        private void CheckLicense(object sender, EventArgs e)
        {
            string configDir = configInfo.configDir;
            License testLicense = new(configDir);
            //For creating new License file,
            //  Comment out when done creating file
            testLicense.WriteNewLicenseFile(configDir, 2025);
            //
            //
            if (!testLicense.IsLicValid() )
            {
                ShowErrorMessageAndClose("License has expired as of " + testLicense.GetExpirationDate());
            }
        }


        private void ClearStuffAfterSave()
        {
            UpdateVideoPath("");
            combo_CategoryComboBox.SelectedIndex = -1;
            combo_CategoryComboBox.IsEnabled = false;
            combo_CatSubName.SelectedIndex = -1;
            txtb_Description.Clear();
            btn_Save.IsEnabled = false;
            combo_VideoQuality.SelectedIndex = 0;

        }
        private void ClearStuff()
        {

            ClearStuffAfterSave();

            //Stop video
            MediaPlayerStopVideo();
            UpdateVideoButtons(false);
            //Updates quote
            RandomQuote();

        }

        //Updates the video buttons
        private void UpdateVideoButtons(bool update)
        {
            btn_VideoPlay.IsEnabled = update;
            btn_VideoRewind.IsEnabled = update;
            btn_VideoFF.IsEnabled = update;
            btn_VideoRestart.IsEnabled = update;
            slider_VideoSlider.IsEnabled = update;
            
            btn_VideoTrimStart.IsEnabled = update;
            btn_VideoTrimEnd.IsEnabled = update;
            btn_VideoTrimReset.IsEnabled = update;
        }

        private void UpdateClipLabels()
        {
            lbl_trimStart.Content = gVideoClip[0].ToString(@"mm\:ss");
            lbl_trimEnd.Content = gVideoClip[1].ToString(@"mm\:ss");
            slider_VideoSlider.Ticks = gSliderTickList;
            
        }

        private void ResetClipTimeVariables()
        {
            gVideoClip[0] = TimeSpan.Zero;
            gVideoClip[1] = TimeSpan.Zero;
            gSliderTickList[0] = 0;
            gSliderTickList[1] = 0;
        }

        //updates the global variable and updates the view label
        private void UpdateVideoPath(string path)
        {
            currentVideoPath = path;
            if(path != "")
            {
                string tmpPath = Path.GetFileName(path);
                path = tmpPath;
                            }
            lbl_VideoFile.Text = path;

        }

        private void RandomQuote()
        {
            var fileName = Path.Combine(configInfo.configDir, "quotes.txt");
            var file = File.ReadLines(fileName).ToList();
            int count = file.Count;
            Random rnd = new();
            int skip = rnd.Next(0, count);
            string line = file.Skip(skip).First();
            textBlock_quotesLabel.Text = line;
        }

        //
        //
        //
        public string GetSecondaryLogo()
        {
            
            string returnPath = "";
            string catCombo = combo_CategoryComboBox.SelectedValue.ToString();
            
            if (catCombo == "Teams" || catCombo == "Teams - Private")
            {
                string nameCombo = combo_CatSubName.SelectedValue.ToString();
                if (configInfo.customLogos.ContainsKey(nameCombo))
                {
                    returnPath = configInfo.customLogos[nameCombo];
                }
            }
            else
            {
                string category = "Category";
                if (configInfo.customLogos.ContainsKey(category))
                {
                    //previously was using catCombo
                    returnPath = configInfo.customLogos[category];
                }
            }

            if(returnPath != "" && !File.Exists(returnPath))
            {
                //Throw error message
                string msg = "Secondary logo was not found: " + returnPath;
                //Debug.Write(msg);
                ShowErrorMessage(msg);
                returnPath = "";
            }
            return returnPath;
        }

        public string GetPrimaryLogo()
        {
            string returnPath="";

            if (configInfo.customLogos.ContainsKey("Primary"))
            {
                returnPath = configInfo.customLogos["Primary"];
            }
            
            if (returnPath != "" && !File.Exists(returnPath))
            {
                //Throw error message
                string msg = "Primary Logo was not found: " + returnPath;
                //Debug.Write(msg);
                ShowErrorMessage(msg);
                returnPath = "";
            }
            return returnPath;
        }

        private void SaveButtonHelper()
        {
            //Create variables to be used when creating unique filenames later 
            string sourcePath;
            string sourcePathFile; //Source path and file
            string uploadPath;
            string uploadPathFile;  //upload path and file

            DateTime pickedDateTime = (DateTime)date_DatePicker1.SelectedDate;

            string yearFolder = pickedDateTime.Year.ToString();
            string monthFolder = pickedDateTime.Month.ToString();
            string dateFolder = pickedDateTime.Day.ToString();
            string datePath = yearFolder + "\\" + monthFolder + "\\" + dateFolder;
            //Conditional for mac
            if (!winPlatform)
            {
                datePath = datePath.Replace('\\', '/');
            }

            string videoQuality;
            if (combo_VideoQuality.IsEffectivelyVisible)
            {
                videoQuality = combo_VideoQuality.SelectedValue.ToString();
            }
            else
            {
                videoQuality = "480";
            }

            string catCombo = combo_CategoryComboBox.SelectedValue.ToString();
            string nameCombo = "";

            if (sPanel_CatSubName.IsVisible)
            {
                nameCombo = combo_CatSubName.SelectedValue.ToString();
            }
            
            string fileName = CreateFileName();          //append unique identifier, resolution
            string extension = Path.GetExtension(currentVideoPath);
            string originalFile = fileName + extension;
            fileName += "_" + videoQuality + extension;

            string tmpStartSourcePath;
            string tmpStartUploadPath;

            if (catCombo == "Teams")    //structure for teams is team-folder/year/month/date/draw-resolution.mp4 
            {
                tmpStartSourcePath = Path.Combine(configInfo.unconvertedVideoDir, nameCombo);     //Create teamPath
                tmpStartUploadPath = Path.Combine(configInfo.TeamUploadTopFolder, nameCombo);    //Create teamUploadPath

            }
            else if (catCombo == "Teams - Private")    //structure for teams is team-folder/year/month/date/draw-resolution.mp4 
            {
                tmpStartSourcePath = Path.Combine(configInfo.unconvertedVideoDir, nameCombo);                //Create PrivateteamPath
                tmpStartUploadPath = Path.Combine(configInfo.PrivateTeamUploadTopFolder, nameCombo);    //Create PrivateTeamUploadPath
            }
            else
            {
                tmpStartSourcePath = configInfo.unconvertedVideoDir;
                tmpStartUploadPath = configInfo.generalCategoryDir;
            }

            sourcePath = Path.Combine(tmpStartSourcePath, datePath);
            sourcePathFile = Path.Combine(sourcePath, originalFile);

            uploadPath = Path.Combine(tmpStartUploadPath, datePath);
            uploadPathFile = Path.Combine(uploadPath, fileName);

            Directory.CreateDirectory(sourcePath);
            Directory.CreateDirectory(uploadPath);

            string fileNameOnly = Path.GetFileNameWithoutExtension(sourcePathFile);
            string UploadNameOnly = Path.GetFileNameWithoutExtension(uploadPathFile);
            
            string newSourcePathFile = sourcePathFile;
            string newUploadPathFile = uploadPathFile;

            //Make sure the filename is unique
            int count = 1;      //int used to increment filename when duplicates are created
            while (File.Exists(newSourcePathFile))
            {
                string tempFileName = string.Format("{0}({1})", fileNameOnly, count);
                newSourcePathFile = Path.Combine(sourcePath, tempFileName + extension);

                string tempUploadName = string.Format("{0}({1})", UploadNameOnly, count);
                newUploadPathFile = Path.Combine(uploadPath, tempUploadName + extension);
                count++;
            }
            
            
            //copy video from camera to HDD
            try
            {
                //FileSystem.CopyFile(currentVideoPath, @"" + newSourcePathFile + @"", UIOption.OnlyErrorDialogs);
                FileSystem.CopyFile(currentVideoPath, @"" + newSourcePathFile + @"");
                long currentVideoPosition = _mp.Time;
                ThreadPool.QueueUserWorkItem(_ => MediaPlayerPlayVideo(newSourcePathFile, currentVideoPosition));

            }
            catch (Exception ex)
            {
                string tmpString = "Error copying over file. " + ex.Message;
                Debug.Write(tmpString + Environment.NewLine);
                Console.Write(tmpString + Environment.NewLine);
                ShowErrorMessage(tmpString);
                
            }       

            string secondaryLogoPath = GetSecondaryLogo();
            string primaryLogoPath = GetPrimaryLogo();
            //Added getting primary logo

            string[] tmpClipTimes = {"",""};
            tmpClipTimes = GetTrimStartEnd();

            //these next variables are named after my friend Bill, who introduced me to skydiving:
            VideoData BillvideoData = new()
            {
                SourcePath = newSourcePathFile,
                UploadPath = newUploadPathFile,
                Resolution = videoQuality,
                SecondaryLogoPath = secondaryLogoPath,
                PrimaryLogoPath = primaryLogoPath,
                FileCreated = DateTime.Now,
                ClipStartTime = tmpClipTimes[0],
                ClipEndTime = tmpClipTimes[1],
                BeingConverted = false
            };

            ListOfFiles ??= new ConcurrentQueue<VideoData>();
            ListOfFiles.Enqueue(BillvideoData);
            ClearStuffAfterSave();
            if (!FlagConverting)
            {
                FlagConverting = true;
                Task.Run(async () => await ConvertAllVideos());
            }
        }
        
        private async Task ConvertAllVideos()
        {
            while ( !ListOfFiles.IsEmpty )
            {
                var videoData = ListOfFiles.OrderBy(f => f.FileCreated).First();
                if (videoData.BeingConverted == false)
                {
                    await ConvertSingleVideo(videoData);
                }
                
            }
            FlagConverting = false;
        }

        private async Task ConvertSingleVideo(VideoData videoDataConverting)       //method to convert video
        {
            
            if (ListOfFiles == null || ListOfFiles.IsEmpty)
            {
                FlagConverting = false;
                return;
            }  //exit if list is empty
            
            
            //Set the being converted flag
            videoDataConverting.BeingConverted = true;

            if (!File.Exists(configInfo.ffmpegLocation))
            {
                ShowErrorMessage("ffmpeg could not be found. Your install is corrupted");
                ListOfFiles.Clear();

                return;
            }
                        

            //   Building the argument list
            //          Places the images in the center
            StringBuilder ffmpegArgs = new();
            //ffmpegArgs.Append("-hwaccel auto ");                    // Enable hardware acceleration
            ffmpegArgs.Append($"-i \"{videoDataConverting.SourcePath}\" ");
            ffmpegArgs.Append(GetLogoOverlayArgs(videoDataConverting));

            if (!string.IsNullOrEmpty(videoDataConverting.ClipStartTime))
            {
                ffmpegArgs.Append($"-ss {videoDataConverting.ClipStartTime} -to {videoDataConverting.ClipEndTime} ");
            }

            string iBitrate = GetBitRate();
            ffmpegArgs.Append($"-s hd{videoDataConverting.Resolution} -c:v libx264 -crf {iBitrate} -an \"{videoDataConverting.UploadPath}\"");
            /*
            //Hardware acceleration encoding
            if (winPlatform)        
            {
                ffmpegArgs.Append($"-s hd{videoDataConverting.Resolution} -c:v h264_nvenc -crf {iBitrate} -an \"{videoDataConverting.UploadPath}\""); // Use NVENC for Windows
            }
            else
            {
                ffmpegArgs.Append($"-s hd{videoDataConverting.Resolution} -c:v h264_videotoolbox -crf {iBitrate} -an \"{videoDataConverting.UploadPath}\""); // Use VideoToolbox for macOS
            }
            */

            if (!winPlatform)
            {
                ffmpegArgs.Replace('\\', '/');
            }
            //End of ffmpeg arguments creation

            //----DEBUG----//
            //Debug.WriteLine("From ffmpeg: " + ffmpegArgs);

            Process ffmpeg = new()
            {
                StartInfo =
                {
                    FileName = configInfo.ffmpegLocation,
                    Arguments = ffmpegArgs.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                    //WorkingDirectory = configInfo.workingDirectoryVar
                },
                EnableRaisingEvents = false
            };
            try
            {
                ffmpeg.Start();
                completionTimer.Start();
                // Run ShowPercentComplete concurrently
                //var progressTask = Task.Run(() => ShowPercentComplete());
                ShowPercentComplete(ffmpeg);
                //completionTimer.Tick += PercentCompleteTracker;

                // Wait for ffmpeg to complete
                //await ffmpeg.WaitForExitAsync();

                // Ensure progress tracking completes
                //await progressTask;
                

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception after ffmpeg.Start: " + ex.Message);
                ShowErrorMessage("Exception after ffmpeg.Start: " + ex.Message);
            }

            //ListOfFiles.Remove(videoDataConverting);          moved to show function
            
        }

        private static string GetLogoOverlayArgs(VideoData videoData)
        {
            //if the resolution is 720, scale the images
            string scalingOneLogo = "-filter_complex \"overlay=x=W/2-w/2-10:y=H-h-10\"";
            string scalingTwoLogos = "-filter_complex \"[0][1]overlay=x=W/2-w-10:y=H-h-10[v1];[v1][2]overlay=W/2+10:y=H-h-10[v2]\" -map \"[v2]\" ";

            if (videoData.Resolution != "1080")
            {
                //If the image needs to be scaled
                scalingOneLogo = "-filter_complex \"[1]scale=0.7*iw:0.7*ih[p1],[0][p1]overlay=x=W/2-w/2-10:y=H-h-10\"";
                scalingTwoLogos = "-filter_complex \"[1]scale=0.7*iw:0.7*ih[p1];[0][p1]overlay=x=W/2-w-10:y=H-h-10[v1];[2]scale=0.7*iw:0.7*ih[p2];[v1][p2]overlay=W/2+10:y=H-h-10[v2]\" -map \"[v2]\" ";
            }


            if (!string.IsNullOrEmpty(videoData.PrimaryLogoPath) && !string.IsNullOrEmpty(videoData.SecondaryLogoPath))
            {
                return $"-i \"{videoData.PrimaryLogoPath}\" -i \"{videoData.SecondaryLogoPath}\" {scalingTwoLogos} ";
            }
            if (!string.IsNullOrEmpty(videoData.PrimaryLogoPath))
            {
                return $"-i \"{videoData.PrimaryLogoPath}\" {scalingOneLogo} ";
            }
            if (!string.IsNullOrEmpty(videoData.SecondaryLogoPath))
            {
                return $"-i \"{videoData.SecondaryLogoPath}\" {scalingOneLogo} ";
            }
            return string.Empty;
        }

        private string GetBitRate()
        {
            string returnString = "23";
            if(configInfo.bitRate == 1)
            {
                returnString = "20";
            }

            return returnString;
        }
        
        private async void ShowPercentComplete(Process ffmpeg)
        {
            string duration = "";
            string patternDuration = @"Duration:\s(\d+:\d+:\d+\.\d+)";
            string patternTime = @"time=(\d+:\d+:\d+\.\d+)";

            try
            {
                using StreamReader reader = ffmpeg.StandardError;
                string line;

                while ((line = reader.ReadLine()) != null)
                //while ((line = await reader.ReadLineAsync()) != null)
                {
                    //----DEBUG----//
                    //Debug.WriteLine("From ffmpeg: " + line);

                    // Try to capture total duration
                    if (string.IsNullOrEmpty(duration))
                    {
                        Match matchDuration = Regex.Match(line, patternDuration);
                        if (matchDuration.Success)
                        {
                            duration = matchDuration.Groups[1].Value;
                        }
                    }

                    // Capture current progress time
                    Match matchTime = Regex.Match(line, patternTime);
                    if (matchTime.Success && !string.IsNullOrEmpty(duration))
                    {
                        string currentDuration = matchTime.Groups[1].Value;
                        TimeSpan totalDuration = TimeSpan.Parse(duration);
                        TimeSpan currentTime = TimeSpan.Parse(currentDuration);

                        percent = totalDuration.TotalSeconds == 0 ? 0 :
                            Convert.ToInt32((currentTime.TotalSeconds / totalDuration.TotalSeconds) * 100);

                        //----DEBUG----//
                        Debug.WriteLine($"Conversion complete: {percent}");
                    }
                }

                reader.Dispose();       //Not sure if this is really needed
                ffmpeg.Dispose();       //Not sure if this is really needed
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ShowPercentComplete: {ex.Message}");
                ShowErrorMessage($"ffmpeg Error: {ex.Message}");
            }

            //Everything should be done
            ListOfFiles.TryDequeue(out _);

        }
        
        /*
        private async void ShowPercentComplete()
        {
            //Calculates percent complete
            string line, current_duration, duration = "";
            StreamReader reader = ffmpeg.StandardError;
            while ((line = reader.ReadLine()) != null)
            {
                //----DEBUG----//
                //Debug.WriteLine("From ffmpeg: " + line);

                if (!string.IsNullOrEmpty(line))
                {
                    if (line.Contains("Duration") && line.Contains("bitrate") && line.Contains("start") && line.Contains("kb/s"))
                    {
                        int startPos = line.LastIndexOf("Duration: ") + "Duration: ".Length + 1;
                        int length = line.IndexOf(", start:") - startPos;
                        string sub = line.Substring(startPos, length);
                        duration = sub;
                        //----DEBUG----//
                        //Debug.WriteLine(duration);
                    }
                    if (line.Contains("frame=") && line.Contains("size=") && line.Contains("time="))
                    {
                        int startPos = line.LastIndexOf("time=") + "Time=".Length + 1;
                        int length = line.IndexOf(" bitrate=") - startPos;
                        string sub = line.Substring(startPos, length);
                        
                        if (sub.Contains(':'))
                        {
                            current_duration = sub;
                            //----DEBUG----//
                            //Debug.WriteLine(current_duration);
                            percent = Convert.ToInt32(Math.Round(TimeSpan.Parse(duration).TotalSeconds)) == 0 ? 0 :
                                Convert.ToInt32(Math.Round((TimeSpan.Parse(current_duration).TotalSeconds * 100) / TimeSpan.Parse(duration).TotalSeconds, 5));
                            //----DEBUG----//
                            //Debug.WriteLine("Conversion complete: " + percent);
                        }

                    }
                }
            }
        }
        */

        //
        //Creates the filname for the video
        //
        public string CreateFileName()
        {
            string newFileName;
            string catCombo = combo_CategoryComboBox.SelectedValue.ToString();
            string dirtyFileName = "";
            if (catCombo == "Teams" || catCombo == "Teams - Private")  //shows dropbox for draw
            {
                dirtyFileName += txtb_Description.Text;
            }
            else
            {
                if (sPanel_CatSubName.IsVisible)
                {
                    string nameCombo = combo_CatSubName.SelectedValue.ToString();
                    if (nameCombo != "")
                    {
                        dirtyFileName += nameCombo + " - ";   //starts filename with name from combo_CatSubName.Text so nameTheFile adds the draw after the person or team named there
                    }
                }
                dirtyFileName += txtb_Description.Text;
            }
            newFileName = Regex.Replace(dirtyFileName, @"([^a-zA-Z0-9_ ]|^\s)", "-");

            return newFileName;
        }

        private bool IsTrimValid()
        {
            bool rtn = false;
            if (gVideoClip[0] == TimeSpan.Zero && gVideoClip[1] == TimeSpan.Zero)
            {
                rtn = true;
            }
            else
            {
                int iCompare = TimeSpan.Compare(gVideoClip[0], gVideoClip[1]);
                if (iCompare < 0)
                {
                    rtn = true;
                }
            }

            return rtn;
        }
        
        public string[] GetTrimStartEnd()
        {
            string[] tmpString = {"",""};
            if (gVideoClip[1] != TimeSpan.Zero)
            {

                tmpString[0] = gVideoClip[0].ToString(@"hh\:mm\:ss\.fff");
                tmpString[1] = gVideoClip[1].ToString(@"hh\:mm\:ss\.fff");
            }

            return tmpString;
        }

        public void RunCleanup()
        {
            
            if (configInfo.cleanupAfterDays > 0)
            {
                //Are we converting files
                //if(ListOfFiles.Count == 0)
                //{
                    FolderCleanup cleanupFolder = new(configInfo.cleanupAfterDays, configInfo.unconvertedVideoDir);
                    string cleanupErrors = cleanupFolder.CleanUpFolder();
                    if (cleanupErrors != "")
                    {
                        ShowErrorMessage(cleanupErrors);
                    }
                //}
                
            }
        }

        //---------
        //---------End of Helper functions
        //---------

        //---------
        //---------Helper Events
        //---------

        private async Task<string> ReturnSeachFile()
        {
            if (initVideoDirectory == null || !Directory.Exists(initVideoDirectory))
            {
                initVideoDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            // Get top level from the current control
            var topLevel = TopLevel.GetTopLevel(this);
            var folderPathToStart = await topLevel.StorageProvider.TryGetFolderFromPathAsync(initVideoDirectory);
            // Setting options            
            var options = new FilePickerOpenOptions
            {
                Title = "Select video file",
                AllowMultiple = false,
                SuggestedStartLocation = folderPathToStart,
                FileTypeFilter = new FilePickerFileType[] { new("MP4 file") { Patterns = new[] { "*.mp4", "*.MP4" } } }
            };
            
            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            string sResult = "";
            if (files.Count > 0)
            {
                sResult = files[0].TryGetLocalPath();
                initVideoDirectory = System.IO.Path.GetDirectoryName(sResult);
            }
              
            return sResult;

        }

        private void ReloadComboBoxes(object sender, EventArgs e)
        {
            Load_ComboBoxes();
        }
        
        private void ReloadConfigValues(ConfigStruct tmpConfigInfo)
        {
            this.configInfo = tmpConfigInfo;
        }
        
        private void CategoriesBox_SelectedIndexChanged(object sender, EventArgs e) //populate combo_CatSubName ComboBox and display correct labels and boxes
        {
            combo_CatSubName.SelectedIndex = -1;
            txtb_Description.IsEnabled = false;
            if (combo_CategoryComboBox.SelectedIndex == -1)
            {
                combo_CatSubName.IsEnabled = false;
            }
            if (combo_CategoryComboBox.SelectedIndex != -1)
            {
                btn_Save.IsEnabled = false;
                combo_CatSubName.IsEnabled = true;

                String pickedCategory = combo_CategoryComboBox.SelectedItem.ToString();
                
                combo_CatSubName.Items.Clear();
                if (pickedCategory == "Fun Jumpers")
                {
                    sPanel_CatSubName.IsVisible = false;
                    txtb_Description.IsEnabled = true;
                }
                else
                {
                    sPanel_CatSubName.IsVisible= true;
                    for (int i = 0; i < categoriesDic[pickedCategory].Length; i++)
                    {
                        combo_CatSubName.Items.Add(categoriesDic[pickedCategory][i].ToString());
                    }
                }

            }
            
        }

        private void NamesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (combo_CatSubName.SelectedIndex == -1)
            {
                btn_Save.IsEnabled = false;
            }
            if (combo_CatSubName.SelectedIndex != -1)
            {
                txtb_Description.IsEnabled = true;
                pnl_VideoQuality.IsVisible = true;
                if (combo_CatSubName.SelectedItem.ToString() == "Rhythm")
                {
                    combo_VideoQuality.SelectedIndex=1;
                }
            }
        }

        private void ShowErrorAfterLoad(object sender, EventArgs e)
        {
            if (myErrorsOnLoad != "")
            {
                ShowErrorMessage(myErrorsOnLoad);
                myErrorsOnLoad = "";

            }

        }
        private static async void ShowErrorMessage(string tmpMsg)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", tmpMsg,
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error,
                    WindowStartupLocation.CenterOwner);
            await box.ShowWindowAsync();

        }

        private async void ShowErrorMessageAndClose(string tmpMsg)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", tmpMsg,
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error,
                    WindowStartupLocation.CenterOwner);
            
            if (await box.ShowWindowAsync() == MsBox.Avalonia.Enums.ButtonResult.Ok)
            {
                CloseApp();
            }

        }


        private async void SL_TimeChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(async () => await UpdateTimerFromSlider(e), DispatcherPriority.Background);
        }
        
        private Task UpdateTimerFromSlider(RangeBaseValueChangedEventArgs e)
        {
            _mp.Time = (long)e.NewValue;
            //_mp.Time =(long)slider_VideoSlider.Value;
            return Task.CompletedTask;
        }

        private async void MP_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(async () => await UpdateTimeLabelsFromVideo(), DispatcherPriority.Background);
        }

        private Task UpdateTimeLabelsFromVideo()
        {
            TimeSpan currentTime = TimeSpan.FromMilliseconds(_mp.Time);
            TimeSpan endTime = TimeSpan.FromMilliseconds(_mp.Length);
            
            lbl_VideoCurrentTime.Content = currentTime.ToString(@"mm\:ss");
            lbl_VideoEndTime.Content = endTime.ToString(@"mm\:ss");

            //Update Slider bar
            slider_VideoSlider.ValueChanged -= SL_TimeChanged;
            slider_VideoSlider.IsEnabled = true;
            slider_VideoSlider.Maximum = _mp.Length;
            slider_VideoSlider.Value = _mp.Time;
            slider_VideoSlider.ValueChanged += SL_TimeChanged;
            return Task.CompletedTask;
        }
        
        private void DescriptionTextBox_TextChanged(object sender, EventArgs e)
        {
            if (txtb_Description.Text.Length > 0)
            { btn_Save.IsEnabled = true; }
            else
            { btn_Save.IsEnabled = false; }
        }

        
        //
        //---------App is closing, clean needs to happen
        //
        private async void AppClosingTest(object sender, WindowClosingEventArgs e)
        {
            // Display a MsgBox asking the user if they really want to exit.

            if (!ListOfFiles.IsEmpty)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Exiting", @"Videos still being converted: " + ListOfFiles.Count + "\nAre you sure you want to Exit",
                    MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question,
                    WindowStartupLocation.CenterOwner);

                e.Cancel = true;
            
                if ( await box.ShowWindowAsync() == MsBox.Avalonia.Enums.ButtonResult.Yes)
                {
                    CloseApp();
                }
            }

        }

        private void CloseApp()
        {
            _media?.Dispose();          //Checks if it's null and then runs Dispose
            _mp?.Dispose();
            _libVLC?.Dispose();
            mutex.ReleaseMutex();
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime) lifetime.Shutdown();
        }


        private void PercentCompleteTracker(object sender, EventArgs e)
        {
            Debug.WriteLine("PercentCompleteTrackerEvent: " + percent + " List count: " + ListOfFiles.Count );

            if (percent < 100 && !ListOfFiles.IsEmpty)
            {
                lbl_progressLabel.IsVisible = true;
                string fileShow = Path.GetFileNameWithoutExtension(ListOfFiles.OrderBy(f => f.FileCreated).First().UploadPath);
                lbl_progressLabel.Text = percent.ToString() + "% (1 of " + ListOfFiles.Count + " ) " + fileShow;
            }
            else if ( ListOfFiles.IsEmpty )
            {
                lbl_progressLabel.Text = "";
                lbl_progressLabel.IsVisible = false;
                completionTimer.Stop();
                //Debug.WriteLine("PercentCompleteTrackerEvent: done and list clear");
            }
        }

        //---------
        //---------Button Click Events
        //---------

        private async void SearchButton_Click(object sender, EventArgs e)        //Video File Selection Button
        {
           
            string sResult = await ReturnSeachFile();
            if(sResult != "")
            {
                combo_CategoryComboBox.IsEnabled = true;
                UpdateVideoPath(sResult);
                ResetClipTimeVariables();
                PlayVideo(sResult);
            }

        }
        
        private void ClearButton_Click(object sender, EventArgs e)
        {
            ClearStuff();
        }
        
        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (IsTrimValid())
            {
                SaveButtonHelper();
            }
            else
            {
                ShowErrorMessage("Please fix or reset the video trim options");
            }
        }
        //Gets the Path of the secondary logo, if it exists
        

        //
        //-------Video button click events
        //
        private void VideoPlayPause_Click(object sender, EventArgs e)
        {
            if (_mp.IsPlaying)
            {
                ThreadPool.QueueUserWorkItem(_ => _mp.Pause());
            }
            else
            {
                ThreadPool.QueueUserWorkItem(_ => _mp.Play());
            }
        }

        private void VideoRestart_Click(Object sender, EventArgs e)
        {

            if ( _mp.State != VLCState.Error )
            {
                _mp.Position = 0;
            }
        }
        
        private void VideoRwd_Click(object sender, EventArgs e)
        {
            float vlc_currentPos = _mp.Position;
            float vlc_newPos = vlc_currentPos - 0.05f;
            if (vlc_newPos < 0)
            {
                vlc_newPos = 0;
            }
            // _mp.SetPosition(vlc_newPos);
            _mp.Position = vlc_newPos;
        }

        private void VideoFwd_Click(object sender, EventArgs e)
        {
            float vlc_currentPos = _mp.Position;
            float vlc_newPos = vlc_currentPos + 0.05f;
            if (vlc_newPos > 1)
            {
                vlc_newPos = 1;
            }
            //_mp.SetPosition(vlc_newPos);
            _mp.Position = vlc_newPos;
        }

        private void TrimStart_Click(object sender, EventArgs e)
        {
            if (_mp.State != VLCState.Error)
            {
                TimeSpan currentTime = TimeSpan.FromMilliseconds(_mp.Time);

                gVideoClip[0] = currentTime;
                lbl_trimStart.Content = currentTime.ToString(@"mm\:ss");
                //Dispatcher.UIThread.Post(() => UpdateClipLabels(), DispatcherPriority.Background);
                gSliderTickList[0] = slider_VideoSlider.Value;
                //slider_VideoSlider.Ticks = gSliderTickList;
            }
        }

        private void TrimEnd_Click(object sender, EventArgs e)
        {
            if (_mp.State != VLCState.Error)
            {
                TimeSpan currentTime = TimeSpan.FromMilliseconds(_mp.Time);

                gVideoClip[1] = currentTime;
                lbl_trimEnd.Content = currentTime.ToString(@"mm\:ss");
                //Dispatcher.UIThread.Post(() => UpdateClipLabels(), DispatcherPriority.Background);

                gSliderTickList[1] = slider_VideoSlider.Value;
                //slider_VideoSlider.Ticks = tickList;
            }
        }

        private void TrimReset_Click(object sender, EventArgs e)
        {
            if (_mp.State != VLCState.Error)
            {
                ResetClipTimeVariables();
                UpdateClipLabels();
                //Dispatcher.UIThread.Post(() => UpdateClipLabels(), DispatcherPriority.Background);
            }
        }

        //
        //-------Timer button click events
        //
        private void StartTimerButton_Click(object sender, EventArgs e)
        {
            timeset = Convert.ToInt32(cnt_TimerValue.Value);
            int secondsEntered = timeset;
            timer.SetTime(0, secondsEntered);
            timer.Start();
            timer.TimeChanged = Timer_TickChange;
            cnt_TimerValue.IsEnabled = false;
            timer.StepMs = 100;
            btn_startTimer.IsVisible = false;
            btn_addPoint.IsVisible = true;
            btn_addPoint.IsEnabled = true;
            btn_stopTimer.IsVisible = true;
            btn_stopTimer.IsEnabled = true;
            btn_resetTimer.IsEnabled = false;
            //btn_startTimer
            //btn_addPoint
            //btn_stopTimer
            //btn_resetTimer

        }   //startbutton for timer and points counter
                
        private void ResetTimerButton_Click(Object sender, EventArgs e)
        {
            cnt_TimerValue.IsEnabled = true;
            cnt_TimerValue.Value = timeset;
            cnt_TimerValue.Background = Brushes.Black;
            lbl_pointValue.Content = "0";
            //btn_stopTimer.IsEnabled = true;
            btn_addPoint.IsEnabled = false;
            btn_addPoint.IsVisible = false;
            lbl_pointValue.Content = "0";
            btn_startTimer.IsVisible = true;
            btn_startTimer.IsEnabled = true;
            
            counter = 0;
        }
       
        private void StopTimerButton_Click(object sender, EventArgs e)
        {
            timer.Stop();
            //btn_startTimer.IsVisible = true;
            btn_startTimer.IsEnabled = true;
            //cnt_TimerValue.IsEnabled = true;
            //btn_addPoint.IsVisible = false;
            btn_addPoint.IsEnabled = false;
            //TimerPanel.BackColor = Color.Black;
             
            cnt_TimerValue.Background = Brushes.Black;
            btn_stopTimer.IsEnabled = false;
            btn_resetTimer.IsEnabled = true;

        }
        
        private void Timer_TickChange()
        {
            cnt_TimerValue.Text = timer.TimeLeftMsStr;
            if (cnt_TimerValue.Value == 0)
            {
                btn_addPoint.IsEnabled = false;
                //secondsUpdown.Enabled = false;
                btn_stopTimer.IsEnabled = false;
                btn_resetTimer.IsEnabled = true;
            }
            if (cnt_TimerValue.Value == 5)
            {
                cnt_TimerValue.Background = Brushes.Red;
            }
        }

        private void PointButton_Click(object sender, EventArgs e)  //increments the number of points while timer is running
        {
            ++counter; ;
            string points = counter.ToString();
            lbl_pointValue.Content = points;
        }
        
        

        //---------
        //---------Menu Click Events
        //---------
        public void Menu_AboutClick()
        {
            AboutWindow aboutDialog = new();
            var ownerWindow = this;
            aboutDialog.ShowDialog(ownerWindow);
        }

        public void Menu_CategoryNamesClick()
        {
            _categoriesWindow = new(configInfo, categoriesDic);
            _categoriesWindow.Closing += ReloadComboBoxes;
            var ownerWindow = this;
            _categoriesWindow.ShowDialog(ownerWindow);
        }

        public void Menu_SettingsClick()
        {
            _settingsWindow = new(configInfo);
            _settingsWindow.Closing += (sender, e) => ReloadConfigValues(_settingsWindow.myConfigInfo);
            var ownerWindow = this;
            _settingsWindow.ShowDialog(ownerWindow);
        }
        public void Menu_LogoClick()
        {
            _logoWindow = new(configInfo, categoriesDic);
            var ownerWindow = this;
            _logoWindow.ShowDialog(ownerWindow);
        }

    }
    
}