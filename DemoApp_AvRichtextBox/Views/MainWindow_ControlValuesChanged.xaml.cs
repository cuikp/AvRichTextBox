using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using AvRichTextBox;
using System;
using System.Diagnostics;

namespace DemoApp_AvRichtextBox.Views;

public partial class MainWindow
{

    internal void PagePaddingNSL_ValueChanged(double value)
    {
        Thickness p = MainRTB.FlowDocument.PagePadding;
        MainRTB.FlowDocument.PagePadding = new Thickness(PagePaddingNSL.Value, p.Top, p.Right, p.Bottom);
    }

    internal void PagePaddingNST_ValueChanged(double value)
    {
        Thickness p = MainRTB.FlowDocument.PagePadding;
        MainRTB.FlowDocument.PagePadding = new Thickness(p.Left, PagePaddingNST.Value, p.Right, p.Bottom);
    }

    internal void PagePaddingNSR_ValueChanged(double value)
    {
        Thickness p = MainRTB.FlowDocument.PagePadding;
        MainRTB.FlowDocument.PagePadding = new Thickness(p.Left, p.Top, PagePaddingNSR.Value, p.Bottom);
    }

    internal void PagePaddingNSB_ValueChanged(double value)
    {
        Thickness p = MainRTB.FlowDocument.PagePadding;
        MainRTB.FlowDocument.PagePadding = new Thickness(p.Left, p.Top, p.Right, PagePaddingNSB.Value);
    }



    bool _applyingFormatting = false;

    internal void FontSizeNS_UserValueChanged(double value)
    {
        _applyingFormatting = true;

        MainRTB.FlowDocument.Selection.ApplyFormatting(FontSizeProperty, value);

        Dispatcher.UIThread.Post(() =>
        {
            _applyingFormatting = false;

        }, DispatcherPriority.Background);
    }


    internal void LineHeightNS_UserValueChanged(double value)
    {
        foreach (Paragraph p in MainRTB.FlowDocument.GetSelectedParagraphs)
        {
            //p.LineSpacing *= 2;
            p.LineHeight = value;
        }

    }

    internal void ParagraphBorderNS_UserValueChanged(double value)
    {
        foreach (Paragraph p in MainRTB.FlowDocument.GetSelectedParagraphs)
            p.BorderThickness = new Thickness(value);

    }

    private void ParBorder_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (progChange) return;
        SolidColorBrush hBrush = new(e.NewColor);
        foreach (Paragraph p in MainRTB.FlowDocument.GetSelectedParagraphs)
            p.BorderBrush = hBrush;

    }

    private void ParBackground_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (progChange) return;
        SolidColorBrush hBrush = new(e.NewColor);
        foreach (Paragraph p in MainRTB.FlowDocument.GetSelectedParagraphs)
            p.Background = hBrush;

    }


    private void FontCP_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        SolidColorBrush hBrush = new(e.NewColor);
        MainRTB.FlowDocument.Selection.ApplyFormatting(ForegroundProperty, hBrush);

    }

    private void HighlightCP_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        SolidColorBrush hBrush = new(e.NewColor);
        MainRTB.FlowDocument.Selection.ApplyFormatting(BackgroundProperty, hBrush);

    }

#if DEBUG
    private void DebugPanelCB_CheckedUnchecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is CheckBox thisCB && MainRTB != null)
            MainRTB.ShowDebuggerPanelInDebugMode = thisCB.IsChecked is bool b && b;
    }
#endif

    private void FontsComboBox_DropDownClosed(object? sender, System.EventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
        {
            string? newFont = comboBox.SelectedItem.ToString();
            if (newFont != null)
                MainRTB.FlowDocument.Selection.ApplyFormatting(FontFamilyProperty, new FontFamily(newFont));
        }

    }

    private void JustificationComboBox_DropDownClosed(object? sender, System.EventArgs e)
    {
        if (sender is ComboBox cbox && cbox.SelectedItem is ComboBoxItem cbitem)
        {
            if (cbitem.Content is string selJust && MainRTB.FlowDocument.Selection.GetStartPar() is Paragraph p)
            {
                p.TextAlignment = selJust switch
                {
                    "Left" => TextAlignment.Left,
                    "Center" => TextAlignment.Center,
                    "Right" => TextAlignment.Right,
                    "Justified" => TextAlignment.Justify,
                    _ => TextAlignment.Left
                };
            }
        }


    }

    internal void RTBZoomNS_UserValueChanged(double value)
    {
        MainRTB.Zoom = value;
    }

    private void CheckBox_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is CheckBox cbox && cbox.IsChecked is bool b)
            MainRTB.IsReadOnly = b;
    }

    private void StrikeCB_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (MainRTB.FlowDocument.Selection.Length == 0) return;
        if (sender is CheckBox cb && cb.IsChecked is bool b)
        {
            ApplyDecoration(!b, TextDecorationLocation.Strikethrough); // checkbox not yet changed on PointerReleased
        }
    }

    private void UnderCB_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (MainRTB.FlowDocument.Selection.Length == 0) return;

        if (sender is CheckBox cb && cb.IsChecked is bool b)
        {
            ApplyDecoration(!b, TextDecorationLocation.Underline); // checkbox not yet changed on PointerReleased
        }

    }

    private void OverCB_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (MainRTB.FlowDocument.Selection.Length == 0) return;
        if (sender is CheckBox cb && cb.IsChecked is bool b)
        {
            ApplyDecoration(!b, TextDecorationLocation.Overline); // checkbox not yet changed on PointerReleased
        }

    }

    private void ApplyDecoration(bool on, TextDecorationLocation textDecLoc)
    {
        _applyingFormatting = true;


        MainRTB.FlowDocument.Selection.ApplyFormatting(Inline.TextDecorationsProperty, textDecLoc);

        Dispatcher.UIThread.Post(() =>
        {
            _applyingFormatting = false;

        }, DispatcherPriority.Background);


    }

    private void CellAlignmentCB_DropDownClosed(object? sender, EventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem cbi)
        {
            if (MainRTB.FlowDocument.Selection.GetStartPar() is Paragraph thisPar && thisPar.IsCellBlock)
            {
               thisPar.GetOwningCell.CellVerticalAlignment = cbi.Content?.ToString() switch
                {
                    "Top" => Avalonia.Layout.VerticalAlignment.Top,
                    "Center" => Avalonia.Layout.VerticalAlignment.Center,
                    "Bottom" => Avalonia.Layout.VerticalAlignment.Bottom,
                    _ => Avalonia.Layout.VerticalAlignment.Top
                };
            }
        }
    }

    private void CellBackground_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (progChange) return;
        SolidColorBrush hBrush = new(e.NewColor);
        if (MainRTB.FlowDocument.Selection.GetStartPar() is Paragraph thisPar && thisPar.IsCellBlock)
        {
            thisPar.GetOwningCell.CellBackground = hBrush;
        }
    }
    
    private void TableBorder_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (progChange) return;
        SolidColorBrush hBrush = new(e.NewColor);
        if (MainRTB.FlowDocument.Selection.GetStartPar() is Paragraph thisPar && thisPar.IsCellBlock)
        {
            thisPar.GetOwningCell.GetOwningTable.BorderBrush = hBrush;
        }
    }

    internal void TableBorderNS_UserValueChanged(double value)
    {
        if (MainRTB.FlowDocument.Selection.GetStartPar() is Paragraph thisPar && thisPar.IsCellBlock && thisPar.GetOwningCell is Cell c && c.GetOwningTable is Table t)
        {
            t.BorderThickness = new Thickness(value);
        }
            
    }



}