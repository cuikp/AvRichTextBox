using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace DemoApp_AvRichtextBox.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
   public new event PropertyChangedEventHandler? PropertyChanged;
   public void NotifyPropertyChanged([CallerMemberName] String propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public string CurrentOpenFilePath 
   { 
      get; 
      set 
      { 
         field = value; 
         NotifyPropertyChanged(nameof(CurrentOpenFileNameExt)); 
      } 
   } = Path.Combine(AppContext.BaseDirectory, "TestFiles", "NewFile");

   internal string CurrentOpenFileName => Path.GetFileNameWithoutExtension(CurrentOpenFilePath);
   string CurrentExt => Path.GetExtension(CurrentOpenFilePath);
   public string CurrentOpenFileNameExt => "DemoApp_AvRichTextBox - " + CurrentOpenFileName + CurrentExt;

   public MainWindowViewModel()
   {
     

   }     

}
