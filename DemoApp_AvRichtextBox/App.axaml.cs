using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using DemoApp_AvRichtextBox.ViewModels;
using DemoApp_AvRichtextBox.Views;
using System.Linq;

namespace DemoApp_AvRichtextBox;

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
         // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
         // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
   
         desktop.MainWindow = new MainWindow
         {
            DataContext = new MainWindowViewModel(),
         };
      }

      base.OnFrameworkInitializationCompleted();
   }

   
}