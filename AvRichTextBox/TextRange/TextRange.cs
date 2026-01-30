using System.ComponentModel;

namespace AvRichTextBox;

public class TextRange : INotifyPropertyChanged, IDisposable
{
   public event PropertyChangedEventHandler? PropertyChanged;
   private void InvokeProperty(PropertyChangedEventArgs pceArgs) { PropertyChanged?.Invoke(this, pceArgs); }
   private static readonly PropertyChangedEventArgs StartChangedArgs = new(nameof(Start));
   private static readonly PropertyChangedEventArgs EndChangedArgs = new(nameof(End));


   internal delegate void Start_ChangedHandler(TextRange sender, int newStart);
   internal event Start_ChangedHandler? Start_Changed;
   internal delegate void End_ChangedHandler(TextRange sender, int newEnd);
   internal event End_ChangedHandler? End_Changed;

   public override string ToString() => $"{Start} → {End}";

   public TextRange(FlowDocument flowdoc, int start, int end)
   {
      if (end < start) throw new AvaloniaInternalException("TextRange not valid (start must be less than end)");

      this.Start = start;
      this.End = end;
      myFlowDoc = flowdoc;
      myFlowDoc.TextRanges.Add(this);

   }

   internal FlowDocument myFlowDoc;
   public int Length  => End - Start;
 
   public int Start { get;  set { if (field != value) { field = value; Start_Changed?.Invoke(this, value); InvokeProperty(StartChangedArgs); } } }
   public int End { get; set { if (field != value) { field = value; End_Changed?.Invoke(this, value); InvokeProperty(EndChangedArgs); } } }
   internal void UpdateStart() { InvokeProperty(StartChangedArgs); }
   internal void UpdateEnd() { InvokeProperty(EndChangedArgs); }

   internal Paragraph StartParagraph = null!;
   internal Paragraph EndParagraph = null!;

   internal Rect PrevCharRect;
   internal Rect StartRect { get; set; }
   internal Rect EndRect { get; set; }
   internal bool IsAtEndOfLineSpace = false;
   internal bool IsAtEndOfLine = false;
   internal bool IsAtLineBreak = false;

   internal bool BiasForwardStart = true;
   internal bool BiasForwardEnd = true;
   public void CollapseToStart() { End = Start;  }
   public void CollapseToEnd() { Start = End ; }


   internal void CheckLineBreaks()
   {
      GetStartInline();
      IsAtLineBreak = false;

      if (GetStartPar() is not Paragraph startPar) return;
      IEditable? startInline = null;

      foreach (IEditable inline in startPar.Inlines)
      {
         if (BiasForwardStart)
         {
            IEditable? startInlineReal = startPar.Inlines.LastOrDefault(ied => startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= Start);
            startInline = startPar.Inlines.LastOrDefault(ied => !ied.IsLineBreak && startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= Start);
            IsAtLineBreak = startInline != startInlineReal;
         }
         else
         {
            if (Start == startPar.StartInDoc)
               startInline = startPar.Inlines.FirstOrDefault();
            else
            {
               startInline = startPar.Inlines.LastOrDefault(ied => startPar.StartInDoc + ied.TextPositionOfInlineInParagraph < Start);
               IEditable? startInlineUpToLineBreak = startPar.Inlines.LastOrDefault(ied => !ied.IsLineBreak && startPar.StartInDoc + ied.TextPositionOfInlineInParagraph < Start);
               if (startInline != null && startInline.IsLineBreak)
                  startInline = myFlowDoc.GetNextInline(startInline) ?? startInline;
               IsAtLineBreak = startInline != startInlineUpToLineBreak;
            }
         }
      }

   }

   internal IEditable? GetStartInline()
   {
      
      if (GetStartPar() is not Paragraph startPar) return null;

      IEditable? startInline = null;
      IsAtLineBreak = false;

      if (BiasForwardStart)
      {
         IEditable? startInlineReal = startPar.Inlines.LastOrDefault(ied => startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= Start);
         startInline = startPar.Inlines.LastOrDefault(ied => !ied.IsLineBreak && startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= Start);
         IsAtLineBreak = startInline != startInlineReal;
         //Debug.WriteLine("calculating isatlinebreak biasforwardstart");
      }
      else
      {
         if (Start == startPar.StartInDoc)
            startInline = startPar.Inlines.FirstOrDefault();
         else
         {
            //Debug.WriteLine("calculating isatlinebreak - OTHER");
            startInline = startPar.Inlines.LastOrDefault(ied => startPar.StartInDoc + ied.TextPositionOfInlineInParagraph < Start);
            IEditable? startInlineUpToLineBreak = startPar.Inlines.LastOrDefault(ied => !ied.IsLineBreak && startPar.StartInDoc + ied.TextPositionOfInlineInParagraph < Start);
            if (startInline != null && startInline.IsLineBreak)
               startInline = myFlowDoc.GetNextInline(startInline) ?? startInline;
            IsAtLineBreak = startInline != startInlineUpToLineBreak;
         }
      }

      return startInline;

   }


   internal IEditable? GetEndInline()
   {
      if (GetEndPar() is not Paragraph endPar) return null;

      IEditable? endInline = null;

      //if (trange.BiasForwardStart && trange.Length == 0)
      if (BiasForwardStart)
         endInline = endPar.Inlines.LastOrDefault(ied => endPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= End);
      else
         endInline = endPar.Inlines.LastOrDefault(ied => endPar.StartInDoc + ied.TextPositionOfInlineInParagraph < End);

      return endInline;
   }


   public Paragraph? GetStartPar()
   {
      if (myFlowDoc.Blocks.OfType<Paragraph>().LastOrDefault(p => p.StartInDoc <= Start) is not Paragraph startPar) return null;

      ////Check if start at end of last paragraph (cannot span from end of a paragraph)
      //if (startPar != myFlowDoc.Blocks.OfType<Paragraph>().Last() && startPar.EndInDoc == Start)
      //   startPar = myFlowDoc.Blocks.OfType<Paragraph>().FirstOrDefault(p => myFlowDoc.Blocks.IndexOf(p) > myFlowDoc.Blocks.IndexOf(startPar));

      return startPar;

   }

   public Paragraph? GetEndPar()
   {
      return myFlowDoc.Blocks.LastOrDefault(b => b.IsParagraph && b.StartInDoc < End) as Paragraph;  // less than to keep within end of paragraph
      
   }

   public object? GetFormatting(AvaloniaProperty avProp)
   {
      object? formatting = null;
      if (myFlowDoc == null) return null;
      if (GetStartInline() is IEditable currentInline)
         formatting = GetFormattingInline(avProp, currentInline);
      
      return formatting;
   }


   internal static object? GetFormattingInline(AvaloniaProperty avProperty, IEditable inline)
   {
      object? returnValue = null;

      if (inline is EditableRun run)
      {
         switch (avProperty.Name)
         {
            case "Bold": returnValue = run.FontWeight; break;
            case "FontFamily": returnValue = run.FontFamily; break;
            case "FontStyle": returnValue = run.FontStyle; break;
            case "TextDecorations": returnValue = run.TextDecorations; break;
            case "FontSize": returnValue = run.FontSize; break;
            case "Background": returnValue = run.Background; break;
            case "Foreground": returnValue = run.Foreground; break;
            case "FontStretch": returnValue = run.FontStretch; break;
            case "BaselineAlignment": returnValue = run.BaselineAlignment; break;
         }
      }

      return returnValue;
   }


   public void ApplyFormatting(AvaloniaProperty avProp, object value)
   {
      if (myFlowDoc == null) return;
      if (Length < 1) return;
      if (this.Text == "") return;

      myFlowDoc.ApplyFormattingRange(avProp, value, this);

      BiasForwardStart = false;
      BiasForwardEnd = false;
      
   }

   internal string GetText()
   {
      if (myFlowDoc == null) return "";
      return myFlowDoc.GetText(this);
   }
 
   public string Text
   {
      get => GetText();
      set => myFlowDoc.SetRangeToText(this, value);
   }

   public void Dispose()
   {    
      Dispose(true);
      GC.SuppressFinalize(this);
    
   }

   private bool _disposed = false;
   protected virtual void Dispose(bool disposing)
   {
      if (_disposed)
         return;

      if (disposing)
      {
         StartParagraph = null!;
         EndParagraph = null!;

         Start_Changed = null;
         End_Changed = null;
         this.Start = 0; this.End = 0;
         myFlowDoc.TextRanges.Remove(this);
         myFlowDoc = null!;

      }
      _disposed = true;
   }



}

