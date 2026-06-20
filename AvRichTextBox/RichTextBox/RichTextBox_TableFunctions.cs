using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvRichTextBox;

public partial class RichTextBox 
{

    double GetWantedCellHeight(EditableCell ec)
    {
        if (ec.GetVisualDescendants().OfType<ItemsControl>().FirstOrDefault() is not ItemsControl ic)
            return ec.Bounds.Height;

        ic.Measure(new Size(ic.Bounds.Width, double.PositiveInfinity));
        return Math.Ceiling(ic.DesiredSize.Height + ec.Padding.Top + ec.Padding.Bottom);
    }

    private void EditableParagraph_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender is not EditableParagraph thisEdPar) return;
        if (thisEdPar.DataContext is not Paragraph thisPar) return;
        if (thisPar.OwningTable is not Table thisTable) return;
        if (thisPar.OwningCell is not Cell thisCell) return;
        if (thisEdPar.Parent is not ContentPresenter contPres || contPres.Parent is not ItemsControl itemsControl) return;
        //Debug.WriteLine("\nnew size height = " + e.NewSize);

        double maxCellContentHeight = 0;

        if (itemsControl.FindAncestorOfType<ItemsControl>() is ItemsControl tableIC)
        {
            if (tableIC?.GetVisualDescendants().OfType<ItemsPresenter>().FirstOrDefault() is ItemsPresenter presenter)
            {
                if (presenter?.GetVisualDescendants().OfType<BindableGrid>().FirstOrDefault() is BindableGrid bgrid)
                {
                    List<EditableCell> rowECs = [.. bgrid.GetVisualDescendants().OfType<EditableCell>().Where(ec => ec.DataContext is Cell c && c.RowNo == thisCell.RowNo)];
                    maxCellContentHeight = rowECs.Max(GetWantedCellHeight);
                    itemsControl.Measure(new Size(itemsControl.Bounds.Width, double.PositiveInfinity));
                    var wantedHeight = Math.Ceiling(itemsControl.DesiredSize.Height + thisPar.OwningCell.Padding.Top + thisPar.OwningCell.Padding.Bottom);
                    thisTable.RowDefs[thisPar.OwningCell.RowNo].Height = new GridLength(Math.Max(maxCellContentHeight, wantedHeight));
                }
            }
        }

    }

    private void EditableCell_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender is not EditableCell ecell) return;
        if (ecell.DataContext is not Cell thisCell) return;

        Dispatcher.UIThread.Post(() =>
        {
            thisCell.OwningTable.Height = thisCell.OwningTable.RowDefs.Sum(rdef => rdef.Height.Value);
            //Debug.WriteLine("table height = " + thisCell.OwningTable.Height);
        });

        
    }
}

