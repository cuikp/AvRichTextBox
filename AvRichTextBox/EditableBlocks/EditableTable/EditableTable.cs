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
    internal delegate void MouseMoveHandler(EditableTable sender, Cursor tableCursor);
    internal event MouseMoveHandler? MouseMove;

    internal delegate void MouseLeaveHandler(EditableTable sender);
    internal event MouseLeaveHandler? MouseLeave;

    private const double ResizeGripSize = 5;
    private const double MinColumnWidth = 24;
    private const double MinRowHeight = 24;

    private readonly Cursor _ewResizeCursor = new(StandardCursorType.SizeWestEast);
    private readonly Cursor _nsResizeCursor = new(StandardCursorType.SizeNorthSouth);

    public bool IsEditable { get; set; } = true;

    public EditableTable()
    {
        Loaded += EditableTable_Loaded;
        SizeChanged += EditableTable_SizeChanged;
        PropertyChanged += EditableTable_PropertyChanged;
    }

    //private void EditableTable_MouseLeave(EditableTable sender) { throw new NotImplementedException(); }
    //private void EditableTable_MouseMove(EditableTable sender, Cursor tableCursor) { throw new NotImplementedException(); }

    private void EditableTable_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (this.DataContext is not Table thisTable) return;

        switch (e.Property.Name)
        {
            case "Margin":
                this.UpdateLayout();
                break;
            
            case "BorderThickness":
                
                this.UpdateLayout();
                thisTable.UpdateColAndRowPoints();

                this.Width = thisTable.ColDefs.Sum(cd => cd.Width.Value) + thisTable.BorderThickness.Left + thisTable.BorderThickness.Right;
                this.Height = thisTable.RowDefs.Sum(cd => cd.Height.Value) + thisTable.BorderThickness.Top + thisTable.BorderThickness.Bottom;

                break;
        }

    }

    private void EditableTable_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (this.DataContext is not Table table)
            return;

        table.UpdateColAndRowPoints();

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

    internal void UpdateBordersCanvas() { Dispatcher.UIThread.Post(() => { bordersCanvas.InvalidateVisual(); }); }

    private void Table_ColDefsChanged(Table sender) { bordersCanvas.UpdateColPoints(sender.ColDefs); }
    //private void Table_RowDefsChanged(Table sender) { bordersCanvas.UpdateRowPoints(sender.RowDefs); this.UpdateLayout(); this.InvalidateArrange(); this.InvalidateVisual();  }
    private void Table_RowDefsChanged(Table sender) { bordersCanvas.UpdateRowPoints(sender.RowDefs);  }


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

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        MouseLeave?.Invoke(this);

    }

    bool borderResized = false;

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (DataContext is not Table table)
            return;

        MouseMove?.Invoke(this, this.Cursor!);

        Point position = e.GetPosition(this);
        if (_resizeMode != ResizeMode.None)
        {
            ResizeTable(table, position);

            borderResized = true;
            
            e.Handled = true;

            table.UpdateColAndRowPoints();

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
    private Point _resizeStartPoint;
    private ResizeMode _resizeMode;
    private int _resizeIndex = -1;
    private double _resizeStartPrimarySize;
    private double _resizeStartSecondarySize;
    private double _resizePrimarySize;
    private double _resizeSecondarySize;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsEditable || DataContext is not Table table)
            return;

        borderResized = false;
        tableWidthChange = 0;
        DisableUndoStack = true;
        

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
            if (_resizeIndex < table.ColDefs.Count - 1)
                _resizeStartSecondarySize = table.ColDefs[_resizeIndex + 1].Width.Value;
        }
        else
        {
            _resizeStartPrimarySize = table.RowDefs[_resizeIndex].Height.Value;
            if (_resizeIndex < table.RowDefs.Count - 1)
            {
                _resizeStartSecondarySize = table.RowDefs[_resizeIndex + 1].Height.Value;
                //minLowerCellPadding = table.Cells.Where(c => c.RowNo == _resizeIndex + 1).ToList().Min(cc => cc.Padding.Top + cc.Padding.Bottom);
            }
        }

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        DisableUndoStack = false;
              
        if (!IsEditable || DataContext is not Table table || _resizeMode == ResizeMode.None)
            return;

        if (borderResized && !DisableUndoStack)
        {
            table.MyFlowDoc.Undos.Add(_resizeMode switch
            {
                ResizeMode.Column => new AdjustTableColumnSizeUndo(table.Id, _resizeIndex, _resizeStartPrimarySize, _resizePrimarySize, shiftWasOnAtPress, _resizeStartSecondarySize, _resizeSecondarySize, table.MyFlowDoc),
                _ => new AdjustTableRowSizeUndo(table.Id, _resizeIndex, _resizeStartPrimarySize, _resizePrimarySize, shiftWasOnAtPress, _resizeStartSecondarySize, _resizeSecondarySize, table.MyFlowDoc)
            });

            //Resize table if necessary
            table.Width += tableWidthChange;
        }


        if (shiftWasOnAtPress)
        {
            DisableUndoStack = true;
            this.Background = keepTableBackground;
            table.BorderBrush = keepTableBorderBrush;
            shiftWasOnAtPress = false;
            DisableUndoStack = false;

        }

        _resizeMode = ResizeMode.None;
        _resizeIndex = -1;
        e.Pointer.Capture(null);
        e.Handled = true;
        _PointerPressedOnBorder = false;
       

    }


    private void ResizeTable(Table table, Point position)
    {
        table.MyFlowDoc.Redos.Clear();

        if (_resizeMode == ResizeMode.Column)
        {
            bool isRightEdge = _resizeIndex == table.ColDefs.Count - 1;

            double delta = position.X - _resizeStartPoint.X;

            double newPrimarySize = _resizeStartPrimarySize + delta;
            _resizePrimarySize = shiftWasOnAtPress || isRightEdge ? newPrimarySize : Math.Max(MinColumnWidth, newPrimarySize);

            double newSecondarySize = _resizeStartSecondarySize - (_resizePrimarySize - _resizeStartPrimarySize);
            _resizeSecondarySize = shiftWasOnAtPress || isRightEdge ? newSecondarySize : Math.Max(MinColumnWidth, newSecondarySize);

            _resizePrimarySize = Math.Max(MinColumnWidth, _resizeStartPrimarySize + (_resizeStartSecondarySize - _resizeSecondarySize));
            double netChange = _resizePrimarySize - _resizeStartPrimarySize;

            table.ColDefs[_resizeIndex].Width = new GridLength(_resizePrimarySize, GridUnitType.Pixel);

            if (shiftWasOnAtPress || isRightEdge)
            {   // don't shorten column at right, just resize table accordingly (only on mouse up)
                tableWidthChange = netChange;
            }
            else
            {   // column at right is shortened
                table.ColDefs[_resizeIndex + 1].Width = new GridLength(_resizeSecondarySize, GridUnitType.Pixel);
            }
        }
        else if (_resizeMode == ResizeMode.Row)
        {
            bool isBottomEdge = _resizeIndex == table.RowDefs.Count - 1;

            double delta = position.Y - _resizeStartPoint.Y;

            double maxHeight = shiftWasOnAtPress || isBottomEdge ? Double.MaxValue : _resizeStartPrimarySize + _resizeStartSecondarySize - 40;
            double minHeight = 40;
            double newPrimaryHeight = _resizeStartPrimarySize + delta;
            if (newPrimaryHeight < maxHeight && newPrimaryHeight > minHeight)
            {
                table.RowDefs[_resizeIndex].MinHeight = newPrimaryHeight;
                table.RowDefs[_resizeIndex].Height = new GridLength(newPrimaryHeight, GridUnitType.Pixel);
                _resizePrimarySize = newPrimaryHeight;

                if (shiftWasOnAtPress || isBottomEdge)
                { }  // don't reduce height in lower cells, just let table resize accordingly
                else
                {   // lower row's height is shortened
                    double newSecondaryHeight = _resizeStartSecondarySize - delta;
                    table.RowDefs[_resizeIndex + 1].Height = new GridLength(newSecondaryHeight, GridUnitType.Pixel);
                    _resizeSecondarySize = newSecondaryHeight;

                }
            }
        }


        bordersCanvas.InvalidateVisual();
        table.MyFlowDoc.UpdateSelection();
        table.MyFlowDoc.UpdateCaret();

    }

    private static ResizeHit GetResizeHit(Table table, Point position)
    {
        double x = table.BorderThickness.Left;
        for (int index = 0; index < table.ColDefs.Count; index++)
        {
            x += table.ColDefs[index].Width.Value;
            if (Math.Abs(position.X - x) <= ResizeGripSize)
                return new ResizeHit(ResizeMode.Column, index);
        }

        double y = table.BorderThickness.Top;
        for (int index = 0; index < table.RowDefs.Count; index++)
        {
            y += table.RowDefs[index].Height.Value;
            if (Math.Abs(position.Y - y) <= ResizeGripSize)
                return new ResizeHit(ResizeMode.Row, index);
        }

        return new ResizeHit(ResizeMode.None, -1);
    }

    internal static readonly StyledProperty<ObservableCollection<EditableCell>> CellsProperty = AvaloniaProperty.Register<EditableTable, ObservableCollection<EditableCell>>(nameof(Cells), defaultValue: []);
    internal ObservableCollection<EditableCell> Cells { get => GetValue(CellsProperty); set => SetValue(CellsProperty, value); }

    //private void Cells_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    //{

    //    this.UpdateLayout();
    //}

}

internal readonly record struct ResizeHit(ResizeMode Mode, int Index);

internal enum ResizeMode
{
    None,
    Column,
    Row
}


