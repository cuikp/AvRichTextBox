using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AvRichTextBox;

public class BordersCanvas : Control
{
    readonly List<double> ColPoints = [];
    readonly List<double> RowPoints = [];

    readonly Table myTable = null!;

    public BordersCanvas(Table table)
    {
        ClipToBounds = false;
        
        myTable = table;
      
    }


    internal void UpdateColPoints(ColumnDefinitions columnDefs)
    {
        double colPoint = 0;
        ColPoints.Clear();
        foreach (ColumnDefinition cdef in columnDefs)
        {
            colPoint += cdef.Width.Value;
            ColPoints.Add(colPoint);
            
        }
        InvalidateVisual();
    }

    internal void UpdateRowPoints(RowDefinitions rowDefs)
    {
        double rowPoint = 0;
        RowPoints.Clear();
        foreach (RowDefinition rdef in rowDefs)
        {
            rowPoint += rdef.Height.Value;
            RowPoints.Add(rowPoint);
        }
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var pen = new Pen();

        double lastRowPoint = 0;
        double lastCellPoint = 0;
        double cellHeight = 0;
        for (int rowno = 0; rowno < RowPoints.Count; rowno++)
        {
            lastCellPoint = 0;
            cellHeight = RowPoints[rowno] - lastRowPoint;
            

            int horizMergeCount = 0;

            for (int colno = 0; colno < ColPoints.Count; colno++)
            {
               
                if (myTable.GetCellAt(rowno, colno) is Cell thisCell)
                {
                    horizMergeCount = thisCell.ColSpan - 1;

                    double thisCellRight = ColPoints[colno + thisCell.ColSpan - 1];
                    double thisCellBottom = RowPoints[rowno + thisCell.RowSpan - 1];

                    //cache these pens for each Cell maybe? $$$$$$$$$$
                    pen = new Pen(thisCell.BorderBrush, thisCell.BorderThickness.Right);

                    // Vertical cell lines (right)
                    context.DrawLine(pen, new Point(thisCellRight, lastRowPoint), new Point(thisCellRight, thisCellBottom));

                    pen = new Pen(thisCell.BorderBrush, thisCell.BorderThickness.Left);

                    if (colno== 0)  // Always draw left borders for column 0
                        context.DrawLine(pen, new Point(0, lastRowPoint), new Point(0, thisCellBottom));

                    pen = new Pen(thisCell.BorderBrush, thisCell.BorderThickness.Bottom);

                    //Horizontal cell lines
                    context.DrawLine(pen, new Point(lastCellPoint, thisCellBottom), new Point(thisCellRight, thisCellBottom));

                    pen = new Pen(thisCell.BorderBrush, thisCell.BorderThickness.Top);

                    if (rowno == 0)  // draw top borders for row 0
                        context.DrawLine(pen, new Point(lastCellPoint, 0), new Point(thisCellRight, 0));

                }

                lastCellPoint = ColPoints[colno];
            }
            
            lastRowPoint = RowPoints[rowno];
        }
           

    }
}

public class BindableGrid : Grid
{
    public BindableGrid()
    {
        this.Loaded += BindableGrid_Loaded;
     
    }

    private void BindableGrid_Loaded(object? sender, RoutedEventArgs e)
    {
        RowDefinitions = RowDefs!;
        ColumnDefinitions = ColDefs!;

        //ColDefs.CollectionChanged += ColDefs_CollectionChanged;
        //RowDefs.CollectionChanged += RowDefs_CollectionChanged;

        //Debug.WriteLine("loading bindablegrid");

        this.UpdateLayout();

    }

    public static readonly StyledProperty<RowDefinitions> RowDefsProperty = AvaloniaProperty.Register<BindableGrid, RowDefinitions>(nameof(RowDefs), defaultValue: []);
    public static readonly StyledProperty<ColumnDefinitions> ColDefsProperty = AvaloniaProperty.Register<BindableGrid, ColumnDefinitions>(nameof(ColDefs), defaultValue: []);
    public RowDefinitions RowDefs { get => GetValue(RowDefsProperty); set => SetValue(RowDefsProperty, value); }
    public ColumnDefinitions ColDefs { get => GetValue(ColDefsProperty); set => SetValue(ColDefsProperty, value); }

    //private void ColDefs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    //{

    //    Debug.WriteLine("coll changed");

    //    if (e.NewItems != null)
    //        foreach (ColumnDefinition cdef in e.NewItems)
    //            ColumnDefinitions.Add(cdef);
    //    if (e.OldItems != null)
    //        foreach (ColumnDefinition cdef in e.OldItems)
    //            ColumnDefinitions.Remove(cdef);
                
    //}

    //private void RowDefs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    //{
    //    if (e.NewItems != null)
    //        foreach (RowDefinition rdef in e.NewItems)
    //            RowDefinitions.Add(rdef);

    //    if (e.OldItems != null)
    //        foreach (RowDefinition rdef in e.OldItems)
    //            RowDefinitions.Remove(rdef);
    //}


}
