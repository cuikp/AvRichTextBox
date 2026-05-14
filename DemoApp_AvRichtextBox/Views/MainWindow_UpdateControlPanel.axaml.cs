using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
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

public partial class MainWindow
{
    private void ShowPagePaddingValue()
    {
        PagePaddingNSL.Value = MainRTB.FlowDocument.PagePadding.Left;
        PagePaddingNSR.Value = MainRTB.FlowDocument.PagePadding.Right;
        PagePaddingNST.Value = MainRTB.FlowDocument.PagePadding.Top;
        PagePaddingNSB.Value = MainRTB.FlowDocument.PagePadding.Bottom;
    }


    bool progChange = true;

    private void MainRTB_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        UpdatePanelValues();
    }

    private void MainRTB_KeyUp(object? sender, KeyEventArgs e)
    {
        //UpdatePanelValues();
    }

    private void UpdatePanelValues()
    {
        progChange = true;

        if (MainRTB.FlowDocument.Selection.GetStartPar() is Paragraph thisPar)
        {
            switch (thisPar.IsCellBlock)
            {
                case true:
                    TablePanel.Background = Brushes.LightGreen;
                    TablePanel.IsEnabled = true;
                    if (thisPar.GetOwningCell is Cell c)
                    {
                        CellBackgroundCP.Color = (Color)(c.CellBackground == null ? Colors.Transparent : c.CellBackground.Color!);
                        
                        if (c.CellVerticalAlignment.ToString() is string cellalignstring)
                        {
                            CellAlignmentCB.SelectedItem = CellAlignmentCB.Items.FirstOrDefault(it => it is ComboBoxItem cbi && cbi.Content is string s && s == cellalignstring)!;
                        }

                        Table thisTable = c.GetOwningTable;
                        TableBorderCP.Color = (Color)(thisTable.BorderBrush == null ? Colors.Transparent : thisTable.BorderBrush.Color!);
                        TableBorderNS.Value = thisTable.BorderThickness.Left;
                    }
                    break;

                case false:
                    TablePanel.IsEnabled = false;
                    TablePanel.Background = Brushes.Transparent;
                    CellBackgroundCP.Color = Colors.Transparent;
                    CellAlignmentCB.SelectedItem = null!;
                    break;
            }
            
        }

        TextRange selection = MainRTB.FlowDocument.Selection;

        if (selection.GetFormatting(BackgroundProperty) is ISolidColorBrush selBackground)
            HighlightCP.Color = selBackground == null ? Colors.Transparent : selBackground.Color;
        else
            HighlightCP.Color = Colors.Transparent;

        if (selection.GetFormatting(ForegroundProperty) is ISolidColorBrush selForeground)
            FontColorCP.Color = selForeground == null ? Colors.Transparent : selForeground.Color;
        else
            FontColorCP.Color = Colors.Black;


        FontFamily fontFamily = MainRTB.FontFamily;

        if (selection.GetStartPar() is Paragraph selPar)
        {
            LineHeightNS.Value = selPar.LineHeight;
            ParagraphBorderNS.Value = selPar.BorderThickness.Left;
            ParBorderCP.Color = selPar.BorderBrush == null ? Colors.Transparent : selPar.BorderBrush.Color;
            ParBackgroundCP.Color = selPar.Background == null ? Colors.Transparent : selPar.Background.Color;
            fontFamily = selPar.FontFamily;

        }

        if (selection.GetFormatting(FontFamilyProperty) is FontFamily selFFP)
            fontFamily = selFFP;

        if (fontFamily.ToString() is string fontFamilyString)
        {
            if (FontsCB.Items.FirstOrDefault(it => it?.ToString() == fontFamilyString) is string foundFF)
                FontsCB.SelectedItem = foundFF; // fontFamily.ToString();
            else
                FontsCB.SelectedItem = null!;
        }


        var size = selection.GetFormatting(FontSizeProperty);
        FontSizeNS.Value = Math.Round((double)(size ?? 14D));

        if (selection.GetFormatting(Inline.TextDecorationsProperty) is TextDecorationCollection tdc)
        {
            StrikeCB.IsChecked = tdc.Any(tc => tc.Location == TextDecorationLocation.Strikethrough);
            UnderCB.IsChecked = tdc.Any(tc => tc.Location == TextDecorationLocation.Underline);
            OverCB.IsChecked = tdc.Any(tc => tc.Location == TextDecorationLocation.Overline);
        }
        else
        {
            StrikeCB.IsChecked = false;
            UnderCB.IsChecked = false;
            OverCB.IsChecked = false;
        }

        progChange = false;

    }

    private void FlowDocument_Selection_Changed(TextRange selection)
    {

    }


}