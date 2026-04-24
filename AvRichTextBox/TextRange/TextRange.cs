using DocumentFormat.OpenXml.Math;
using System.ComponentModel;
using System.Text;
using static AvRichTextBox.XamlConversions;

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
 
   public int Start { get;  set {  field = value; Start_Changed?.Invoke(this, value); InvokeProperty(StartChangedArgs);  } }
   public int End { get; set {  field = value; End_Changed?.Invoke(this, value); InvokeProperty(EndChangedArgs);  } }

   internal Paragraph StartParagraph = null!;
   internal Paragraph EndParagraph = null!;

   internal Rect PrevCharRect;
   internal Rect StartRect { get; set; }
   internal Rect EndRect { get; set; }
   internal bool IsAtEndOfLineSpace = false;
   internal bool IsAtEndOfLine = false;
   internal bool IsAtLineBreak = false;
   internal bool IsAtCellBreak = false;

   internal bool BiasForwardStart = true;
   internal bool BiasForwardEnd = true;
   public void CollapseToStart() { End = Start;  }
   public void CollapseToEnd() { Start = End ; }

    
   internal int CalculateStartInInline(IEditable inline)
   {
      return this.Start - (StartParagraph.StartInDoc + inline.TextPositionOfInlineInParagraph);
   }

   internal int CalculateEndInInline(IEditable inline)
   {
      return this.End - (EndParagraph.StartInDoc + inline.TextPositionOfInlineInParagraph);
   }

   internal IEditable? GetStartInline()
   {
      IsAtLineBreak = false;
      IsAtCellBreak = false;

      if (GetStartPar() is not Paragraph startPar) return null;
      IEditable? startInline = null;
      
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

      //Check if at cellbreak
      if (startInline != null && startInline.IsTableCellInline && startInline.IsLastInlineOfParagraph)
            IsAtCellBreak = CalculateStartInInline(startInline) >= startInline.InlineText.Length;

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


      //Check if at cellbreak
      if (endInline != null && endInline.IsTableCellInline && endInline.IsLastInlineOfParagraph)
         IsAtCellBreak = CalculateEndInInline(endInline) >= endInline.InlineText.Length;

      return endInline;
   }

   public Paragraph? GetStartPar() => myFlowDoc.AllParagraphs.LastOrDefault(p => p.StartInDoc <= Start);
   public Paragraph? GetEndPar() => myFlowDoc.AllParagraphs.LastOrDefault(p => p.StartInDoc < End);

   public object? GetFormatting(AvaloniaProperty avProp)
   {
      object? formatting = null;
      if (myFlowDoc == null) return null;
      if (GetStartInline() is IEditable currentInline)
         formatting = GetFormattingOfInline(avProp, currentInline);
      
      return formatting;
   }

   internal static object? GetFormattingOfInline(AvaloniaProperty avProperty, IEditable inline)
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


   public void Save(Stream stream, ContentDataFormat dataFormat)
   {      
      switch (dataFormat)
      {
         case ContentDataFormat.Xaml:

            StringBuilder rangeXamlBuilder = new(SectionTextDefault);
            rangeXamlBuilder.Append(GetParagraphRunsXaml(myFlowDoc.GetRangeInlines(this), false));
            rangeXamlBuilder.Append("</Section>");
            byte[] xamlStringBytes = Encoding.UTF8.GetBytes(rangeXamlBuilder.ToString());
            stream.Write(xamlStringBytes, 0, xamlStringBytes.Length);
            break;

         case ContentDataFormat.XamlPackage:

            break;

         case ContentDataFormat.Text:

            byte[] textStringBytes = Encoding.UTF8.GetBytes(this.Text);
            stream.Write(textStringBytes, 0, textStringBytes.Length);
            break;

         case ContentDataFormat.Rtf:

            byte[] rtfStringBytes = Encoding.UTF8.GetBytes(RtfConversions.GetRtfFromInlines(myFlowDoc.GetRangeInlines(this)));
            stream.Write(rtfStringBytes, 0, rtfStringBytes.Length);
            break;
      }
      

   }

   public void Load(Stream stream, ContentDataFormat dataFormat)
   {
      (int idLeft, int idRight) edgeIds = myFlowDoc.DeleteRange(this, false, false);
      
      byte[] streamBytes = new byte[stream.Length];
      stream.ReadExactly(streamBytes);
      string readString = Encoding.UTF8.GetString(streamBytes, 0, streamBytes.Length);
      List<int> addedBlockIds = []; // dummy or use for undo

      switch (dataFormat)
      {
         case ContentDataFormat.Xaml:

            myFlowDoc.InsertXaml(streamBytes, StartParagraph, EndParagraph, this, myFlowDoc.Blocks.IndexOf(StartParagraph), addedBlockIds);
            break;

         case ContentDataFormat.XamlPackage:

            break;

         case ContentDataFormat.Text:

            this.Text = readString;
            break;

         case ContentDataFormat.Rtf:
                        
            myFlowDoc.InsertRTF(streamBytes, StartParagraph, this, myFlowDoc.Blocks.IndexOf(StartParagraph), addedBlockIds);
            break;
      }


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

