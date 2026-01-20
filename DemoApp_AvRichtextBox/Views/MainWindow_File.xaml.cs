using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DemoApp_AvRichtextBox.ViewModels;
using System;
using System.IO;

namespace DemoApp_AvRichtextBox.Views;

public partial class MainWindow : Window
{
   string OpenFilePath
   {
      get => ((MainWindowViewModel)DataContext!).CurrentOpenFilePath;
      set { ((MainWindowViewModel)DataContext!).CurrentOpenFilePath = value; }
   }

   string OpenFileName => ((MainWindowViewModel)DataContext!).CurrentOpenFileName;

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
            OpenFilePath = f;
            MainRTB.LoadXamlPackage(f);
            ShowPagePaddingValue();
         }
            
      
      }
   }


   private void SaveXamlPackageMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      MainRTB.SaveXamlPackage(Path.Combine(Path.GetDirectoryName(OpenFilePath)!, OpenFileName + ".xamlp"));
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
            OpenFilePath = f;
            MainRTB.LoadRtfDoc(f);
            ShowPagePaddingValue();
         }
      }
   }

   private void SaveRtfFileMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      MainRTB.SaveRtfDoc(Path.Combine(Path.GetDirectoryName(OpenFilePath)!, OpenFileName + ".rtf"));
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
            OpenFilePath = f;
            MainRTB.LoadWordDoc(f);
            ShowPagePaddingValue();
         }
            
      }

   }

   private void SaveWordFileMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      MainRTB.SaveWordDoc(Path.Combine(Path.GetDirectoryName(OpenFilePath)!, OpenFileName + ".docx"));
   }

   private void SaveHtmlFileMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      MainRTB.SaveHtmlDoc(Path.Combine(Path.GetDirectoryName(OpenFilePath)!, OpenFileName + ".html"));
   }
   
   private async void LoadHtmlFileMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      FilePickerOpenOptions filePickerOptions = new()
      {
         Title = "Open Html file",
         SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(Path.Combine(AppContext.BaseDirectory, "TestFiles")),
         FileTypeFilter = [new("Html files") { Patterns = ["*.html"], }],
         AllowMultiple = false
      };

      var topLevel = TopLevel.GetTopLevel(this);
      var files = await topLevel!.StorageProvider.OpenFilePickerAsync(filePickerOptions);

      if (files.Count == 1)
      {
         string? f = files[0].TryGetLocalPath();
         if (f != null)
         {
            OpenFilePath = f;
            MainRTB.LoadHtmlDoc(f);
            ShowPagePaddingValue();
         }

      }

   }

}