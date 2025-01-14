using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace xPlatformLukma;

public partial class LogoWindow : Window
{
    readonly SortedDictionary<string, string[]> myCategoryDic;
    ConfigStruct myConfigInfo;
    string initLogoDirectory;
    Utils newUtil;


    public LogoWindow()
    {
        InitializeComponent();
    }

    public LogoWindow(ConfigStruct passConfigInfo, SortedDictionary<string, string[]> passCategoryDic)
    {
        InitializeComponent();
        myCategoryDic = passCategoryDic;
        myConfigInfo = passConfigInfo;
        //string logoConfigFile = System.IO.Path.Combine(myConfigInfo.configDir, myConfigInfo.customLogoFile);
        newUtil = new Utils();
        //newUtil.ReadCustomLogos(myConfigInfo);

        btn_Search.IsEnabled = false;
        comboBox_Name.IsEnabled = false;
        btn_Apply.IsEnabled = false;
        btn_Remove.IsEnabled = false;
        
        InitializeEvents();
        rb_Primary.IsChecked = true;


    }

    //
    //---------Helper functions
    //
    
    //Reads the custom logos file and updates local structure
    
    

    //Checks to see if the custom logo is already defined, updates or adds it and then resets the images
    private void CheckAndAddNewLogo(string sCat, string logoLocation)
    {
        if (myConfigInfo.customLogos.ContainsKey(sCat))
        {
            myConfigInfo.customLogos[sCat] = logoLocation;
            newUtil.ReWriteCustomLogosFile(myConfigInfo);
        }
        else
        {
            myConfigInfo.customLogos.Add(sCat, logoLocation);
            newUtil.AppendCustomLogosFile(myConfigInfo, sCat, logoLocation);
        }
        SetImage(image_CurrentLogo, logoLocation);
        lbl_additionalInfo.Text = logoLocation;

        ClearNewLogoBox();
    }

    public void Load_ComboBox(string sComboName)
    {
        comboBox_Name.Items.Clear();
        if (myCategoryDic.ContainsKey(sComboName))
        {
            string[] tmpString = myCategoryDic[sComboName];
            foreach (string str in tmpString)
            {
                comboBox_Name.Items.Add(str);
            }
        }
    }

    private void ClearCurrentLogoBox()
    {
        SetImage(image_CurrentLogo, null);
    }
    
    private void ClearNewLogoBox()
    {
        SetImage(image_NewLogo, null);
    }

    private void InitializeEvents()
    {

        btn_Close.Click += CloseButton_Click;
        btn_Search.Click += SearchButton_Click;
        btn_Apply.Click += ApplyButton_Click;
        btn_Remove.Click += RemoveButton_Click;

        comboBox_Name.SelectionChanged += NameComboBox_SelectedIndexChange;
        rb_Primary.IsCheckedChanged += RBPrimaryLogo_CheckedChanged;
        rb_TeamsPrivate.IsCheckedChanged += RBTeam_CheckedChanged;
        rb_Teams.IsCheckedChanged += RBTeam_CheckedChanged;
        rb_Category.IsCheckedChanged += RBCategory_CheckedChanged;
       

    }


    //
    //---------Helper Events
    //
#nullable enable
    private void SetImage(Image? imageControl, string? sImageName)
    {
        Bitmap? img = null;
        if (sImageName != null)
        {
            img = new Bitmap(sImageName);
        }
        if (imageControl != null) 
        {
            imageControl.Source = img;
        }
    }
#nullable disable
    private void NameComboBox_SelectedIndexChange(object sender, EventArgs e)
    {
        if(comboBox_Name.SelectedValue != null)
        {
            string sComboName = comboBox_Name.SelectedValue.ToString();
            ClearCurrentLogoBox();
            lbl_additionalInfo.Text = "Applies to only individual teams";
            if (myConfigInfo.customLogos.ContainsKey(sComboName))
            {
                string curFile = myConfigInfo.customLogos[sComboName].ToString();
                if (File.Exists(curFile))
                {
                    lbl_additionalInfo.Text = curFile;
                    SetImage(image_CurrentLogo, curFile);

                }
            }
            Toggle_SearchButton(true, new EventArgs());
            //Enable_SearchButton(this, new EventArgs());
            Toggle_ApplyButton(this, new EventArgs());
            Toggle_RemoveButton(this, new EventArgs());
        }
        else
        {
            Toggle_SearchButton(false, new EventArgs());
        }

    }
    
    private void RBCategory_CheckedChanged(object sender, EventArgs e)
    {
        if (rb_Category.IsChecked == true)
        {
            //string comboName = "Category";
            ClearCurrentLogoBox();
            ClearNewLogoBox();
            Toggle_ApplyButton(this, new EventArgs());
            lbl_additionalInfo.Text = "Applies to all Categories except Teams/Private Teams";
            string catName = "Category";
            comboBox_Name.Items.Clear();
            comboBox_Name.IsEnabled = false;
            if (myConfigInfo.customLogos.ContainsKey(catName))
            {
                string tmpString = myConfigInfo.customLogos[catName];
                SetImage(image_CurrentLogo, tmpString);
            }
            Toggle_SearchButton(true, new EventArgs());
            //Enable_SearchButton(this, new EventArgs());
            Toggle_ApplyButton(this, new EventArgs());
            Toggle_RemoveButton(this, new EventArgs());

        }

    }
    private void RBTeam_CheckedChanged(object sender, EventArgs e)
    {

        comboBox_Name.SelectedValue = null;
        
        if (rb_Teams.IsChecked == true || rb_TeamsPrivate.IsChecked == true)
        {
            comboBox_Name.IsEnabled = true;
            string comboName = "Teams";
            ClearCurrentLogoBox();
            ClearNewLogoBox();
            Toggle_ApplyButton(this, new EventArgs());

            if (rb_TeamsPrivate.IsChecked == true)
            {
                comboName = "Teams - Private";
            }
            lbl_additionalInfo.Text = "Applies to only individual teams";
            Load_ComboBox(comboName);
            Toggle_SearchButton(false, new EventArgs());
            //Enable_SearchButton(this, new EventArgs());

        }
        else
        {
            comboBox_Name.IsEnabled = false;
        }

    }

    private void RBPrimaryLogo_CheckedChanged(object sender, EventArgs e)
    {
        if (rb_Primary.IsChecked == true)
        {
            //comboBox_Name.SelectedValue = null;
            comboBox_Name.Items.Clear();
            comboBox_Name.IsEnabled = false;
            ClearCurrentLogoBox();
            ClearNewLogoBox();
            lbl_additionalInfo.Text = "Applies to ALL videos";
            
            Toggle_SearchButton(true, new EventArgs());
            //Toggle_ApplyButton(this, new EventArgs());
            //Toggle_RemoveButton(this, new EventArgs());

            string catName = "Primary";
            string tmpString="";
            if (myConfigInfo.customLogos.ContainsKey(catName))
            {
                tmpString = myConfigInfo.customLogos[catName];
            }
            //else
            //{
            //    tmpString = System.IO.Path.Combine(myConfigInfo.logoDir, "SDCLogo_1080.png");
            //    CheckAndAddNewLogo(catName, tmpString);
            //}
            if(tmpString != "")
            {
                if (File.Exists(tmpString))
                {
                    lbl_additionalInfo.Text = tmpString;
                    SetImage(image_CurrentLogo, tmpString);
                    //Enable_SearchButton(this, new EventArgs());
                    
                }
                else
                {
                    string errorMessage = "Re-add logo for " + catName + ". File not found: " + tmpString;
                    //lbl_additionalInfo.Text = "Re-add logo for " + catName + ". File not found: " + tmpString;
                    MessageBoxManager.GetMessageBoxStandard("Warning", errorMessage,
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning,
                    WindowStartupLocation.CenterOwner).ShowWindowDialogAsync(this);
                }
            }
            Toggle_ApplyButton(this, new EventArgs());
            Toggle_RemoveButton(this, new EventArgs());


        }
    }
        
    private void Toggle_ApplyButton(object sender, EventArgs e) 
    {
        if (image_NewLogo.Source != null)
        {
            btn_Apply.IsEnabled = true;
        }
        else { btn_Apply.IsEnabled = false; }
    }
   
    private void Toggle_RemoveButton(object sender, EventArgs e) 
    {
        if (image_CurrentLogo.Source != null)
        {
            btn_Remove.IsEnabled = true;
        }
        else { btn_Remove.IsEnabled = false; }
    }


    private void Toggle_SearchButton(bool bValue, EventArgs e)
    {
        btn_Search.IsEnabled = bValue;
    }
    private void Enable_SearchButton(object sender, EventArgs e) 
    {

        if ( (rb_Primary.IsChecked == true || rb_Category.IsChecked == true ) ||
            comboBox_Name.SelectedItem != null)
        {
            btn_Search.IsEnabled = true;
        }
        else 
        { 
            btn_Search.IsEnabled = false;
        }
    }

    private async Task<string> ReturnSeachFile()
    {
        
        if (initLogoDirectory == null || !Directory.Exists(initLogoDirectory))
        {
            initLogoDirectory = myConfigInfo.logoDir;
        }

        // Get top level from the current control
        var topLevel = TopLevel.GetTopLevel(this);
        var folderPathToStart = await topLevel.StorageProvider.TryGetFolderFromPathAsync(initLogoDirectory);
        // Setting options            
        var options = new FilePickerOpenOptions
        {
            Title = "Select video file",
            AllowMultiple = false,
            SuggestedStartLocation = folderPathToStart,
            FileTypeFilter = new FilePickerFileType[] { new("PNG file") { Patterns = new[] { "*.png", "*.PNG" } } }
        };

        // Start async operation to open the dialog.
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        string sResult = "";
        if (files.Count > 0)
        {
            sResult = files[0].TryGetLocalPath();
            initLogoDirectory = System.IO.Path.GetDirectoryName(sResult);        }


        return sResult;
    }

    //
    //---------Button Click Events
    //

    private void CloseButton_Click(object sender, EventArgs e)
    {
        this.Close();
    }


    private async void SearchButton_Click(object sender, EventArgs e)        //Logo File Selection Button
    {
        string sResult = await ReturnSeachFile();
        if (sResult != "")
        {
            lbl_additionalInfo.Text = sResult;
            SetImage(image_NewLogo, sResult);
            Toggle_ApplyButton(this, new EventArgs());

        }
    }

    private void ApplyButton_Click(object sender, EventArgs e)
    {
        string sComboName;
        string imageLocation = lbl_additionalInfo.Text.ToString();
        if (File.Exists(imageLocation) )
        {
            if ((bool)rb_Primary.IsChecked)
            {
                sComboName = "Primary";
                CheckAndAddNewLogo(sComboName, imageLocation);
            }
            else if ((bool)rb_Category.IsChecked)
            {
                sComboName = "Category";
                CheckAndAddNewLogo(sComboName, imageLocation);
            }
            else if ((bool)rb_TeamsPrivate.IsChecked || (bool)rb_Teams.IsChecked)
            {
                sComboName = comboBox_Name.SelectedValue.ToString();
                if (sComboName.Length > 0 && imageLocation != null)
                {
                    CheckAndAddNewLogo(sComboName, imageLocation);
                }
            }
            lbl_additionalInfo.Text = imageLocation;
            SetImage(image_CurrentLogo, imageLocation);
            ClearNewLogoBox();
            Toggle_ApplyButton(this, new EventArgs());
            Toggle_RemoveButton(this, new EventArgs());
        }
        else
        {
            Toggle_ApplyButton(this, new EventArgs());
            Toggle_RemoveButton(this, new EventArgs());
            string tmpMessage = "File not found";
            MessageBoxManager.GetMessageBoxStandard("Warning", tmpMessage,
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning,
                    WindowStartupLocation.CenterOwner).ShowWindowDialogAsync(this);
            
        }
    }

    private void RemoveButton_Click(object sender, EventArgs e)
    {
        string sComboName;
        if ((bool)rb_Primary.IsChecked)
        {
            sComboName = "Primary";
            if (myConfigInfo.customLogos.ContainsKey(sComboName))
            {
                myConfigInfo.customLogos.Remove(sComboName);
                newUtil.ReWriteCustomLogosFile(myConfigInfo);
            }
            ClearCurrentLogoBox();
            Toggle_ApplyButton(this, new EventArgs());
            Toggle_RemoveButton(this, new EventArgs());

        }
        else if ((bool)rb_Category.IsChecked)
        {
            sComboName = "Category";
            if (myConfigInfo.customLogos.ContainsKey(sComboName))
            {
                myConfigInfo.customLogos.Remove(sComboName);
                newUtil.ReWriteCustomLogosFile(myConfigInfo);
            }
            ClearCurrentLogoBox();
            Toggle_ApplyButton(this, new EventArgs());
            Toggle_RemoveButton(this, new EventArgs());

        }
        else if ((bool)rb_TeamsPrivate.IsChecked || (bool)rb_Teams.IsChecked)
        {
            sComboName = comboBox_Name.SelectedValue.ToString();
            if (sComboName.Length > 0 && image_CurrentLogo.Source != null)
            {
                if (myConfigInfo.customLogos.ContainsKey(sComboName))
                {
                    myConfigInfo.customLogos.Remove(sComboName);
                    newUtil.ReWriteCustomLogosFile(myConfigInfo);
                }
                ClearCurrentLogoBox();
                lbl_additionalInfo.Text = "Applies to only individual teams";
                Toggle_ApplyButton(this, new EventArgs());
                Toggle_RemoveButton(this, new EventArgs());
            }
        }

    }



}