using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DemoApp_AvRichtextBox.ViewModels;
using System;
using System.IO;

namespace DemoApp_AvRichtextBox.Views;

public partial class MainWindow : Window
{
   string openFilePath
   {
      get { return ((MainWindowViewModel)DataContext!).CurrentOpenFilePath; }
      set { ((MainWindowViewModel)DataContext!).CurrentOpenFilePath = value; }
   }

   string openFileName => ((MainWindowViewModel)DataContext!).CurrentOpenFileName;

   private async void LoadXamlPackageMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {

      FilePickerOpenOptions filePickerOptions = new()
      {
         Title = "Open Xaml package file",
         SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(Path.Combine(AppContext.BaseDirectory, "TestFiles")),
         FileTypeFilter = [new("Xaml package files") { Patterns = ["*.xamlp"] }],
         AllowMultiple = false
      };

      var topLevel = TopLevel.GetTopLevel(this);
      var files = await topLevel!.StorageProvider.OpenFilePickerAsync(filePickerOptions);

      if (files.Count == 1)
      {
         string? f = files[0].TryGetLocalPath();
         if (f != null)
         {
            openFilePath = f;
            MainRTB.LoadXamlPackage(f);
            ShowPagePaddingValue();
         }
            
      
      }
   }


   private void SaveXamlPackageMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      MainRTB.SaveXamlPackage(Path.Combine(Path.GetDirectoryName(openFilePath)!, openFileName + ".xamlp"));
   }

   private async void LoadRtfFileMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      FilePickerOpenOptions filePickerOptions = new()
      {
         Title = "Open Rtf file",
         SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(Path.Combine(AppContext.BaseDirectory, "TestFiles")),
         FileTypeFilter = [new("Rtf files") { Patterns = ["*.rtf"] }],
         AllowMultiple = false
      };

      var topLevel = TopLevel.GetTopLevel(this);
      var files = await topLevel!.StorageProvider.OpenFilePickerAsync(filePickerOptions);

      if (files.Count == 1)
      {
         string? f = files[0].TryGetLocalPath();
         if (f != null)
         {
            openFilePath = f;
            MainRTB.LoadRtfDoc(f);
            ShowPagePaddingValue();
         }
      }
   }

   private void SaveRtfFileMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      MainRTB.SaveRtfDoc(Path.Combine(Path.GetDirectoryName(openFilePath)!, openFileName + ".rtf"));
   }


   private async void LoadWordFileMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {

      FilePickerOpenOptions filePickerOptions = new()
      {
         Title = "Open Word doc file",
         SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(Path.Combine(AppContext.BaseDirectory, "TestFiles")),
         FileTypeFilter = [new("Word doc files") { Patterns = ["*.docx"], }],
         AllowMultiple = false
      };
      

      var topLevel = TopLevel.GetTopLevel(this);
      var files = await topLevel!.StorageProvider.OpenFilePickerAsync(filePickerOptions);

      if (files.Count == 1)
      {
         string? f = files[0].TryGetLocalPath();
         if (f != null)
         {
            openFilePath = f;
            MainRTB.LoadWordDoc(f);
            ShowPagePaddingValue();
         }
            
      }

   }

   private void SaveWordFileMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      MainRTB.SaveAsWord(Path.Combine(Path.GetDirectoryName(openFilePath)!, openFileName + ".docx"));
   }


}