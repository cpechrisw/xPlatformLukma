using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using MsBox.Avalonia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace xPlatformLukma;

public partial class CategoriesWindow : Window
{
    ConfigStruct myConfigInfo;
    Dictionary<string, string[]> myCategoriesDic;

    public CategoriesWindow()
    { 
        InitializeComponent();
    }
    public CategoriesWindow(ConfigStruct configInfo, Dictionary<string, string[]> categoriesDic)
        :this()
    {
        myConfigInfo = configInfo;
        myCategoriesDic = categoriesDic;
        InitializeBtns();
        LoadCategoryBox();
        InitializeEvents();

    }
    //lsbox_Categories
    //txtBox_NewCategory
    //btn_CatAdd
    //btn_CatRemove
    //
    //lsbox_Name
    //txtBox_NewName
    //btn_NameAdd
    //btn_NameRemove
    //
    //bt_close

    //
    //---------Helper functions
    //
    private void InitializeBtns()
    {
        txtBox_NewCategory.IsEnabled = true;
        btn_CatAdd.IsEnabled = false;
        btn_CatRemove.IsEnabled = false;

        txtBox_NewName.IsEnabled = false;
        btn_NameAdd.IsEnabled = false;
        btn_NameRemove.IsEnabled = false;
        bt_close.IsEnabled = true;
    }
    //Loads the Category Box
    private void LoadCategoryBox()
    {
        lsbox_Categories.Items.Clear();
        for (int i = 0; i < myCategoriesDic.Count; i++)
        {
            lsbox_Categories.Items.Add(myCategoriesDic.ElementAt(i).Key);

        }
    }

    //Initializes the events that need to be added to the various boxes/buttons
    private void InitializeEvents()
    {
        //Intializing Events

        bt_close.Click += CloseButton_Click;

        //Categories boxes and buttons
        lsbox_Categories.SelectionChanged += Load_Names_Box;
        lsbox_Categories.SelectionChanged += Enable_Category_Removal;
        lsbox_Categories.SelectionChanged += Enable_Name_AddTextBox;

        txtBox_NewCategory.TextChanged += Enable_Category_Textbox;
        txtBox_NewCategory.KeyDown += AddCategory_KeyDown;
        btn_CatAdd.Click += Add_CatButton_Click;
        btn_CatRemove.Click += RemoveCatButton_Click;


        //Names boxes and buttons
        lsbox_Name.SelectionChanged += Enable_Name_Removal;

        txtBox_NewName.TextChanged += Enable_Name_AddBtn;
        txtBox_NewName.KeyDown += AddName_KeyDown;
        btn_NameAdd.Click += Add_NameButton_Click;
        btn_NameRemove.Click += RemoveNamesButton_Click;

    }

    protected static bool AddLineToFile(string filePath, string textBoxText)
    {

        if (!File.Exists(filePath)) File.Create(filePath).Close();


        if (new FileInfo(filePath).Length != 0)
        {
            File.AppendAllText(filePath, Environment.NewLine);
        }
        File.AppendAllText(filePath, textBoxText);
        return true;

    }

    //
    //---------Helper Events
    //
            
    private void Load_Names_Box(object sender, EventArgs e) //populate NamesBox ComboBox
    {
        lsbox_Name.Items.Clear();
        try
        {
            //
            //---------------Need to Check for null value here
            //
            string category = lsbox_Categories.SelectedItem?.ToString();
            if (category != "Fun Jumpers")
            {
                txtBox_NewName.IsEnabled = true;
                lsbox_Name.Items.Clear();
                for (int i = 0; i < myCategoriesDic[category].Length; i++)
                {
                    lsbox_Name.Items.Add(myCategoriesDic[category][i]);
                }
            }

            else
            {
                btn_NameAdd.IsEnabled = false;
                txtBox_NewName.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        Enable_Name_Removal(this, new EventArgs());
    }

    private void Enable_Category_Removal(object sender, EventArgs e) //enables "Add" Button for Categories
    {
        if (lsbox_Categories.SelectedItem != null)
        {
            btn_CatRemove.IsEnabled = true;
        }
        else { btn_CatRemove.IsEnabled = false; }
    }

    private void Enable_Category_Textbox(object sender, EventArgs e) 
    {
        if (txtBox_NewCategory.Text != "")
        {
            btn_CatAdd.IsEnabled = true;
        }
        else { btn_CatAdd.IsEnabled = false; }
    }

    private void Enable_Name_AddBtn(object sender, EventArgs e)  
    {
        if (txtBox_NewName.Text != "")
        {
            btn_NameAdd.IsEnabled = true;
        }
        else { btn_NameAdd.IsEnabled = false; }
    }

    private void Enable_Name_AddTextBox(object sender, EventArgs e) 
    {
        if (lsbox_Categories.SelectedItem != null)
        {
            txtBox_NewName.IsEnabled = true;
        }
        else { txtBox_NewName.IsEnabled = false; }
    }

    private void Enable_Name_Removal(object sender, EventArgs e) 
    {
        if (lsbox_Name.SelectedItem != null)
        {
            btn_NameRemove.IsEnabled = true;
        }
        else { btn_NameRemove.IsEnabled = false; }
    }

    private void AddCategory_KeyDown(object sender, KeyEventArgs e) 
    {
        if (e.Key == Key.Enter)
        {
            Add_CatButton_Click(this, new EventArgs());
        }
    }

    private void AddName_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Add_NameButton_Click(this, new EventArgs());
        }
    }




    //
    //---------Button Click Events
    //
    private void Add_CatButton_Click(object sender, EventArgs e) //add category to text file and list box
    {
        if (txtBox_NewCategory.Text != null)
        {
            //Check to see if the category you're trying to add is already there.
            if (!myCategoriesDic.ContainsKey(txtBox_NewCategory.Text))
            {
                //Add it to the dictionary
                string[] newString = Array.Empty<string>();
                myCategoriesDic.Add(txtBox_NewCategory.Text, newString);

                //add it to the list box
                lsbox_Categories.Items.Add(txtBox_NewCategory.Text);

                //Writing new category to categoryFile

                string filePath = Path.Combine(myConfigInfo.configDir, myConfigInfo.categoryFile);
                AddLineToFile(filePath, txtBox_NewCategory.Text);

                //create a new file
                using (StreamWriter file = new(myConfigInfo.categoryFile, true))
                {

                    //Creating new category file so names can be added
                    string tmpPath = Path.Combine(myConfigInfo.configDir, txtBox_NewCategory.Text + ".txt");
                    if (!File.Exists(tmpPath))
                    {
                        File.Create(tmpPath).Dispose();
                    }

                }

            }
            else
            {

                MessageBoxManager.GetMessageBoxStandard("Warning", "That Already Exists",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning,
                    WindowStartupLocation.CenterOwner).ShowWindowDialogAsync(this);
            }
            txtBox_NewCategory.Clear();
            btn_CatAdd.IsEnabled = false;
        }

    }

    private void Add_NameButton_Click(object sender, EventArgs e)
    {
        if (lsbox_Categories.SelectedItem != null)
        {

            string sCatName = lsbox_Categories.SelectedItem.ToString();
            string sName = txtBox_NewName.Text.ToString();
            List<string> tmpArrString = myCategoriesDic[sCatName].ToList();
            if (!tmpArrString.Contains(sName))
            {
                //Add it to the dictionary
                tmpArrString.Add(sName);
                myCategoriesDic[sCatName] = tmpArrString.ToArray();

                //add it to the list box
                lsbox_Name.Items.Add(sName);

                //Add it to the file
                string filePath = Path.Combine(myConfigInfo.configDir, sCatName + ".txt");
                AddLineToFile(filePath, sName);
            }
            else
            {
                MessageBoxManager.GetMessageBoxStandard("Warning", "That Already Exists",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning,
                    WindowStartupLocation.CenterOwner).ShowWindowDialogAsync(this);
            }

            txtBox_NewName.Clear();
            btn_NameAdd.IsEnabled = false;

        }
    }

    private void RemoveCatButton_Click(object sender, EventArgs e)
    {
        string localCategoryText = lsbox_Categories.SelectedItem?.ToString();
        if (localCategoryText != "Teams" && localCategoryText != "Teams - Private" && localCategoryText != "Fun Jumpers")
        {
            //Add code to remove from categories

            string specificCatFilePath = Path.Combine(myConfigInfo.configDir, localCategoryText + ".txt");
            if (File.Exists(specificCatFilePath))
            {
                File.Delete(specificCatFilePath);
            }

            if (myCategoriesDic.ContainsKey(localCategoryText))
            {
                myCategoriesDic.Remove(localCategoryText);
                string[] sArray = myCategoriesDic.Keys.ToArray();
                File.WriteAllText(myConfigInfo.categoryFile, string.Join(Environment.NewLine, sArray));
            }
            LoadCategoryBox();
        }
        else
        {

            MessageBoxManager.GetMessageBoxStandard("Warning", "Cannot remove Fun Jumpers or Teams categories",
                MsBox.Avalonia.Enums.ButtonEnum.Ok,MsBox.Avalonia.Enums.Icon.Warning, 
                WindowStartupLocation.CenterOwner).ShowWindowDialogAsync(this);
        }

    }

    private void RemoveNamesButton_Click(object sender, EventArgs e)
    {

        string localCategoryText = lsbox_Categories.SelectedItem?.ToString();
        string localNameText = lsbox_Name.SelectedItem?.ToString();
        //Add code to remove from categories
        string specificCatFilePath = Path.Combine(myConfigInfo.configDir, localCategoryText + ".txt");
        if (myCategoriesDic[localCategoryText].Contains(localNameText))
        {
            List<string> tmpList = myCategoriesDic[localCategoryText].ToList();
            tmpList.Remove(localNameText);
            myCategoriesDic[localCategoryText] = tmpList.ToArray();

            File.WriteAllText(specificCatFilePath, string.Join(Environment.NewLine, tmpList));
            Load_Names_Box(sender, new EventArgs());
        }
    }

    private void CloseButton_Click(object sender, EventArgs e)
    {
        this.Close();
    }
}