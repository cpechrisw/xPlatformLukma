using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace xPlatformLukma
{
    public partial class App : Application
    {
        public static Window MainAppWindow { get; private set; }
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainAppWindow = new MainWindow();
                desktop.MainWindow = MainAppWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}