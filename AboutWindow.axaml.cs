using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Diagnostics;

namespace xPlatformLukma;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }
    public void ButtonClick_Close(object? sender, RoutedEventArgs args)
    {
        this.Close();
    }
}