using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace xPlatformLukma;

public partial class SettingsWindow : Window
{
    ConfigStruct myConfigInfo;
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
        ReadConfig();
        InitializeEvents();
    }

    //btn_UnconvertedVideoDir
    //lbl_UnconvertedVideoDir
    //
    //btn_ConvertedVideoDir
    //lbl_ConvertedVideoDir
    //
    //btn_Close

    //
    //---------Helper functions
    //

    private void ReadConfig()
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

    }

    //
    //---------Button Click Events
    //

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

    private void CloseButton_Click(object sender, EventArgs e)
    {
        this.Close();
    }

}