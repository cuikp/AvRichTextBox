using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Collections.ObjectModel;

namespace AvRichTextBox;

public partial class EditableTable : ItemsControl
{
    public delegate void MouseMoveHandler(EditableTable sender, Cursor tableCursor);
    public event MouseMoveHandler? MouseMove;

    public delegate void MouseLeaveHandler(EditableTable sender);
    public event MouseLeaveHandler? MouseLeave;

    private const double ResizeGripSize = 5;
    private const double MinColumnWidth = 24;
    private const double MinRowHeight = 24;
    
    private readonly Cursor _ewResizeCursor = new(StandardCursorType.SizeWestEast);
    private readonly Cursor _nsResizeCursor = new(StandardCursorType.SizeNorthSouth);

    private Point _resizeStartPoint;
    private ResizeMode _resizeMode;
    private int _resizeIndex = -1;
    private double _resizeStartPrimarySize;
    private double _resizeStartSecondarySize;

    public bool IsEditable { get; set; } = true;

    public EditableTable()
    {
        Loaded += EditableTable_Loaded;

        SizeChanged += EditableTable_SizeChanged;
    }

    private void EditableTable_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (this.DataContext is not Table table)
            return;
        
        bordersCanvas?.UpdateColPoints(table.ColDefs);
        bordersCanvas?.UpdateRowPoints(table.RowDefs);

        this.UpdateLayout();

        //Recalculate all paragraph layouts in all cells
        foreach (Cell c in table.Cells)
        {
            foreach (Paragraph cPar in c.CellBlocks.OfType<Paragraph>())
            {
                Dispatcher.UIThread.Post(() =>
                {
                    cPar.CallRequestTextLayoutInfoStart();
                    cPar.CallRequestTextLayoutInfoEnd();
                });
            }
        }

        bordersCanvas?.InvalidateVisual();
        table.MyFlowDoc.InvokeSelectionChanged();
                
    }

    BordersCanvas bordersCanvas = null!;

    private void EditableTable_Loaded(object? sender, RoutedEventArgs e)
    {
        this.UpdateLayout();
        this.Cursor = Cursor.Default;


        if (this.DataContext is not Table table)
            return;

        table.ColDefsChanged += Table_ColDefsChanged;
        table.RowDefsChanged += Table_RowDefsChanged;

        bordersCanvas = new BordersCanvas(table) { IsHitTestVisible = false, ClipToBounds = false };
        AdornerLayer.SetAdorner(this, bordersCanvas);
        AdornerLayer.SetIsClipEnabled(bordersCanvas, false);

    }

    private void Table_ColDefsChanged(Table sender)
    {
        bordersCanvas.UpdateColPoints(sender.ColDefs);
    }

    private void Table_RowDefsChanged(Table sender)
    {
        bordersCanvas.UpdateRowPoints(sender.RowDefs);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        MouseLeave?.Invoke(this);
        
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        //bool shiftOn = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        if (DataContext is not Table table)
            return;

        MouseMove?.Invoke(this, this.Cursor!);
        
        Point position = e.GetPosition(this);
        if (_resizeMode != ResizeMode.None)
        {
            ResizeTable(table, position);

            e.Handled = true;
            bordersCanvas.UpdateColPoints(table.ColDefs);
            bordersCanvas.UpdateRowPoints(table.RowDefs);
            //bordersCanvas.InvalidateVisual();
            return;
        }

        if (!_PointerPressedOnBorder)
        {
            ResizeHit hit = GetResizeHit(table, position);
            Cursor = hit.Mode switch
            {
                ResizeMode.Column => _ewResizeCursor,
                ResizeMode.Row => _nsResizeCursor,
                //_ => null
                _ => Cursor.Default
            };
        }

    }

    internal bool _PointerPressedOnBorder = false;
    private double tableWidthChange = 0;
    private IBrush? keepTableBackground;
    private IBrush keepTableBorderBrush = null!;
    private bool shiftWasOnAtPress = false;
    private double minCurrentCellPadding = 0;
    private double minLowerCellPadding = 0;
    private List<double> origCellPaddings = [];

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsEditable || DataContext is not Table table)
            return;
         
        tableWidthChange = 0;
    
        shiftWasOnAtPress = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (shiftWasOnAtPress)
        {
            keepTableBackground = this.Background;
            keepTableBorderBrush = table.BorderBrush;
            this.Background = Brushes.Transparent;
            table.BorderBrush = Brushes.Transparent;
        }

        Point position = e.GetPosition(this);
        ResizeHit hit = GetResizeHit(table, position);
        if (hit.Mode == ResizeMode.None)
            return;

        _PointerPressedOnBorder = true;
        _resizeMode = hit.Mode;
        _resizeIndex = hit.Index;
        _resizeStartPoint = position;
        if (_resizeMode == ResizeMode.Column)
        {
            _resizeStartPrimarySize = table.ColDefs[_resizeIndex].Width.Value;
            if (_resizeIndex <  table.ColDefs.Count - 1)
                _resizeStartSecondarySize = table.ColDefs[_resizeIndex + 1].Width.Value;
        }
        else
        {            
            origCellPaddings = [.. table.Cells.Where(c => c.RowNo == _resizeIndex).ToList().ConvertAll(cc => cc.Padding.Top + cc.Padding.Bottom)];
            minCurrentCellPadding = origCellPaddings.Min();
            _resizeStartPrimarySize = table.RowDefs[_resizeIndex].Height.Value;
            if (_resizeIndex < table.RowDefs.Count - 1)
            {
                _resizeStartSecondarySize = table.RowDefs[_resizeIndex + 1].Height.Value;
                minLowerCellPadding = table.Cells.Where(c => c.RowNo == _resizeIndex + 1).ToList().Min(cc => cc.Padding.Top + cc.Padding.Bottom);
            }
        }

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_resizeMode == ResizeMode.None)
            return;

        _resizeMode = ResizeMode.None;
        _resizeIndex = -1;
        e.Pointer.Capture(null);
        e.Handled = true;
        _PointerPressedOnBorder = false;

        if (!IsEditable || DataContext is not Table table)
            return;

        //Resize table if necessary
        table.Width += tableWidthChange;
        
        if (shiftWasOnAtPress)
        {
            this.Background = keepTableBackground;
            table.BorderBrush = keepTableBorderBrush;
            shiftWasOnAtPress = false;
        }

        //table.MyFlowDoc.InvokeSelectionChanged();

    }

    
    private void ResizeTable(Table table, Point position)
    {
        if (_resizeMode == ResizeMode.Column)
        {
            bool isRightEdge = _resizeIndex == table.ColDefs.Count - 1;

            double delta = position.X - _resizeStartPoint.X;
            
            double newPrimarySize = _resizeStartPrimarySize + delta;
            double primaryWidth = shiftWasOnAtPress || isRightEdge ? newPrimarySize : Math.Max(MinColumnWidth, newPrimarySize);
           
            double newSecondarySize = _resizeStartSecondarySize - (primaryWidth - _resizeStartPrimarySize);
            double secondaryWidth = shiftWasOnAtPress || isRightEdge ? newSecondarySize : Math.Max(MinColumnWidth, newSecondarySize);
            
            primaryWidth = Math.Max(MinColumnWidth, _resizeStartPrimarySize + (_resizeStartSecondarySize - secondaryWidth));
            double netChange = primaryWidth - _resizeStartPrimarySize;

            table.ColDefs[_resizeIndex].Width = new GridLength(primaryWidth, GridUnitType.Pixel);

            if (shiftWasOnAtPress || isRightEdge)
            {   // don't shorten column at right, just resize table accordingly (only on mouse up)
                tableWidthChange = netChange;
            }
            else
            {   // column at right is shortened
                table.ColDefs[_resizeIndex + 1].Width = new GridLength(secondaryWidth, GridUnitType.Pixel);
            }
        }
        else if (_resizeMode == ResizeMode.Row)
        {
            bool isBottomEdge = _resizeIndex == table.RowDefs.Count - 1;

            double delta = position.Y - _resizeStartPoint.Y;

            double maxPadding = shiftWasOnAtPress || isBottomEdge ? Double.MaxValue : minCurrentCellPadding + minLowerCellPadding;

            List<Cell> cellsToRepad = [.. table.Cells.Where(c => c.RowNo == _resizeIndex)];
            for (int cellno = 0; cellno < cellsToRepad.Count; cellno++) 
            {
                Cell cell = cellsToRepad[cellno];
                double newTotalCellVerticalPadding = Math.Max(0, Math.Min(maxPadding, origCellPaddings[cellno] + delta)) ;
                cell.Padding = new Thickness(cell.Padding.Left, newTotalCellVerticalPadding / 2, cell.Padding.Right, newTotalCellVerticalPadding / 2);
                foreach (Block b in cell.CellBlocks)
                {
                    if (b is Paragraph p)
                        p.CallRequestSizeChanged(); 
                }
            }
            
            
            if (shiftWasOnAtPress || isBottomEdge)
            { }  // don't reduce padding in lower cells, just let table resize accordingly
            else
            {   // lower cells paddings are shortened
                foreach (Cell lowerCell in table.Cells.Where(c=> c.RowNo == _resizeIndex + 1))
                {
                    double newLowerCellTotalVerticalPadding = Math.Max(0, Math.Min(maxPadding, minLowerCellPadding - delta));
                    lowerCell.Padding = new Thickness(lowerCell.Padding.Left, newLowerCellTotalVerticalPadding / 2, lowerCell.Padding.Right, newLowerCellTotalVerticalPadding / 2);

                    foreach (Block b in lowerCell.CellBlocks)
                    {
                        if (b is Paragraph p)
                            p.CallRequestSizeChanged();
                    }
                }
            }
        }
                

        bordersCanvas.InvalidateVisual();
        table.MyFlowDoc.UpdateSelection(); 
        table.MyFlowDoc.UpdateCaret();
        
    }

    private static ResizeHit GetResizeHit(Table table, Point position)
    {
        double x = 0;
        for (int index = 0; index < table.ColDefs.Count; index++)
        {
            x += table.ColDefs[index].Width.Value;
            if (Math.Abs(position.X - x) <= ResizeGripSize)
                return new ResizeHit(ResizeMode.Column, index);
        }

        double y = 0;
        for (int index = 0; index < table.RowDefs.Count; index++)
        {
            y += table.RowDefs[index].Height.Value;
            if (Math.Abs(position.Y - y) <= ResizeGripSize)
                return new ResizeHit(ResizeMode.Row, index);
        }

        return new ResizeHit(ResizeMode.None, -1);
    }

    public static readonly StyledProperty<ObservableCollection<EditableCell>> CellsProperty = AvaloniaProperty.Register<EditableTable, ObservableCollection<EditableCell>>(nameof(Cells), defaultValue: []);
    public ObservableCollection<EditableCell> Cells { get => GetValue(CellsProperty); set => SetValue(CellsProperty, value); }

    //private void Cells_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    //{

    //    this.UpdateLayout();
    //}

    ////public string GetText => string.Join("", ((Table)this.DataContext).Inlines.ToList().ConvertAll(edinline => edinline.InlineText));

}

internal readonly record struct ResizeHit(ResizeMode Mode, int Index);

internal enum ResizeMode
{
    None,
    Column,
    Row
}


