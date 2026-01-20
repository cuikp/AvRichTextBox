using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;

public class TextRange : INotifyPropertyChanged, IDisposable
{
   public event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

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
   public int Start { get;  set { if (field != value) { field = value; Start_Changed?.Invoke(this, value); NotifyPropertyChanged(nameof(Start)); } } }
   public int End { get; set { if (field != value) { field = value; End_Changed?.Invoke(this, value); NotifyPropertyChanged(nameof(End)); } } }

   internal void UpdateStart() { NotifyPropertyChanged(nameof(Start)); }
   internal void UpdateEnd() { NotifyPropertyChanged(nameof(End)); }

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


   internal IEditable GetStartInline()
   {
      
      Paragraph? startPar = GetStartPar();
      if (startPar == null) return null!;
      IEditable startInline = null!;
      IsAtLineBreak = false;

      if (BiasForwardStart)
      {
         IEditable startInlineReal = startPar.Inlines.LastOrDefault(ied => startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= Start)!;
         startInline = startPar.Inlines.LastOrDefault(ied => !ied.IsLineBreak && startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= Start)!;
         IsAtLineBreak = startInline != startInlineReal;
         //Debug.WriteLine("calculating isatlinebreak biasforwardstart");
      }
      else
      {
         if (Start - startPar.StartInDoc == 0)
            startInline = startPar.Inlines.FirstOrDefault()!;
         else
         {
            //Debug.WriteLine("calculating isatlinebreak - OTHER");
            startInline = startPar.Inlines.LastOrDefault(ied => startPar.StartInDoc + ied.TextPositionOfInlineInParagraph < Start)!;
            IEditable startInlineUpToLineBreak = startPar.Inlines.LastOrDefault(ied => !ied.IsLineBreak && startPar.StartInDoc + ied.TextPositionOfInlineInParagraph < Start)!;
            if (startInline.IsLineBreak)
               startInline = myFlowDoc.GetNextInline(startInline) ?? startInline;
            IsAtLineBreak = startInline != startInlineUpToLineBreak;
         }
      }

      return startInline!;

   }


   internal IEditable GetEndInline()
   {
      Paragraph? endPar = GetEndPar();
      if (endPar == null) return null!;

      IEditable endInline = null!;

      //if (trange.BiasForwardStart && trange.Length == 0)
      if (BiasForwardStart)
         endInline = endPar.Inlines.LastOrDefault(ied => endPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= End)!;
      else
         endInline = endPar.Inlines.LastOrDefault(ied => endPar.StartInDoc + ied.TextPositionOfInlineInParagraph < End)!;

      return endInline!;
   }


   public Paragraph? GetStartPar()
   {
      Paragraph? startPar = myFlowDoc.Blocks.LastOrDefault(b => b.IsParagraph && (b.StartInDoc <= Start))! as Paragraph;

      if (startPar != null)
      {
         //Check if start at end of last paragraph (cannot span from end of a paragraph)
         if (startPar != myFlowDoc.Blocks.Where(b => b.IsParagraph).Last() && startPar!.EndInDoc == Start)
            startPar = myFlowDoc.Blocks.FirstOrDefault(b => b.IsParagraph && myFlowDoc.Blocks.IndexOf(b) > myFlowDoc.Blocks.IndexOf(startPar))! as Paragraph;
      }

      return startPar;

   }

   public Paragraph? GetEndPar()
   {
      return myFlowDoc.Blocks.LastOrDefault(b => b.IsParagraph && b.StartInDoc < End)! as Paragraph;  // less than to keep within emd of paragraph

   }

   public object? GetFormatting(AvaloniaProperty avProp)
   {
      object? formatting = null!;
      if (myFlowDoc == null) return null!;
      IEditable currentInline = GetStartInline();
      if (currentInline != null)
         formatting = GetFormattingInline(avProp, currentInline);
      
      return formatting;
   }


   internal static object? GetFormattingInline(AvaloniaProperty avProperty, IEditable inline)
   {
      object? returnValue = null!;

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

      //try
      //{
      //   Debug.WriteLine("\napplying: " + (this.Text ?? "null"));
      //}
      //catch (Exception ex) { Debug.WriteLine("exception, length = " + this.Length + " :::start = " + this.Start + " ::: end= " + this.End + " :::"  + ex.Message); }
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

