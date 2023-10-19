using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Media;
using MsBox.Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using LibVLCSharp.Shared;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using LibVLCSharp.Avalonia;
using System.Xml;
using Avalonia.Threading;
using System.Diagnostics.Metrics;


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
        List<VideoData> ListOfFiles = new();
        VideoData BillvideoData;				//Does NOT need to be Global
        VideoData videoDataConverting;			//Doesn't seem like it needs to be Global

        //----Video VLC variables
        public LibVLC _libVLC;
        public MediaPlayer _mp;
        public Media _media;
        private string initVideoDirectory;
        private string currentVideoPath;
        private VideoView _videoViewer;
        //public Size vlc_oldVideoSize;
        //public Size vlc_oldFormSize;
        //public Point vlc_oldVideoLocation;

        //----used in ReadConfig
        ConfigStruct configInfo;    //Structure to contain all config info
        Dictionary<string, string[]> categoriesDic;    //Variable for storing categories and sub categories

        //----Points and Timer variables
        readonly CountdownTimer timer = new();
        int counter = 0;    //Keeps track of the points
        int timeset = 10;   //Keeps track of the initial state of timer


        //-----End of Global Variables

        //
        //-----Main function
        //
        public MainWindow()
        {
            LukmaStartup();
                        
        }

        //---------
        //---------Helper functions
        //---------

        public void LukmaStartup()
        {
            InitializeComponent();

            //Screen related intializations
            int screenWidth = Screens.Primary.WorkingArea.Width;
            int screenHeight = Screens.Primary.WorkingArea.Height;
            lbl_ScreenRes.Content = "Monitor Resolution: " + screenWidth.ToString() + "x" + screenHeight.ToString();

            //Form intializations
            //this.FormClosing += Form1_FormClosing;

            //VLC initialization and View controls
            Core.Initialize();
            _libVLC = new LibVLC("--input-repeat=2");   //"--verbose=2"
            _mp = new MediaPlayer(_libVLC);
            //_videoViewer = this.Get<VideoView>("VideoViewer");
            //_mp.EndReached += MediaEndReached;
            VideoViewer.MediaPlayer = _mp;
            

            //Structure Intializations
            configInfo = new ConfigStruct();
            categoriesDic = new Dictionary<string, string[]> { };
            ReadConfig();
            ReadCategoryFiles();
            Load_ComboBoxes();
            InitializeButtonEventsLabels();

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

            using (StreamReader sr = new(sConfigFile))
            {
                while (!sr.EndOfStream)
                {
                    string sLine = sr.ReadLine().Trim();
                    string[] aLine = sLine.Split('=', (char)StringSplitOptions.RemoveEmptyEntries);
                    if (aLine.Length == 2)
                    {
                        string sParameter = aLine[0].Trim();
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
            
            Load_Category_ComboBox(categoriesDic);
            Load_VideoQuality_comboBox();
            
        }
        private void Load_Category_ComboBox(Dictionary<string, string[]> myDictionary)                                   //Populate categories ComboBox
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
        }

        private void PlayVideo(string file)
        {
            try
            {
                MediaPlayerPlayVideo(file);
                                
                //slider_VideoSlider

                //Buttons
                Dispatcher.UIThread.Post(() => UpdateVideoButtons(true), DispatcherPriority.Background);

            }
            catch (Exception ex)
            {
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
                slider_VideoSlider.IsEnabled = true;
                slider_VideoSlider.Minimum = 0;
                slider_VideoSlider.Value = 0;

                _mp.Play(_media);
                _mp.TimeChanged += MP_TimeChanged;
                _mp.Mute = true;
                _media?.Dispose();
            }
            else
            {
                Console.WriteLine("File not found: " + filename);
            }
        }

        private void MediaPlayerStopVideo()
        {
            if (_mp.IsPlaying)
            {
                _mp.Stop();
                _mp.TimeChanged -= MP_TimeChanged;
                slider_VideoSlider.Value = 0;
                slider_VideoSlider.IsEnabled = false;
                //_mp?.Dispose();       //This is causing the popout
                //_mp = new MediaPlayer(_libVLC);
                //VideoViewer.MediaPlayer = _mp;
                Dispatcher.UIThread.Post(() => UpdateVideoButtons(false), DispatcherPriority.Background);
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
            lbl_VideoFile.Content = "";
            RandomQuote();
        }

        private void ClearStuff()
        {
            UpdateVideoPath("");
            combo_CategoryComboBox.SelectedIndex = -1;
            combo_CatSubName.SelectedIndex = -1;
            txtb_Description.Clear();
            btn_Save.IsEnabled = false;
            
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

        }

        //updates the global variable and updates the view label
        private void UpdateVideoPath(string path)
        {
            currentVideoPath = path;
            lbl_VideoFile.Content = path;
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

            List<string> extension = new() { "MP4", "mp4" };
            FileDialogFilter myFilter = new()
            {
                Extensions = extension
            };

            List<FileDialogFilter> myFilters = new()
            {
                myFilter
            };
            OpenFileDialog fileDlg = new()
            {
                Title = "Select video file",
                AllowMultiple = false,
                Directory = initVideoDirectory,
                Filters = myFilters,
            };
            string[] result = await fileDlg.ShowAsync(this);
            string sResult = "";
            if (result.Length>0)
            {
                sResult= result[0];
                initVideoDirectory = Path.GetDirectoryName(sResult);
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
         
        private void MP_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() => UpdateVideoTime(), DispatcherPriority.Background);
        }
        private async Task UpdateVideoTime()
        {
            TimeSpan currentTime = TimeSpan.FromMilliseconds(_mp.Time);
            TimeSpan endTime = TimeSpan.FromMilliseconds(_mp.Length);
            
            lbl_VideoCurrentTime.Content = currentTime.ToString(@"mm\:ss");
            lbl_VideoEndTime.Content = endTime.ToString(@"mm\:ss");
            
            //Update Slider bar
            slider_VideoSlider.IsEnabled = true;
            slider_VideoSlider.Maximum = _mp.Length;
            slider_VideoSlider.Value = _mp.Time;
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
        private void AppClosing(object sender, EventArgs e)
        {
            // Display a MsgBox asking the user if they really want to exit.
            if (ListOfFiles.Count > 0)
            {

                MessageBoxManager.GetMessageBoxStandard("Warning", "That Already Exists",
                    MsBox.Avalonia.Enums.ButtonEnum.OkCancel, MsBox.Avalonia.Enums.Icon.Warning,
                    WindowStartupLocation.CenterOwner).ShowWindowDialogAsync(this);

                /*if (MessageBox.Show(@"Videos still being converted: " + ListOfFiles.Count + "\nAre you sure you want to Exit", "Lukma",
                   MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    // Cancel the Closing event from closing the form.
                    e.Cancel = true;
                }
                else
                {
                    _media?.Dispose();          //Checks if it's null and then runs Dispose
                    _mp?.Dispose();
                    _libVLC?.Dispose();
                }*/
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




        }

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
            if(_categoriesWindow == null)
            {
                _categoriesWindow = new(configInfo, categoriesDic);
                _categoriesWindow.Closing += ReloadComboBoxes;
            }
            _categoriesWindow.Show();
        }

        public void Menu_SettingsClick()
        {
            _settingsWindow = new(configInfo);
            _settingsWindow.Show();
        }
        public void Menu_LogoClick()
        {
            _logoWindow = new();
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