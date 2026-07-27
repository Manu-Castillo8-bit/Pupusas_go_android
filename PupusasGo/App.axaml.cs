using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace PupusasGo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new Window
            {
                Title = "PupuGo",
                Width = 400,
                Height = 780,
                MinWidth = 350,
                MaxWidth = 450,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FCFCFC")),
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = new MainView()
            };

            desktop.MainWindow = mainWindow;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}