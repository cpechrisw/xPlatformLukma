using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Diagnostics;

namespace xPlatformLukma;

public partial class AboutWindow : Window
{
    readonly string versionNumber = "1.8.3";
    
    public AboutWindow()
    {
        InitializeComponent();
        txtBlock_licAgreement.Text = "Overview These are the changes made to this specific license: Copyright Owner: Chris Webb Product Name: xPlatformLukma License Modifications Source code is NOT provided. Warranty is NOT given. \r\n\r\nDefinitions \"Product\", or \"software\" refers to the work that is copyrighted under this license.\r\n\r\n\"We\", or \"us\" refer to the copyright owner of this product. That owner is Chris Webb\r\n\r\n\"You\" refers to the licensee of this product.\r\n\r\nLicense, not purchase By purchasing this license, you are allowed to use this product, but we retain the copyright to the software.\r\n\r\nRedistribution You may not redistribute or resell the software.\r\n\r\nWarranty This product does NOT come with any warranty, express or implied, notwithstanding legislation to the contrary.\r\n\r\nSource Code This software does NOT provide the source code, and it is NOT legal to attempt to aquire it, whether by reverse engineering or otherwise.";
        lbl_versionNumber.Content = versionNumber;
    }
    public void ButtonClick_Close(object sender, RoutedEventArgs args)
    {
        this.Close();
    }
}