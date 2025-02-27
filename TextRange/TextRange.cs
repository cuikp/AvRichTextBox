using Avalonia;
using Avalonia.Input;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
   private int _Start;
   public int Start { get => _Start; set { if (_Start != value) { _Start = value;  Start_Changed?.Invoke(this, value); NotifyPropertyChanged(nameof(Start)); } } }
      
   private int _End;
   public int End { get => _End; set { if (_End != value) { _End = value; End_Changed?.Invoke(this, value); NotifyPropertyChanged(nameof(End)); } } }

   internal void UpdateStart() { NotifyPropertyChanged(nameof(Start)); }
   internal void UpdateEnd() { NotifyPropertyChanged(nameof(End)); }

   internal Paragraph StartParagraph = null!;
   internal Paragraph EndParagraph = null!;

   internal Rect PrevCharRect;
   internal Rect StartRect { get; set; }
   internal Rect EndRect { get; set; }
   internal bool IsAtEndOfLineSpace = false;
   internal bool IsAtEndOfLine = false;

   internal bool BiasForward = true;
   public void CollapseToStart() { End = Start;  }
   public void CollapseToEnd() { Start = End ; }


   internal IEditable GetStartInline()
   {
      Paragraph? startPar = GetStartPar();
      if (startPar == null) return null!;

      //Debug.WriteLine("\n**startPar=" + startPar.Text);

      foreach (IEditable ied in startPar!.Inlines) ied.IsStartInline = false;

      IEditable startInline = null!;
      if (BiasForward)
         startInline = startPar.Inlines.LastOrDefault(ied => startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= Start)!;
      else
      {
         if (Start - startPar.StartInDoc == 0)
            startInline = startPar.Inlines.FirstOrDefault()!;
         else
            startInline = startPar.Inlines.LastOrDefault(ied => startPar.StartInDoc + ied.TextPositionOfInlineInParagraph < Start)!;
      }

#if DEBUG
      foreach (Block b in myFlowDoc.Blocks)
      {
         if (b.IsParagraph)
         {
            foreach (IEditable ied in ((Paragraph)b).Inlines)
               ied.IsStartInline = false;
            if (startInline != null)
               startInline.IsStartInline = this == myFlowDoc.Selection;
         }
      }

#endif

      return startInline!;

   }


   internal IEditable GetEndInline()
   {
      Paragraph? endPar = GetEndPar();
      if (endPar == null) return null!;

      IEditable endInline = null!;

      //if (trange.BiasForward && trange.Length == 0)
      if (BiasForward)
         endInline = endPar.Inlines.LastOrDefault(ied => endPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= End)!;
      else
         endInline = endPar.Inlines.LastOrDefault(ied => endPar.StartInDoc + ied.TextPositionOfInlineInParagraph < End)!;

#if DEBUG
      foreach (Block b in myFlowDoc.Blocks)
      {
         if (b.IsParagraph)
         {
            foreach (IEditable ied in ((Paragraph)b).Inlines)
               ied.IsEndInline = false;
            if (endInline != null)
               endInline.IsEndInline = this == myFlowDoc.Selection;
         }
      }
#endif

      return endInline!;
   }


   internal Paragraph? GetStartPar()
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

   internal Paragraph? GetEndPar()
   {
      return myFlowDoc.Blocks.LastOrDefault(b => b.IsParagraph && b.StartInDoc < End)! as Paragraph;  // less than to keep within emd of paragraph

   }

   public void ApplyFormatting(AvaloniaProperty avProp, object value)
   {
      if (myFlowDoc == null) return;
      myFlowDoc.ApplyFormattingRange(avProp, value, this);
      BiasForward = false;
   }

   //public void Delete()
   //{
   //   if (myFlowDoc == null) return;
   //   if (!this.Equals(myFlowDoc.Selection))
   //   myFlowDoc.DeleteRange(this);
   //}

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

   //internal void SaveXaml(Stream stream, bool asXamlPackage)
   //{
   //   byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes(GetDocXaml(asXamlPackage));
   //   stream.Write(stringBytes, 0, stringBytes.Length);
   //}

   //internal void LoadXaml(Stream stream)
   //{
   //   byte[] readBytes = new byte[stream.Length];
   //   stream.Write(readBytes, 0, readBytes.Length);
   //   string xamlString = System.Text.Encoding.UTF8.GetString(readBytes);
      
   //   ProcessXamlString(xamlString);

   //}


}

