using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AvRichTextBox;

public partial class FlowDocument : INotifyPropertyChanged
{
   public event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public delegate void ScrollInDirection_Handler(int direction);
   internal event ScrollInDirection_Handler? ScrollInDirection;

   private Thickness _PagePadding = new Thickness(5);
   public Thickness PagePadding { get => _PagePadding; set {  _PagePadding = value; NotifyPropertyChanged(nameof(PagePadding)); } }

   internal bool IsEditable { get; set; } = true;

   SolidColorBrush cursorBrush = new SolidColorBrush(Colors.Cyan, 0.55);

   internal List<IUndo> Undos { get; set; } = [];

   internal List<TextRange> TextRanges = [];


   public ObservableCollection<Block> Blocks { get; set; } = [];
   internal ObservableCollection<Paragraph> SelectionParagraphs { get; set; } = [];

   public string Text => string.Join("", Blocks.ToList().ConvertAll(b => string.Join("", b.Text + "\r")));
   public TextRange Selection { get; set; }

   Dictionary<AvaloniaProperty, FormatRuns> formatRunsActions;
   Dictionary<AvaloniaProperty, FormatRun> formatRunActions;

   public FlowDocument()
   {
      Selection = new TextRange(this, 0, 0);
      Selection.Start_Changed += SelectionStart_Changed;
      Selection.End_Changed += SelectionEnd_Changed;

      formatRunsActions = new Dictionary<AvaloniaProperty, FormatRuns>
       {
           { Inline.FontWeightProperty, ApplyBoldRuns },
           { Inline.FontStyleProperty, ApplyItalicRuns },
           { Inline.TextDecorationsProperty, ApplyUnderlineRuns },
           { Inline.FontSizeProperty, ApplyFontSizeRuns },
           { Inline.BackgroundProperty, ApplyHighlightingRuns }

       };
      
      
      formatRunActions = new Dictionary<AvaloniaProperty, FormatRun>
       {
           { Inline.FontWeightProperty, ApplyBoldRun },
           { Inline.FontStyleProperty, ApplyItalicRun },
           { Inline.TextDecorationsProperty, ApplyUnderlineRun },
           { Inline.FontSizeProperty, ApplyFontSizeRun },
           { Inline.BackgroundProperty, ApplyHighlightingRun }

       };

   }

   internal void CloseDocument()
   {

      Blocks.Clear();

      for (int tRangeNo = TextRanges.Count - 1; tRangeNo >= 0; tRangeNo--)
      {
         if (!TextRanges[tRangeNo].Equals(Selection))
            TextRanges[tRangeNo].Dispose();
      }


      Undos.Clear();
            
      Paragraph newpar = new();
      EditableRun newerun = new("");
      newpar.Inlines.Add(newerun);
      Blocks.Add(newpar);

   }

   internal void InitializeDocument()
   {
      InitializeParagraphs();
                  
   }

   internal void InitializeParagraphs()
   {
      UpdateBlockAndInlineStarts(0);

      Selection.BiasForward = true;
      SelectionStart_Changed(Selection, 0);
      SelectionEnd_Changed(Selection, 0);

      Selection.StartParagraph.RequestInlinesUpdate = true;
      Selection.UpdateStart();
      Selection.UpdateEnd();
      
      ((Paragraph)Blocks[0]).RequestTextBoxFocus = true;

      UpdateBlockAndInlineStarts(0);

   }

   private void UpdateSelectionParagraphs()
   {
      SelectionParagraphs.Clear();
      SelectionParagraphs.AddRange(Blocks.Where(p => p.StartInDoc + p.BlockLength > Selection.Start && p.StartInDoc <= Selection.End).ToList().ConvertAll(bb=>(Paragraph)bb));
   }

   internal void SelectionStart_Changed(TextRange selRange, int newStart)
   {
      
      selRange.StartParagraph = GetContainingParagraph(newStart);
      selRange.StartParagraph.SelectionStartInBlock = newStart - selRange.StartParagraph.StartInDoc;
      selRange.StartParagraph.UpdateTextLayoutInfoStart();
      selRange.GetStartInline(); //?????????????
      UpdateSelectionParagraphs();

      //Make sure end is not less than start
      if (Selection.Length > 0)
         if (selRange.StartParagraph.SelectionEndInBlock < selRange.StartParagraph.SelectionStartInBlock)
            selRange.StartParagraph.SelectionEndInBlock = selRange.StartParagraph.SelectionStartInBlock;


   }

   internal void SelectionEnd_Changed(TextRange selRange, int newEnd)
   {
      //Debug.WriteLine("END CHANGE");

      selRange.EndParagraph = GetContainingParagraph(newEnd);
      selRange.EndParagraph.SelectionEndInBlock = newEnd - selRange.EndParagraph.StartInDoc;
      selRange.EndParagraph.UpdateTextLayoutInfoEnd();
      selRange.GetEndInline();
      UpdateSelectionParagraphs();

      //Make sure end is not less than start
      if (Selection.Length > 0)
         if (selRange.EndParagraph.SelectionEndInBlock < selRange.EndParagraph.SelectionStartInBlock)
            selRange.EndParagraph.SelectionStartInBlock = selRange.EndParagraph.SelectionEndInBlock;

   }

   internal string GetText(TextRange tRange)
   {
      List<IEditable> rangeInlines = GetRangeInlines(tRange);
      return string.Join("", rangeInlines.ToList().ConvertAll(il=> il.myParagraph!.Inlines.IndexOf(il) == il.myParagraph.Inlines.Count - 1 ? il.InlineText + "\r" : il.InlineText));

   }

   internal List<Block> GetRangeBlocks(TextRange trange)
   {
      //return Blocks.Where(b => b.IsParagraph && ((Paragraph)b).StartInDoc <= trange.End && b.StartInDoc + b.BlockLength - 1 >= trange.Start).ToList().ConvertAll(bb=>(Paragraph)bb);
      return Blocks.Where(b=> b.StartInDoc <= trange.End && b.StartInDoc + b.BlockLength - 1 >= trange.Start).ToList();
   }

   internal Paragraph GetContainingParagraph(int charIndex) => (Paragraph)Blocks.LastOrDefault(b => b.IsParagraph && ((Paragraph)b).StartInDoc <= charIndex)!;

   internal void UpdateBlockAndInlineStarts(int fromBlockIndex)
   {
      int parSum = fromBlockIndex == 0 ? 0 : Blocks[fromBlockIndex - 1].StartInDoc + Blocks[fromBlockIndex - 1].BlockLength;
      for (int parIndex = fromBlockIndex; parIndex < Blocks.Count; parIndex++)
      {
         Blocks[parIndex].StartInDoc = parSum;
         parSum += (Blocks[parIndex].BlockLength);

         if (Blocks[parIndex].IsParagraph)
            ((Paragraph)Blocks[parIndex]).UpdateEditableRunPositions();
      }
   }

   internal void UpdateBlockAndInlineStarts(Block thisBlock)
   {
      int fromBlockIndex = Blocks.IndexOf(thisBlock);
      int parSum = fromBlockIndex == 0 ? 0 : Blocks[fromBlockIndex - 1].StartInDoc + Blocks[fromBlockIndex - 1].BlockLength;
      for (int parIndex = fromBlockIndex; parIndex < Blocks.Count; parIndex++)
      {
         Blocks[parIndex].StartInDoc = parSum;
         parSum += (Blocks[parIndex].BlockLength);

         if (Blocks[parIndex].IsParagraph)
            ((Paragraph)Blocks[parIndex]).UpdateEditableRunPositions();
      }
   }


   internal void ResetSelectionLengthZero(Paragraph currPar)
   {
      int StartParIndex = Blocks.IndexOf(Selection!.StartParagraph);
      int EndParIndex = Blocks.IndexOf(Selection!.EndParagraph);
      foreach (Paragraph p in Blocks.Where(pp => { int pindex = Blocks.IndexOf(pp); return pindex >= StartParIndex && pindex <= EndParIndex; }))
      {
         if (p != currPar)
            p.ClearSelection();
      }

   }

   internal void UpdateTextRanges(int editCharIndexStart, int offset)
   {
      List<TextRange> toRemoveRanges = [];
      
      int editCharIndexEnd = offset == 1 ? editCharIndexStart : editCharIndexStart - offset;

      foreach (TextRange trange in TextRanges)
      {
         if (trange.Equals(this.Selection)) continue;  //Don't update the selection range

         if (trange.Start >= editCharIndexStart && trange.End <= editCharIndexEnd)
            { toRemoveRanges.Add(trange); continue; }

         if (trange.Start >= editCharIndexStart)
         {
            if (trange.Start >= editCharIndexEnd)
               trange.Start += offset;
            else
               trange.Start = editCharIndexStart;
         }
            
         if (trange.End >= editCharIndexStart)
         {
            if (trange.End >= editCharIndexEnd)
               trange.End += offset;
            else
               trange.End = editCharIndexStart;
         }

         if (trange.Start > trange.End)
            trange.End = trange.Start;
      }

      for (int trangeNo = toRemoveRanges.Count - 1; trangeNo >=0; trangeNo--)
      {
         if (!toRemoveRanges[trangeNo].Equals(Selection))
            toRemoveRanges[trangeNo].Dispose();
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

