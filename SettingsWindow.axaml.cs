using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace xPlatformLukma;

public partial class SettingsWindow : Window
{
    public ConfigStruct myConfigInfo;
    Utils newUtil;
    public SettingsWindow()
    {
        InitializeComponent();
    }
    public SettingsWindow(ConfigStruct configInfo)
        : this()
    {
        myConfigInfo = configInfo;
        newUtil = new Utils();
        Load_Bitrate_ComboBox();
        LoadVideoDirectories();
        Load_CleanupDays_ComboBox();
        InitializeEvents();
    }

    //
    //---------Helper functions
    //

    private void LoadVideoDirectories()
    {
        lbl_UnconvertedVideoDir.Content = myConfigInfo.unconvertedVideoDir;
        lbl_ConvertedVideoDir.Content = myConfigInfo.convertedVideosTopDir;
    }

    private async Task<string> ReturnSeachDirectory()
    {
        string tmpDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        // Get top level from the current control
        var topLevel = TopLevel.GetTopLevel(this);
        var folderPathToStart = await topLevel.StorageProvider.TryGetFolderFromPathAsync(tmpDir);

        var options = new FolderPickerOpenOptions
        {
            Title = "Select output directory",
            AllowMultiple = false,
            SuggestedStartLocation = folderPathToStart
        };

        var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        string sResult = "";
        if (folder.Count > 0)
        {
            sResult = folder[0].Path.LocalPath;
        }

        return sResult;
    }


    //End of private function

    //
    //---------Helper Events
    //
    private void InitializeEvents()
    {
        btn_UnconvertedVideoDir.Click += UnconvertedSearchButton_Click;
        btn_ConvertedVideoDir.Click += ConvertedSearchButton_Click;
        btn_Close.Click += CloseButton_Click;
        combo_VideoBitrate.SelectionChanged += BitrateCombo_SelectedIndexChanged;
        combo_CleanupDays.SelectionChanged += CleanupDaysCombo_SelectedIndexChanged;
        btn_Cleanup.Click += CleanupNow_Click;
    }

    private void Load_Bitrate_ComboBox()
    {
        
        combo_VideoBitrate.Items.Clear();
        combo_VideoBitrate.Items.Add("Low");
        combo_VideoBitrate.Items.Add("High");
        if (myConfigInfo.bitRate < 0 || myConfigInfo.bitRate > 1)
        {
            combo_VideoBitrate.SelectedIndex = 0;
            myConfigInfo.bitRate = 0;
        }
        else
        {
            combo_VideoBitrate.SelectedIndex = myConfigInfo.bitRate;
        }

    }

    private void Load_CleanupDays_ComboBox()
    {
        combo_CleanupDays.Items.Clear();
        combo_CleanupDays.Items.Add("Never");
        combo_CleanupDays.Items.Add("30");
        combo_CleanupDays.Items.Add("60");
        combo_CleanupDays.Items.Add("90");
        
        switch (myConfigInfo.cleanupAfterDays)
        {
            case 0:
                combo_CleanupDays.SelectedIndex = 0;
                break;

            case 30:
                combo_CleanupDays.SelectedIndex = 1;
                break;

            case 60:
                combo_CleanupDays.SelectedIndex = 2;
                break;

            case 90:
                combo_CleanupDays.SelectedIndex = 3;
                break;
            default:
                combo_CleanupDays.SelectedIndex = 0;
                break;
        }
        
    }
    //
    //---------Button Click Events
    //
    private void BitrateCombo_SelectedIndexChanged(object sender, EventArgs e)
    {
        int selectedIndex = combo_VideoBitrate.SelectedIndex;
        myConfigInfo.bitRate = selectedIndex;
        //write change to file
        newUtil.UpdateConfigFile(myConfigInfo, "bitrate", selectedIndex.ToString());

    }

    private void CleanupDaysCombo_SelectedIndexChanged(object sender, EventArgs e)
    {
        string selectedItem = combo_CleanupDays.SelectedItem.ToString();
        switch (selectedItem)
        {
            case "Never":
                myConfigInfo.cleanupAfterDays = 0;
                break;

            case "30":
                myConfigInfo.cleanupAfterDays = 30;
                break;

            case "60":
                myConfigInfo.cleanupAfterDays = 60;
                break;

            case "90":
                myConfigInfo.cleanupAfterDays = 90;
                break;
            default:
                myConfigInfo.cleanupAfterDays = 0;
                break;
        }
        newUtil.UpdateConfigFile(myConfigInfo, "cleanupAfterDays", myConfigInfo.cleanupAfterDays.ToString());
    }

    private async void UnconvertedSearchButton_Click(object sender, EventArgs e)
    {
        string unconvertVideo = await ReturnSeachDirectory();
        if (unconvertVideo !="")
        {
            lbl_UnconvertedVideoDir.Content = unconvertVideo;
            myConfigInfo.unconvertedVideoDir = unconvertVideo;

            //write change to file
            newUtil.UpdateConfigFile(myConfigInfo, "localVideoDir", unconvertVideo);

        }
    }

    private async void ConvertedSearchButton_Click(object sender, EventArgs e)
    {
        string convertVideo = await ReturnSeachDirectory();
        if( convertVideo != ""){
            lbl_ConvertedVideoDir.Content = convertVideo;
            myConfigInfo.convertedVideosTopDir = convertVideo;

            //write change to file
            newUtil.UpdateConfigFile(myConfigInfo, "convertedVideosTopDir", convertVideo);
        }
    }

    private async void CleanupNow_Click(object sender, EventArgs e)
    {
        if(myConfigInfo.cleanupAfterDays >0)
        {
            FolderCleanup cleanup = new(myConfigInfo.cleanupAfterDays,myConfigInfo.unconvertedVideoDir);
            string cleanupErrors = cleanup.CleanUpFolder();
            if (cleanupErrors != "")
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", cleanupErrors,
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error,
                    WindowStartupLocation.CenterOwner);
                await box.ShowWindowAsync();
            }

        }
    }

    private void CloseButton_Click(object sender, EventArgs e)
    {
        this.Close();
    }

}