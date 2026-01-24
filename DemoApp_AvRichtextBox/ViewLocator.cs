using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DemoApp_AvRichtextBox.ViewModels;
using System;

namespace DemoApp_AvRichtextBox;

public class ViewLocator : IDataTemplate
{

   public Control? Build(object? param)
   {
      if (param is null)
         return null;

      var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);

      if (Type.GetType(name) is Type type)
      {
         if (Activator.CreateInstance(type) is Control c)
            return c;
      }

      return new TextBlock { Text = "Not Found: " + name };
   }

   public bool Match(object? data)
   {
      return data is ViewModelBase;
   }
}
