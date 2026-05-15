using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvRichTextBox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DemoApp_AvRichtextBox.Views;

public partial class MainWindow : Window
{

    public static List<string> GetAllFonts
    {
        get
        {
            List<string> returnList = [];
            foreach (var font in FontManager.Current.SystemFonts)
                returnList.Add(font.Name);
            return returnList;
        }

    }

    public MainWindow()
    {
        InitializeComponent();


        Loaded += MainWindow_Loaded;

        FontsCB.ItemsSource = GetAllFonts;

        UnderCB.AddHandler(InputElement.PointerReleasedEvent, UnderCB_PointerReleased, RoutingStrategies.Tunnel);
        StrikeCB.AddHandler(InputElement.PointerReleasedEvent, StrikeCB_PointerReleased, RoutingStrategies.Tunnel);
        OverCB.AddHandler(InputElement.PointerReleasedEvent, OverCB_PointerReleased, RoutingStrategies.Tunnel);


    }

    private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MainRTB.FlowDocument.SelectionChanged += FlowDocument_Selection_Changed;


#if DEBUG

        DockPanel debugCBPanel = new() { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom, Margin = new Thickness(10) };
        TextBlock debugTB = new() { Text = "DebugPanel", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        CheckBox debugCB = new() { Focusable = false };
        debugCB.IsCheckedChanged += DebugPanelCB_CheckedUnchecked;
        debugCB.IsChecked = true;
        debugCBPanel.Children.Add(debugCB);
        debugCBPanel.Children.Add(debugTB);
        //DockPanel.SetDock(debugTB, Dock.Top);
        TopPanel.Children.Add(debugCBPanel);

#endif

        progChange = false;


        //DEBUG
        //CreateTestDocumentWithTable();
        //OpenTestDocument();

    }

    internal void OpenTestDocument()
    {
        string testdoc = Path.Combine(AppContext.BaseDirectory, "TestFiles\\TestDocumentXamlPackage.xamlp");
        MainRTB.LoadXamlPackage(testdoc);
        OpenFilePath = testdoc;
    }

    internal void CreateTestDocumentWithTable()
    {
        MainRTB.FlowDocument.Blocks.Clear();

        Paragraph newPar = new(MainRTB.FlowDocument);
        newPar.Inlines.Add(new EditableRun("A "));
        newPar.Inlines.Add(new EditableRun("first line with super/subscripts:"));
        
        
        newPar.Inlines.Add(new EditableRun(" H"));
        newPar.Inlines.Add(new EditableRun("2") { BaselineAlignment = BaselineAlignment.Subscript });
        newPar.Inlines.Add(new EditableRun("O"));
        newPar.Inlines.Add(new EditableRun(" at 2 g/m") { });
        newPar.Inlines.Add(new EditableRun("3") { BaselineAlignment = BaselineAlignment.Superscript });

        newPar.Inlines.Add(new EditableRun(", and a simple hyperlink: "));
        newPar.Inlines.Add(new EditableHyperlink("go to google", @"https://www.google.com"));
        newPar.Inlines.Add(new EditableRun(" for testing."));
        MainRTB.FlowDocument.Blocks.Add(newPar);

        //Test Table
        int noCols = 5;
        int noRows = 4;
        Table newTable = new (noCols, noRows, MainRTB.FlowDocument) { BorderThickness = new(1), BorderBrush = Brushes.ForestGreen, TableAlignment = Avalonia.Layout.HorizontalAlignment.Center };
        
        for (int rowno = 0; rowno < noRows; rowno++)
        {
            for (int colno = 0; colno < noCols; colno++)
            {
                int cellno = rowno * noCols + colno;
                Cell c = newTable.Cells[cellno];
                c.CellVerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                Paragraph p = new (MainRTB.FlowDocument) { TextAlignment = TextAlignment.Center };
                p.Inlines.Add(new EditableRun("c:" + colno));
                p.Inlines.Add(new EditableLineBreak());
                p.Inlines.Add(new EditableRun("r:" + rowno));
                c.CellBlocks[0] = p;
            }
        }

        //Test merging cells  - also combine this into a MergeCellsAt() method
        newTable.GetCellAt(1, 0)?.ColSpan = 2;
        newTable.RemoveCellAt(1, 1);
        newTable.GetCellAt(2, 3)?.RowSpan = 2;
        newTable.RemoveCellAt(3, 3);
        

        MainRTB.FlowDocument.Blocks.Add(newTable);
        
        Paragraph newPar2 = new(MainRTB.FlowDocument);
        newPar2.Inlines.Add(new EditableRun("Some extra text after the table."));
        MainRTB.FlowDocument.Blocks.Add(newPar2);


        Dispatcher.UIThread.Post(() =>
        {
            MainRTB.UpdateLayout();
            MainRTB.FlowDocument.Select(0, 0);
        });
             

    }


    //static bool GetMergedTestCell(Paragraph newPar, Cell newCell, int rowno, int colno)
    //{
    //    //Add merged cells:
    //    if (rowno == 0 && colno == 0)
    //    {
    //        newCell.ColSpan = 2;
    //        newCell.RowSpan = 2;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Center;
    //    }

    //    if (rowno == 0 && colno == 2)
    //    {
    //        newCell.RowSpan = 2;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Center;
    //    }

    //    if (rowno == 1 && colno == 3)
    //    {
    //        newCell.RowSpan = 3;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Center;
    //    }

    //    if (rowno == 2 && colno == 0)
    //    {
    //        newCell.ColSpan = 3;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Center;
    //    }

    //    if (rowno == 0 && colno == 4)
    //    {
    //        newCell.RowSpan = 4;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Center;
    //    }

    //    return
    //       (rowno == 0 && colno == 1) ||
    //       (rowno == 1 && colno == 0) ||
    //       (rowno == 1 && colno == 1) ||
    //       (rowno == 1 && colno == 2) ||
    //       (rowno == 2 && colno == 3) ||
    //       (rowno == 3 && colno == 3) ||
    //       (rowno == 2 && colno == 1) ||
    //       (rowno == 2 && colno == 2) ||

    //       (rowno == 1 && colno == 4) ||
    //       (rowno == 2 && colno == 4) ||
    //       (rowno == 3 && colno == 4);

    //}


    //static bool GetMergedTestCell2(Paragraph newPar, Cell newCell, int rowno, int colno)
    //{
    //    //Add merged cells:
    //    if (rowno == 0 && colno == 0)
    //    {
    //        newCell.ColSpan = 2;
    //        newCell.RowSpan = 2;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Top;
    //    }

    //    if (rowno == 0 && colno == 2)
    //    {
    //        newCell.RowSpan = 2;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Center;
    //    }

    //    if (rowno == 1 && colno == 3)
    //    {
    //        newCell.RowSpan = 3;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Center;
    //        newCell.CellBackground = Brushes.LightSteelBlue;
    //    }

    //    if (rowno == 2 && colno == 0)
    //    {
    //        newCell.ColSpan = 3;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Center;
    //    }

    //    if (rowno == 1 && colno == 4)
    //    {
    //        newCell.RowSpan = 3;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Center;
    //    }

    //    if (rowno == 1 && colno == 3)
    //    {
    //        newCell.ColSpan = 2;
    //        newCell.RowSpan = 2;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Center;
    //    }

    //    if (rowno == 3 && colno == 2)
    //    {
    //        newCell.ColSpan = 3;
    //        newCell.CellVerticalAlignment = VerticalAlignment.Center;
    //    }

    //    return
    //       (rowno == 0 && colno == 1) ||
    //       (rowno == 1 && colno == 0) ||
    //       (rowno == 1 && colno == 1) ||
    //       (rowno == 1 && colno == 2) ||
    //       (rowno == 2 && colno == 3) ||
    //       (rowno == 2 && colno == 1) ||
    //       (rowno == 2 && colno == 2) ||
    //       (rowno == 3 && colno == 3) ||
    //       (rowno == 3 && colno == 4) ||

    //       (rowno == 1 && colno == 4) ||
    //       (rowno == 2 && colno == 4);


    //}



    private void CreateNewDocumentMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MainRTB.CreateNewDocument();

    }
      

    private void FindTextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        FindTB.Background = Brushes.White;

        if (e.Key == Key.Enter)
        {
            PerformFind();
            e.Handled = true;
        }

    }

    private void FindTextBox_GotFocus(object? sender, FocusChangedEventArgs e)
    {
        FindTB.Background = Brushes.White;
        this.FindTB.Focus();
    }

    private void FindTextBox_LostFocus(object? sender, FocusChangedEventArgs e)
    {
        FindTB.Background = Brushes.LightGray;

    }

    private void FindButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PerformFind();

    }

    private async void PerformFind()
    {
        FindTB.Background = Brushes.White;

        if (string.IsNullOrEmpty(FindTB.Text)) return;

        //MatchCollection foundMatches = Regex.Matches(MainRTB.FlowDocument.Text.Replace("\r\n", "\r"), FindTB.Text);
        MatchCollection foundMatches = Regex.Matches(MainRTB.FlowDocument.Text, FindTB.Text);

        Match? firstMatch = foundMatches.FirstOrDefault(m => m.Index >= MainRTB.FlowDocument.Selection.End);

        //Debug.WriteLine("foundmathidx = " + firstMatch.Index);

        if (firstMatch != null)
        {
            MainRTB.FlowDocument.Select(firstMatch.Index, FindTB.Text.Length);
            MainRTB.ScrollToSelection();
        }
        else
        {
            FindTB.Background = Brushes.Coral;
            FindBut.Focus();
        }


    }

    

}