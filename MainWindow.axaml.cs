using Avalonia.Controls;
using Avalonia.Platform;

namespace xPlatformLukma
{
    public partial class MainWindow : Window
    {
        private SettingsWindow _settingsWindow;
        private LogoWindow _logoWindow;
        private CategoriesWindow _categoriesWindow;
        
        public MainWindow()
        {
            InitializeComponent();
            int screenWidth = Screens.Primary.WorkingArea.Width;
            int screenHeight = Screens.Primary.WorkingArea.Height;
            lbl_ScreenRes.Content = "Monitor Resolution: " + screenWidth.ToString() + "x" + screenHeight.ToString();


        }




        //
        // ---------
        //


        //
        // ---------Button Click Events
        //


        //
        //---------Menu Click Events
        //
        public void Menu_AboutClick()
        {
            AboutWindow aboutDialog = new AboutWindow();
            aboutDialog.Show();
        }

        public void Menu_CategoryNamesClick()
        {
            _categoriesWindow ??= new CategoriesWindow();
            _categoriesWindow.Show();
        }

        public void Menu_SettingsClick()
        {
            _settingsWindow ??= new SettingsWindow();
            _settingsWindow.Show();
        }
        public void Menu_LogoClick()
        {
            _logoWindow ??= new LogoWindow();
            _logoWindow.Show();
        }

    }

    //
    // ---------Data Classes
    //


}