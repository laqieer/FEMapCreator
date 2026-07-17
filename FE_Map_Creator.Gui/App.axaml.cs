using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FE_Map_Creator.Gui.Views;

namespace FE_Map_Creator.Gui;

public partial class App : Application
{
  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
#if DEBUG
    this.AttachDeveloperTools();
#endif
  }

  public override void OnFrameworkInitializationCompleted()
  {
    if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      desktop.MainWindow = new MainWindow();
    }
    else if (this.ApplicationLifetime is IActivityApplicationLifetime activity)
    {
      activity.MainViewFactory = () => new MainView();
    }
    else if (this.ApplicationLifetime is ISingleViewApplicationLifetime single_view)
    {
      single_view.MainView = new MainView();
    }

    base.OnFrameworkInitializationCompleted();
  }
}