using Avalonia.Data;
using Avalonia.Media;
using DynamicData;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace AvRichTextBox;

public partial class FlowDocument : AvaloniaObject
{
   public delegate void ScrollInDirection_Handler(int direction);
   internal event ScrollInDirection_Handler? ScrollInDirection;

   public delegate void SelectionChanged_Handler(TextRange selection);
   public event SelectionChanged_Handler? SelectionChanged;

   public delegate void UpdateRTBCaret_Handler();
   internal event UpdateRTBCaret_Handler? UpdateRTBCaret;
   
   internal static int InlineIdCounter { get; set => field = (value == int.MaxValue) ? 0 : value; }
   internal static int ParagraphIdCounter { get; set => field = (value == int.MaxValue) ? 0 : value; }
   internal static int TableIdCounter { get; set => field = (value == int.MaxValue) ? 0 : value; }
      
   internal bool IsEditable { get; set; } = true;

   internal ObservableCollection<IUndo> Undos { get; set; } = [];
   internal ObservableCollection<Paragraph> SelectionParagraphs { get; set; } = [];
   internal List<TextRange> TextRanges = [];

   internal bool disableRunTextUndo = false;

   public void ScrollFlowDocInDirection(int direction) { ScrollInDirection?.Invoke(direction); }

   public List<Paragraph> GetSelectedParagraphs => [.. AllParagraphs.Where(p=> p.StartInDoc <= Selection.Start && p.EndInDoc >= Selection.End).Select(b=>(Paragraph)b)];

   public static readonly StyledProperty<ObservableCollection<Block>> BlocksProperty = AvaloniaProperty.Register<FlowDocument, ObservableCollection<Block>>(nameof(Blocks), defaultBindingMode: BindingMode.TwoWay);
   public ObservableCollection<Block> Blocks
   {
      get => GetValue(BlocksProperty);
      set { SetValue(BlocksProperty, value); }
   }

   public static readonly DirectProperty<FlowDocument, Thickness> PagePaddingProperty = AvaloniaProperty.RegisterDirect<FlowDocument, Thickness>(nameof(PagePadding), o => o.PagePadding, (o, v) => o.PagePadding = v);
   public Thickness PagePadding
   {
      get;
      set => SetAndRaise(PagePaddingProperty, ref field, value);
   }

   public string Text => string.Join("", Blocks.ToList().ConvertAll(b => string.Join("", b.Text + Environment.NewLine)));
   
   public int DocEndPoint => ((Paragraph)Blocks.Last()).EndInDoc;

   public TextRange Selection { get; set; }
   internal IBrush SelectionBrush = Brushes.LightSteelBlue; 
   
   public FlowDocument()
   {
      Blocks = [];
      Selection = new TextRange(this, 0, 0);
      Selection.Start_Changed += SelectionStart_Changed;
      Selection.End_Changed += SelectionEnd_Changed;

      DefineFormatRunActions();

      this.PropertyChanged += FlowDocument_PropertyChanged;

      InlineIdCounter = 0; //reset on new flowdoc

      Blocks.CollectionChanged += Blocks_CollectionChanged;

   }

   private void FlowDocument_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
   {
      if (e.Property == BlocksProperty)
      {
         Blocks.CollectionChanged -= Blocks_CollectionChanged;
         Blocks.CollectionChanged += Blocks_CollectionChanged;
      }
   }

   private void Blocks_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   {
      foreach (Block block in Blocks)
      {
         block.MyFlowDoc = this;
         if (block is Table table)
         {
            foreach (Cell c in table.Cells)
               c.CellContent.MyFlowDoc = this;
         }
      }

      AllParagraphs = [.. GetAllParagraphs];  //update collection of all paragraphs

   }

   public void SelectAll()
   {
      Selection.Start = 0;
      Selection.End = 0;
      SelectionParagraphs.Clear();
      Selection.End = this.DocEndPoint - 1;
      EnsureSelectionContinuity();
      this.SelectionExtendMode = ExtendMode.ExtendModeRight;
   }

   public void Select(int Start, int Length)
   {
      SelectionParagraphs.Clear();

      Selection.Start = Start;
      Selection.End = Start + Length;

      EnsureSelectionContinuity();

      UpdateSelection();

   }

   internal void NewDocument()
   {
      ClearDocument();

      Paragraph newpar = new(this);
      EditableRun newerun = new("");
      newpar.Inlines.Add(newerun);
      Blocks.Add(newpar);

      InitializeDocument();

   }

   internal void CreateTestDocument()
   {
      ClearDocument();

      Paragraph newPar = new(this);
      newPar.Inlines.Add(new EditableRun("A ")  );
      newPar.Inlines.Add(new EditableRun("first") );
      newPar.Inlines.Add(new EditableRun(" H") );

  
      newPar.Inlines.Add(new EditableRun("2") { BaselineAlignment = BaselineAlignment.Subscript });
      newPar.Inlines.Add(new EditableRun("O"));
      newPar.Inlines.Add(new EditableRun("3") { BaselineAlignment = BaselineAlignment.Superscript });

      newPar.Inlines.Add(new EditableRun(" simple "));
      newPar.Inlines.Add(new EditableRun("line.") ) ;
      Blocks.Add(newPar);

      //Test Table
      Blocks.Add(new Table(5, 4, this) { BorderThickness = new(1), BorderBrush = Brushes.ForestGreen, TableAlignment = Avalonia.Layout.HorizontalAlignment.Center });

      Paragraph newPar2 = new(this);
      newPar2.Inlines.Add(new EditableRun("Some extra text after the table."));
      Blocks.Add(newPar2);


      InitializeDocument();

   }

   internal void ClearDocument()
   {
      Blocks.Clear();

      ParagraphIdCounter = 0;
      InlineIdCounter = 0;

      for (int tRangeNo = TextRanges.Count - 1; tRangeNo >= 0; tRangeNo--)
      {
         if (!TextRanges[tRangeNo].Equals(Selection))
            TextRanges[tRangeNo].Dispose();
      }

      this.PagePadding = new Thickness(0);

      Undos.Clear();

   }

   internal async void InitializeDocument()
   {

      Selection.Start = 0;  //necessary
      Selection.CollapseToStart();

      UpdateBlockAndInlineStarts(0);

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      SelectionStart_Changed(Selection, 0);
      SelectionEnd_Changed(Selection, 0);

      await Task.Delay(70);  // For caret

      if (AllParagraphs.ToList()[0] is Paragraph firstPar)
      {  //Required for initial cursor display 
         firstPar.CallRequestTextBoxFocus();
         firstPar.CallRequestTextLayoutInfoStart();
         firstPar.CallRequestTextLayoutInfoEnd();
      }

      UpdateRTBCaret?.Invoke();

   }

   internal string GetText(TextRange tRange) => string.Join("", GetRangeInlines(tRange).ConvertAll(il => il.InlineText));
   
   internal List<Table> GetFullTablesInRange(TextRange trange) => [.. Blocks.Where(b=> b is Table t && t.StartInDoc > trange.Start && t.StartInDoc + t.BlockLength - 1 < trange.End).Cast<Table>()];
   internal List<Table> GetFulTablesInRange(int start, int end) => [.. Blocks.Where(b=> b is Table t && t.StartInDoc > start && t.StartInDoc + t.BlockLength - 1 < end).Cast<Table>()];
   internal List<Paragraph> GetFullParagraphsInRange(TextRange trange) => [.. AllParagraphs.Where(b=> b.StartInDoc >= trange.Start && b.StartInDoc + b.BlockLength - 1 <= trange.End)];
   internal List<Paragraph> GetFullParagraphsInRange(int start, int end) => [.. AllParagraphs.Where(b => b.StartInDoc >= start && b.StartInDoc + b.BlockLength - 1 <= end)];

   internal List<Paragraph> GetOverlappingParagraphsInRange(TextRange trange) => [.. AllParagraphs.Where(b=> b.StartInDoc <= trange.End && b.StartInDoc + b.BlockLength - 1 >= trange.Start)];
   internal List<Paragraph> GetOverlappingParagraphsInRange(int start, int end) => [.. AllParagraphs.Where(b => b.StartInDoc <= end && b.StartInDoc + b.BlockLength - 1 >= start)];

   internal Paragraph GetContainingParagraph(int charIndex) => AllParagraphs.LastOrDefault(p=> p.StartInDoc <= charIndex) as Paragraph ?? null!;
   
   internal List<Paragraph> AllParagraphs = [];

   internal IEnumerable<Paragraph> GetAllParagraphs 
   {
      get
      {
         return Blocks.SelectMany(b =>
         {
            if (b is Paragraph p) return [p];
            if (b is Table t) return t.Cells.Select(c => c.CellContent) ?? Enumerable.Empty<Paragraph>();
            return Enumerable.Empty<Paragraph>();
         }).Cast<Paragraph>();
      }
   }


   internal ExtendMode SelectionExtendMode { get; set; }

   internal enum ExtendMode
   {
      ExtendModeNone,
      ExtendModeRight,
      ExtendModeLeft
   }


}

