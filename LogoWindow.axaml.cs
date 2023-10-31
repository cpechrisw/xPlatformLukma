using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace xPlatformLukma;

public partial class LogoWindow : Window
{
    readonly Dictionary<string, string[]> myCategoryDic;
    ConfigStruct myConfigInfo;
    string initLogoDirectory;

    public LogoWindow()
    {
        InitializeComponent();
    }

    public LogoWindow(ConfigStruct passConfigInfo, Dictionary<string, string[]> passCategoryDic)
    {
        InitializeComponent();
        myCategoryDic = passCategoryDic;
        myConfigInfo = passConfigInfo;
        string logoConfigFile = System.IO.Path.Combine(myConfigInfo.configDir, myConfigInfo.customLogoFile);
        ReadCustomLogos(logoConfigFile, myConfigInfo);

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
    public void ReadCustomLogos(string sConfigFile, ConfigStruct myConfigInfo)
    {

        if (File.Exists(sConfigFile))
        {
            using (StreamReader sr = new(sConfigFile))
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
                        if (!myConfigInfo.customLogos.ContainsKey(sParameter))
                        {
                            myConfigInfo.customLogos.Add(sParameter, sValue);
                        }
                    }
                }
            }
        }
        else
        {
            //If there is no file, add the default primary logo
            string tmpString = System.IO.Path.Combine(myConfigInfo.logoDir, "logo_1080.png");
            if (File.Exists(tmpString))
            {
                myConfigInfo.customLogos.Add("Primary", tmpString);
                ReWriteCustomLogosFile();

            }

        }

    }

    private void ReWriteCustomLogosFile()
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

    private void AppendCustomLogosFile(string cat, string logoLocation)
    {
        string filePath = System.IO.Path.Combine(myConfigInfo.configDir, myConfigInfo.customLogoFile);
        string addedString = cat + "=" + logoLocation;
        if (!File.Exists(filePath)) File.Create(filePath).Close();

        if (new FileInfo(filePath).Length != 0)
        {
            File.AppendAllText(filePath, Environment.NewLine);
        }
        File.AppendAllText(filePath, addedString);

    }

    //-------------NEEDS TO BE UPDATED--------------
    //Checks to see if the custom logo is already defined, updates or adds it and then resets the images
    private void CheckAndAddNewLogo(string sCat, string logoLocation)
    {
        if (myConfigInfo.customLogos.ContainsKey(sCat))
        {
            myConfigInfo.customLogos[sCat] = logoLocation;
            ReWriteCustomLogosFile();
        }
        else
        {
            myConfigInfo.customLogos.Add(sCat, logoLocation);
            AppendCustomLogosFile(sCat, logoLocation);
        }
        //currentLogoBox.Image = null;
        //currentLogoBox.ImageLocation = logoLocation;
        lbl_additionalInfo.Content = logoLocation;

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

    //-------------NEEDS TO BE UPDATED--------------
    private void ClearCurrentLogoBox()
    {
        //currentLogoBox.Image = null;
        //currentLogoBox.ImageLocation = null;

    }
    //-------------NEEDS TO BE UPDATED--------------
    private void ClearNewLogoBox()
    {
        /*newLogoBox.Paint -= new PaintEventHandler(this.NewLogoBox_PaintError);
        newLogoBox.Image = null;
        newLogoBox.ImageLocation = null;*/
    }


    //
    //---------Helper Events
    //
    private void InitializeEvents()
    {
       
        btn_Close.Click += CloseButton_Click;
        btn_Search.Click += SearchButton_Click;
        btn_Apply.Click += ApplyButton_Click;
        btn_Remove.Click += RemoveButton_Click;
        
        comboBox_Name.SelectionChanged += NameComboBox_SelectedIndexChange;
        rb_Primary.IsCheckedChanged += RBPrimaryLogo_CheckedChanged;
        rb_TeamsPrivate.IsCheckedChanged += RBPrivateTeam_CheckedChanged;
        rb_Category.IsCheckedChanged += RBCategory_CheckedChanged;
        rb_Teams.IsCheckedChanged += RBTeam_CheckedChanged;
    }
    
    //-------------NEEDS TO BE UPDATED--------------
    private void NameComboBox_SelectedIndexChange(object sender, EventArgs e)
    {
        string sComboName = comboBox_Name.SelectedValue.ToString();
        /*currentLogoBox.Image = null;
        currentLogoBox.ImageLocation = null;*/
        lbl_additionalInfo.Content = "Applies to only individual teams";
        if (myConfigInfo.customLogos.ContainsKey(sComboName))
        {
            string curFile = myConfigInfo.customLogos[sComboName].ToString();
            if (File.Exists(curFile))
            {
                lbl_additionalInfo.Content = curFile;
                /*currentLogoBox.ImageLocation = curFile;*/
            }
        }
        Enable_SearchButton(this, new EventArgs());
        Enable_ApplyButton(this, new EventArgs());
        Enable_RemoveButton(this, new EventArgs());

    }

    private void RBTeam_CheckedChanged(object sender, EventArgs e)
    {
        comboBox_Name.IsEnabled = true;
        comboBox_Name.SelectedValue = null;
        if (rb_Teams.IsChecked == true)
        {
            ClearCurrentLogoBox();
            string comboName = "Teams";
            lbl_additionalInfo.Content = "Applies to only individual teams";
            Load_ComboBox(comboName);
        }

    }
    //-------------NEEDS TO BE UPDATED--------------
    private void RBCategory_CheckedChanged(object sender, EventArgs e)
    {
        comboBox_Name.IsEnabled = false;
        comboBox_Name.SelectedValue = null;
        if (rb_Category.IsChecked == true)
        {
            //string comboName = "Category";
            ClearCurrentLogoBox();
            lbl_additionalInfo.Content = "Applies to all Categories except Teams/Private Teams";
            string catName = "Category";
            comboBox_Name.Items.Clear();
            if (myConfigInfo.customLogos.ContainsKey(catName))
            {
                string tmpString = myConfigInfo.customLogos[catName];
                /*currentLogoBox.Image = null;
                currentLogoBox.ImageLocation = tmpString;*/
            }
            Enable_SearchButton(this, new EventArgs());
            Enable_ApplyButton(this, new EventArgs());
            Enable_RemoveButton(this, new EventArgs());

        }


    }
    private void RBPrivateTeam_CheckedChanged(object sender, EventArgs e)
    {
        comboBox_Name.IsEnabled = true;
        comboBox_Name.SelectedValue = null;
        if (rb_TeamsPrivate.IsChecked == true)
        {
            ClearCurrentLogoBox();
            string comboName = "Teams - Private";
            lbl_additionalInfo.Content = "Applies to only individual teams";
            Load_ComboBox(comboName);
        }
    }

    private void RBPrimaryLogo_CheckedChanged(object sender, EventArgs e)
    {
        comboBox_Name.IsEnabled = false;
        comboBox_Name.SelectedValue = null;
        if (rb_Primary.IsChecked == true)
        {
            lbl_additionalInfo.Content = "Applies to ALL videos";
            string catName = "Primary";
            comboBox_Name.Items.Clear();
            string tmpString;
            if (myConfigInfo.customLogos.ContainsKey(catName))
            {
                tmpString = myConfigInfo.customLogos[catName];
                /*currentLogoBox.Image = null;
                currentLogoBox.ImageLocation = tmpString;*/
            }
            else
            {
                tmpString = System.IO.Path.Combine(myConfigInfo.logoDir, "logo_1080.png");
                CheckAndAddNewLogo(catName, tmpString);
            }
            lbl_additionalInfo.Content = tmpString;
            Enable_SearchButton(this, new EventArgs());
            Enable_ApplyButton(this, new EventArgs());
        }
    }

    //Redo this and throw an error message box
    private void NewLogoBox_PaintError(object sender, EventArgs e)
    {

        
    }
    //-------------NEEDS TO BE UPDATED--------------
    private void Enable_ApplyButton(object sender, EventArgs e) 
    {
        //if (newLogoBox.ImageLocation != null)
        if(lbl_additionalInfo.Content.ToString().Contains(".png"))
        {
            btn_Apply.IsEnabled = true;
        }
        else { btn_Apply.IsEnabled = false; }
    }
    //-------------NEEDS TO BE UPDATED--------------
    private void Enable_RemoveButton(object sender, EventArgs e) 
    {
        //if (currentLogoBox.ImageLocation != null)
        if (lbl_additionalInfo.Content.ToString().Contains(".png"))
        {
            btn_Remove.IsEnabled = true;
        }
        else { btn_Remove.IsEnabled = false; }
    }
    private void Enable_SearchButton(object sender, EventArgs e) 
    {

        if (rb_Primary.IsChecked == true || rb_Category.IsChecked == true ||
            comboBox_Name.SelectedItem != null)
        {
            btn_Search.IsEnabled = true;
        }
        else { btn_Search.IsEnabled = false; }
    }

    private async Task<string> ReturnSeachFile()
    {
        if (initLogoDirectory == null || !Directory.Exists(initLogoDirectory))
        {
            initLogoDirectory = myConfigInfo.logoDir;
        }
        List<string> extension = new() { "png", "PNG" };
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
            Title = "Select png file",
            AllowMultiple = false,
            Filters = myFilters,
        };
        string[] result = await fileDlg.ShowAsync(this);
        string sResult = "";
        if (result.Length > 0)
        {
            sResult = result[0];
            initLogoDirectory = System.IO.Path.GetDirectoryName(sResult);
        }

        return sResult;
    }

    //
    //---------Button Click Events
    //

    private void CloseButton_Click(object sender, EventArgs e)
    {
        this.Close();
    }


    //-------------NEEDS TO BE UPDATED TO SHOW IMAGE SELECTED--------------
    private async void SearchButton_Click(object sender, EventArgs e)        //Logo File Selection Button
    {
        string sResult = await ReturnSeachFile();
        if (sResult != "")
        {
            lbl_additionalInfo.Content = sResult;
            //Apply new logo
            Enable_ApplyButton(this, new EventArgs());

        }
    }

    //-------------NEEDS TO BE UPDATED--------------
    private void ApplyButton_Click(object sender, EventArgs e)
    {
        string sComboName;
        string imageLocation = lbl_additionalInfo.Content.ToString();//newLogoBox.ImageLocation;

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
        lbl_additionalInfo.Content = imageLocation;
        Enable_ApplyButton(this, new EventArgs());
        Enable_RemoveButton(this, new EventArgs());

    }

    //-------------NEEDS TO BE UPDATED--------------
    private void RemoveButton_Click(object sender, EventArgs e)
    {
        string sComboName;
        if ((bool)rb_Primary.IsChecked)
        {
            //sComboName = "Primary";
            lbl_additionalInfo.Content = "Can't remove Primary Logo";

        }
        else if ((bool)rb_Category.IsChecked)
        {
            sComboName = "Category";
            if (myConfigInfo.customLogos.ContainsKey(sComboName))
            {
                myConfigInfo.customLogos.Remove(sComboName);
                ReWriteCustomLogosFile();
            }

            /*currentLogoBox.Image = null;
            currentLogoBox.ImageLocation = null;*/
            Enable_ApplyButton(this, new EventArgs());
            Enable_RemoveButton(this, new EventArgs());

        }
        else if ((bool)rb_TeamsPrivate.IsChecked || (bool)rb_Teams.IsChecked)
        {
            sComboName = comboBox_Name.SelectedValue.ToString();
            if (sComboName.Length > 0 /*&& currentLogoBox.ImageLocation != null*/)
            {
                if (myConfigInfo.customLogos.ContainsKey(sComboName))
                {
                    myConfigInfo.customLogos.Remove(sComboName);
                    ReWriteCustomLogosFile();
                }

                /*currentLogoBox.Image = null;
                currentLogoBox.ImageLocation = null;*/
                lbl_additionalInfo.Content = "Applies to only individual teams";
                Enable_ApplyButton(this, new EventArgs());
                Enable_RemoveButton(this, new EventArgs());
            }
        }

    }



}