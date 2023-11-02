using Avalonia;
using Avalonia.Controls;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace xPlatformLukma;

public partial class SettingsWindow : Window
{
    ConfigStruct myConfigInfo;
    public SettingsWindow()
    {
        InitializeComponent();
    }
    public SettingsWindow(ConfigStruct configInfo)
        : this()
    {
        myConfigInfo = configInfo;
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
        var folderDlg = new OpenFolderDialog()
        {
            Directory = tmpDir,
            Title = "Select output directory",
        };
        return await folderDlg.ShowAsync(this);
        
    }


    private void UpdateConfigFile(string oldline, string updatedLine)
    {
        string CatPath = Path.Combine(myConfigInfo.configDir, "config.txt");
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

    }//End of private function

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
        lbl_UnconvertedVideoDir.Content = unconvertVideo;
        myConfigInfo.unconvertedVideoDir = unconvertVideo;

        //write change to
        UpdateConfigFile("localVideoDir", unconvertVideo);

    }

    private async void ConvertedSearchButton_Click(object sender, EventArgs e)
    {
        string convertVideo = await ReturnSeachDirectory();
        lbl_ConvertedVideoDir.Content = convertVideo;
        myConfigInfo.convertedVideosTopDir = convertVideo;

        //write change to file
        UpdateConfigFile("convertedVideosTopDir", convertVideo);
    }

    private void CloseButton_Click(object sender, EventArgs e)
    {
        this.Close();
    }

}