using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
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
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using System.Runtime.InteropServices;
using Avalonia.Platform.Storage;
using Avalonia.Utilities;

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
        private bool winPlatform;            //1 if windows, 0 if not windows
        private string myErrorsOnLoad = "";      //specifically for load to show errors
        private Utils newUtil;              //used for common functions

        //----used in ShowPercentComplete when files are being converted
        private DispatcherTimer completionTimer;
        int percent;

        //----used in ConvertVideo

        Process ffmpeg;
        bool FlagConverting = false;
        List<VideoData> ListOfFiles = new();
        VideoData BillvideoData;				//Does NOT need to be Global
        VideoData videoDataConverting;			//Doesn't seem like it needs to be Global

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

        //---------
        //--------- functions
        //---------

        public void LukmaStartup()
        {
            InitializeComponent();
            winPlatform = IsPlatformWindows();
            this.Closing += AppClosingTest;
           
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
            newUtil = new Utils();
            configInfo = new ConfigStruct();
            categoriesDic = new SortedDictionary<string, string[]> { };
            ReadConfig();
            ReadCategoryFiles();
            myErrorsOnLoad += newUtil.ReadCustomLogos(configInfo);
            Load_ComboBoxes();
            InitializeButtonEventsLabels();
            
                          

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
                            myErrorsOnLoad = "Errors found with settings file. Some options were reverted back to defaults";
                        }

                    }
                }
                if (updateList.Count > 0)
                {
                    foreach (string[] tmpString in updateList)
                    {
                        newUtil.UpdateConfigFile(configInfo, tmpString[0], tmpString[1]);
                    }
                }

            }
            else        //File doesn't exist, write it out
            {
                myErrorsOnLoad = "Error: Settings file doesn't exist. Close and reopen Lukma";
                newUtil.UpdateConfigFile(configInfo, "localVideoDir", configInfo.unconvertedVideoDir);
                newUtil.UpdateConfigFile(configInfo, "convertedVideosTopDir", configInfo.convertedVideosTopDir);
            }

            UpdateConfigPaths();
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

                newUtil.CopyConfigsForMac(baseConfigAndLogoDir, configInfo);
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
                configInfo.ffmpegLocation = Path.Combine(configInfo.appDir, "Assets", "ffmpeg");
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
                        using (StreamReader sr = new(filePath))
                        {
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
                MediaPlayerPlayVideo(file);
                                
                //slider_VideoSlider

                //Buttons
                Dispatcher.UIThread.Post(() => UpdateVideoButtons(true), DispatcherPriority.Normal);

            }
            catch (Exception ex)
            {
                Debug.Write("During play video " + ex.Message);
                Console.Write("During play video " + ex.Message);
            }

            /*VideoFileNameBox.Text = OpenFileDialog1.FileName;               //Show the chosen filename in a textbox
            if (OpenFileDialog1.FileName != null
                && OpenFileDialog1.FileName != "")
            {
                combo_CategoryComboBox.IsEnabled = true;
            }
            else
            {
                combo_CategoryComboBox.IsEnabled = false;
            }*/
        }

        private void MediaPlayerPlayVideo(string filename)
        {

            if (File.Exists(filename))
            {
               
                MediaPlayerStopVideo();
                _media = new Media(_libVLC, filename);

                //Video slider initialization
                                
                _mp.Play(_media);
                _mp.TimeChanged += MP_TimeChanged;
                slider_VideoSlider.ValueChanged += SL_TimeChanged;
                _mp.Mute = true;
                _media?.Dispose();
                Dispatcher.UIThread.Post(() => UpdateVideoButtons(true), DispatcherPriority.Normal);
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

            RandomQuote();
        }
        private void ClearStuffAfterSave()
        {
            UpdateVideoPath("");
            combo_CategoryComboBox.SelectedIndex = -1;
            combo_CategoryComboBox.IsEnabled = false;
            combo_CatSubName.SelectedIndex = -1;
            txtb_Description.Clear();
            btn_Save.IsEnabled = false;

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
        //This will have to be redone to support setting secondary logos anywhere
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
                if (configInfo.customLogos.ContainsKey(catCombo))
                {
                    returnPath = configInfo.customLogos[catCombo];
                }
            }

            if(returnPath != "" && !File.Exists(returnPath))
            {
                //Throw error message
                string msg = "Secondary logo was not found: " + returnPath;
                Debug.Write(msg);
                _ = MessageBoxManager.GetMessageBoxStandard("Error", @"msg",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error,
                    WindowStartupLocation.CenterOwner);
                returnPath = "";
            }
            return returnPath;
        }

        public string GetPrimaryLogo()
        {
            string returnPath;

            if (configInfo.customLogos.ContainsKey("Primary"))
            {
                returnPath = configInfo.customLogos["Primary"];
            }
            else
            {
                returnPath = Path.Combine(configInfo.logoDir, "SDCLogo_1080.png");
            }
            
            if (!File.Exists(returnPath))
            {
                //Throw error message
                string msg = "Primary Logo was not found: " + returnPath;
                Debug.Write(msg);
                _ = MessageBoxManager.GetMessageBoxStandard("Error", @"msg",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error,
                    WindowStartupLocation.CenterOwner);
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
                if (nameCombo == "Rhythm")
                {
                    videoQuality = "480";
                }
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
                ThreadPool.QueueUserWorkItem(_ => MediaPlayerPlayVideo(newSourcePathFile));

            }
            catch (Exception ex)
            {
                string tmpString = "Error copying over file. " + ex.Message;
                Debug.Write(tmpString + Environment.NewLine);
                Console.Write(tmpString + Environment.NewLine);
                _ = MessageBoxManager.GetMessageBoxStandard("Error", @"tmpString",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error,
                    WindowStartupLocation.CenterOwner);
            }       

            string secondaryLogoPath = GetSecondaryLogo();

            //these next variables are named after my friend Bill, who introduced me to skydiving:
            BillvideoData = new VideoData
            {
                SourcePath = newSourcePathFile,
                UploadPath = newUploadPathFile,
                Resolution = videoQuality,
                SecondaryLogoPath = secondaryLogoPath,                                 //What is this really used for
                FileCreated = DateTime.Now
            };

            ListOfFiles ??= new List<VideoData>();
            ListOfFiles.Add(BillvideoData);
            ClearStuffAfterSave();
            if (!FlagConverting)
            {
                FlagConverting = true;
                ConvertVideo();
            }
        }

        private async void ConvertVideo()       //method to convert video
        {
            if (ListOfFiles == null || ListOfFiles.Count == 0)
            {
                FlagConverting = false;
                return;
            }  //exit if list is empty
            videoDataConverting = new VideoData();    //creates class for this instance
            DateTime OldestFile = DateTime.Now;     //instantiates OldestFile variable

            foreach (VideoData thisfile in ListOfFiles)
            {
                if (thisfile.FileCreated < OldestFile) { videoDataConverting = thisfile; }
            }   //find oldest file to convert


            string logo1 = GetPrimaryLogo();

            Task converter = Task.Run(() =>                   //convert video to lower res
            {

                //   Building the argument list
                //          Places the images in the center
                string ffmpegArgs = "-i \"" + videoDataConverting.SourcePath + "\" ";
                string scalingOnce = "";
                string scalingTwice = "-filter_complex \"[0][1]overlay=x=W/2-w-10:H-h-10[v1];[v1][2]overlay=W/2+10:H-h-10[v2]\" -map \"[v2]\" ";

                if (videoDataConverting.Resolution != "1080")
                {
                    //If the image needs to be scaled
                    scalingOnce = "[1]scale=0.7*iw:0.7*ih[p1],[0][p1]";
                    scalingTwice = "-filter_complex \"[1]scale=0.7*iw:0.7*ih[p1];[0][p1]overlay=x=W/2-w-10:H-h-10[v1];[2]scale=0.7*iw:0.7*ih[p2];[v1][p2]overlay=W/2+10:H-h-10[v2]\" -map \"[v2]\" ";
                }

                if (videoDataConverting.SecondaryLogoPath != "")
                {
                    string logo2 = videoDataConverting.SecondaryLogoPath;
                    ffmpegArgs = ffmpegArgs +
                    "-i \"" + logo1 + "\" " +
                        "-i \"" + logo2 + "\" " +
                        scalingTwice;
                }
                else
                {
                    ffmpegArgs = ffmpegArgs +
                        "-i \"" + logo1 + "\" " +
                        //"-filter_complex overlay=x=W/2-w/2-10:H-h-10 ";       //original with no scaling
                        //"-filter_complex scale=2*iw:2*ih,overlay=x=W/2-w/2-10:H-h-10 "; //long form
                        "-filter_complex " + scalingOnce + "overlay=x=W/2-w/2-10:H-h-10 ";
                }
                                
                ffmpegArgs = ffmpegArgs +
                    "-s hd" + videoDataConverting.Resolution + " " +
                    "-c:v libx264 " +
                    "-crf 23 " +
                    "-c:a aac " +
                    "-strict -2 " +
                    "-an \"" + videoDataConverting.UploadPath + "\"";

                //Conditional for mac
                if (!winPlatform)
                {
                    ffmpegArgs = ffmpegArgs.Replace('\\', '/');
                }

                //End of ffmpeg arguments creation
                ffmpeg = new Process
                {
                    StartInfo =
                         {
                             FileName = configInfo.ffmpegLocation,
                             Arguments = ffmpegArgs,
                             UseShellExecute = false,
                             RedirectStandardOutput = true,
                             RedirectStandardError = true,
                             CreateNoWindow = true
                             //WorkingDirectory = configInfo.workingDirectoryVar
                         },
                    EnableRaisingEvents = false
                };

                ffmpeg.Start();

                //for debuging purposes
                //string stdout = ffmpeg.StandardOutput.ReadToEnd();
                //string stderr = ffmpeg.StandardError.ReadToEnd();
                try
                {
                    //Debug.WriteLine("Percent complete run");
                    ShowPercentComplete();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("During show complete: " + ex.Message);
                    Console.WriteLine("During show complete: " + ex.Message);
                }

                ListOfFiles.Remove(videoDataConverting);
                ffmpeg.Dispose();       //Not sure if this is really needed
                ConvertVideo();
            });
            await converter;
        }

        private void ShowPercentComplete()
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
                initVideoDirectory = sResult;
            }
              
            return sResult;

        }

        private void ReloadComboBoxes(object sender, EventArgs e)
        {
            Load_ComboBoxes();
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
                if (combo_CatSubName.SelectedItem.ToString() == "Rhythm")
                {
                    pnl_VideoQuality.IsVisible = false;
                }
                else 
                {
                    pnl_VideoQuality.IsVisible = true; 
                }
            }
        }

        private void ShowErrorAfterLoad(object sender, EventArgs e)
        {
            if (myErrorsOnLoad != "")
            {
                textBlock_quotesLabel.Text = myErrorsOnLoad;
                /*
                var box = MessageBoxManager.GetMessageBoxStandard("Error", @"msg",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error,
                    WindowStartupLocation.CenterOwner);
                */
            }

        }

        private void SL_TimeChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() => UpdateTimerFromSlider(e), DispatcherPriority.Background);
        }
        
        private async Task UpdateTimerFromSlider(RangeBaseValueChangedEventArgs e)
        {
            _mp.Time = (long)e.NewValue;
            //_mp.Time =(long)slider_VideoSlider.Value;
        }

        private void MP_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() => UpdateTimeLabelsFromVideo(), DispatcherPriority.Background);
        }

        private async Task UpdateTimeLabelsFromVideo()
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

        }
        private async void DescriptionTextBox_TextChanged(object sender, EventArgs e)
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

            if (ListOfFiles.Count > 0)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Exiting", @"Videos still being converted: " + ListOfFiles.Count + "\nAre you sure you want to Exit",
                    MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question,
                    WindowStartupLocation.CenterOwner);

                e.Cancel = true;
            
                if ( await box.ShowWindowAsync() == MsBox.Avalonia.Enums.ButtonResult.Yes)
                {
                    _media?.Dispose();          //Checks if it's null and then runs Dispose
                    _mp?.Dispose();
                    _libVLC?.Dispose();
                    if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime) lifetime.Shutdown();
                }
            }

        }



        private void PercentCompleteTracker(object sender, EventArgs e)
        {
            if (percent > 0 || ListOfFiles.Count > 0)
            {
                lbl_progressLabel.IsVisible = true;
                string fileShow = Path.GetFileNameWithoutExtension(videoDataConverting.UploadPath);
                lbl_progressLabel.Text = percent.ToString() + "% (1 of " + ListOfFiles.Count + " ) " + fileShow;
            }
            if (lbl_progressLabel.Text.ToString().Contains("100") == true || ListOfFiles.Count == 0)
            {
                lbl_progressLabel.Text = "";
                lbl_progressLabel.IsVisible = false;
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
                PlayVideo(sResult);
            }

        }
        private void ClearButton_Click(object sender, EventArgs e)
        {
            ClearStuff();
        }
        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveButtonHelper();
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
            aboutDialog.Show();
        }

        public void Menu_CategoryNamesClick()
        {
            _categoriesWindow = new(configInfo, categoriesDic);
            _categoriesWindow.Closing += ReloadComboBoxes;
            _categoriesWindow.Show();
        }

        public void Menu_SettingsClick()
        {
            _settingsWindow = new(configInfo);
            _settingsWindow.Show();
        }
        public void Menu_LogoClick()
        {
            _logoWindow = new(configInfo, categoriesDic);
            _logoWindow.Show();
        }

    }

    //---------
    //---------Data Classes and DataStructures
    //---------

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

}