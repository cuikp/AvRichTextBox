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
using DynamicData;

namespace DemoApp_AvRichtextBox.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;

        FontsCB.ItemsSource = GetAllFonts;

        UnderCB.AddHandler(InputElement.PointerReleasedEvent, UnderCB_PointerReleased, RoutingStrategies.Tunnel);
        StrikeCB.AddHandler(InputElement.PointerReleasedEvent, StrikeCB_PointerReleased, RoutingStrategies.Tunnel);
        OverCB.AddHandler(InputElement.PointerReleasedEvent, OverCB_PointerReleased, RoutingStrategies.Tunnel);

    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        MainRTB.FlowDocument.SelectionChanged += FlowDocument_Selection_Changed;


#if DEBUG

        DockPanel debugCBPanel = new() { VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(10) };
        TextBlock debugTB = new() { Text = "DebugPanel", VerticalAlignment = VerticalAlignment.Center };
        CheckBox debugCB = new() { Focusable = false };
        debugCB.IsCheckedChanged += DebugPanelCB_CheckedUnchecked;
        debugCB.IsChecked = true;
        debugCBPanel.Children.Add(debugCB);
        debugCBPanel.Children.Add(debugTB);
        TopPanel.Children.Add(debugCBPanel);

#endif

        progChange = false;


        //DEBUG
        //CreateTestDocumentWithTable();
        //OpenTestDocument();

    }

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
        
        newPar.Inlines.AddRange([
            new EditableRun("A "),
            new EditableRun("first line with super/subscripts:"),
            new EditableRun(" H"),
            new EditableRun("2") { BaselineAlignment = BaselineAlignment.Subscript },
            new EditableRun("O"),
            new EditableRun(" at 2 g/m") { },
            new EditableRun("3") { BaselineAlignment = BaselineAlignment.Superscript },
            new EditableRun(", and a simple hyperlink: "),
            new EditableHyperlink("go to google", @"https://www.google.com"),
            new EditableRun(" for testing.")
        ]);

        MainRTB.FlowDocument.Blocks.Add(newPar);

        //Test Table
        int noCols = 5;
        int noRows = 4;
        Table newTable = new (noCols, noRows, MainRTB.FlowDocument) { BorderThickness = new(1), BorderBrush = Brushes.ForestGreen, TableAlignment = HorizontalAlignment.Center };
        
        for (int rowno = 0; rowno < noRows; rowno++)
        {
            for (int colno = 0; colno < noCols; colno++)
            {
                int cellno = rowno * noCols + colno;
                Cell c = newTable.Cells[cellno];
                c.CellVerticalAlignment = VerticalAlignment.Center;
                Paragraph p = new (MainRTB.FlowDocument) { TextAlignment = TextAlignment.Center };
                p.Inlines.Add(new EditableRun("col:" + colno));
                p.Inlines.Add(new EditableLineBreak());
                p.Inlines.Add(new EditableRun("row:" + rowno));
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

    private void CreateNewDocumentMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        MainRTB.CreateNewDocument();

    }
      
    private void FindTextBox_KeyDown(object? sender, KeyEventArgs e)
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

    private void FindButton_Click(object? sender, RoutedEventArgs e)
    {
        PerformFind();

    }

    private void AddTableButton_Click(object? sender, RoutedEventArgs e)
    {
        if (MainRTB.FlowDocument.Selection.Length > 0) return;
        int currentStart = MainRTB.FlowDocument.Selection.Start;
        MainRTB.FlowDocument.InsertParagraphAt(currentStart);
        MainRTB.FlowDocument.Select(currentStart + 1, 0);

        if (MainRTB.FlowDocument.Selection.GetStartPar() is not Paragraph currParagraph) return;
        int insertParIndex = MainRTB.FlowDocument.Blocks.IndexOf(currParagraph);

        int noCols = Convert.ToInt32(AddColsNS.Value);
        int noRows = Convert.ToInt32(AddRowsNS.Value);

        Table newTable = new (noCols, noRows, MainRTB.FlowDocument);

        MainRTB.FlowDocument.Blocks.Insert(insertParIndex, newTable);
        int endOfTable = currentStart + noCols * noRows;
        MainRTB.FlowDocument.Select(endOfTable, 0);

    }


    private async void PerformFind()
    {
        FindTB.Background = Brushes.White;

        if (string.IsNullOrEmpty(FindTB.Text)) return;

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