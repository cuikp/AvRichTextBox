
using AvRichTextBox;
using DocumentFormat.OpenXml.Spreadsheet;
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


   private string _CurrentOpenFilePath = Path.Combine(AppContext.BaseDirectory, "TestFiles", "NewFile");
   public string CurrentOpenFilePath { get => _CurrentOpenFilePath; set { _CurrentOpenFilePath = value; NotifyPropertyChanged(nameof(CurrentOpenFileNameExt)); } }

   internal string CurrentOpenFileName => Path.GetFileNameWithoutExtension(CurrentOpenFilePath);
   string CurrentExt => Path.GetExtension(CurrentOpenFilePath);
   public string CurrentOpenFileNameExt => "DemoApp_AvRichTextBox - " + CurrentOpenFileName + CurrentExt;

   public FlowDocument MyFlowDoc { get; set; } = new FlowDocument();
   //public ObservableCollection<Block> MyBlocks { get; set; } = [];

   public MainWindowViewModel()
   {
      //Testing creation a flow doc for binding:
      //Paragraph p = new();
      //p.Inlines.Add(new EditableRun("This is a fixxxxxrst inline"));

      //MyFlowDoc.Blocks.Clear();
      //MyFlowDoc.Blocks.Add(p);

      //MyBlocks.Add(p);


   }     

}
