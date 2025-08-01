﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AvRichTextBox;

public partial class FlowDocument : AvaloniaObject, INotifyPropertyChanged
{
   public new event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public delegate void ScrollInDirection_Handler(int direction);
   internal event ScrollInDirection_Handler? ScrollInDirection;

   public delegate void SelectionChanged_Handler(TextRange selection);
   public event SelectionChanged_Handler? Selection_Changed;

   public delegate void UpdateRTBCaret_Handler();
   internal event UpdateRTBCaret_Handler? UpdateRTBCaret;
   
   private Thickness _PagePadding = new (0);
   public Thickness PagePadding { get => _PagePadding; set {  _PagePadding = value; NotifyPropertyChanged(nameof(PagePadding)); } }

   internal bool IsEditable { get; set; } = true;

   readonly SolidColorBrush caretBrush = new(Colors.Cyan, 0.55);

   internal ObservableCollection<IUndo> Undos { get; set; } = [];

   internal List<TextRange> TextRanges = [];

   public void ScrollFlowDocInDirection(int direction) { ScrollInDirection?.Invoke(direction); }

   public List<Paragraph> GetSelectedParagraphs => Blocks.Where(b=> b.StartInDoc <= Selection.Start && b.EndInDoc >= Selection.End).Select(b=>(Paragraph)b).ToList();

   public ObservableCollection<Block> Blocks { get; set; } = [];

   //public static readonly StyledProperty<ObservableCollection<Block>> BlocksProperty =
   //AvaloniaProperty.Register<FlowDocument, ObservableCollection<Block>>(nameof(Blocks), [], defaultBindingMode: BindingMode.TwoWay);

   //public ObservableCollection<Block> Blocks
   //{
   //   get => GetValue(BlocksProperty);
   //   set { SetValue(BlocksProperty, value); }
   //}

   public string Text => string.Join("", Blocks.ToList().ConvertAll(b => string.Join("", b.Text + "\r")));
   
   public int DocEndPoint => ((Paragraph)Blocks.Last()).EndInDoc;

   public TextRange Selection { get; set; }

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

   internal void UpdateSelection()
   {
      UpdateBlockAndInlineStarts(Selection.StartParagraph);

      Selection.StartParagraph.CallRequestInlinesUpdate();
      Selection.GetStartInline();
      Selection.StartParagraph.CallRequestTextLayoutInfoStart();

      Selection.EndParagraph.CallRequestInlinesUpdate();
      Selection.GetEndInline();
      Selection.EndParagraph.CallRequestTextLayoutInfoEnd();

      //Selection.StartParagraph.CallRequestTextBoxFocus();
      
   }


   public FlowDocument()
   {

      Selection = new TextRange(this, 0, 0);
      Selection.Start_Changed += SelectionStart_Changed;
      Selection.End_Changed += SelectionEnd_Changed;

      NewDocument();

      DefineFormatRunActions();

      //this.PropertyChanged += FlowDocument_PropertyChanged;

   }

   private void FlowDocument_PropertyChanged(object? sender, PropertyChangedEventArgs e)
   {
      //Debug.WriteLine("property name = " + e.PropertyName);

      if (e.PropertyName == "Blocks")
      {
         //Debug.WriteLine("FlowDoc property changed: Blocks changed");
      }
   }

   internal void NewDocument()
   {
      ClearDocument();

      Paragraph newpar = new();
      EditableRun newerun = new("");
      newpar.Inlines.Add(newerun);
      Blocks.Add(newpar);

      InitializeDocument();

   }

   internal void ClearDocument()
   {
      Blocks.Clear();
      
      for (int tRangeNo = TextRanges.Count - 1; tRangeNo >= 0; tRangeNo--)
      {
         if (!TextRanges[tRangeNo].Equals(Selection))
            TextRanges[tRangeNo].Dispose();
      }

      this.PagePadding = new Thickness(0);

      Undos.Clear();

   }


   internal void InitializeDocument()
   {
      Selection.Start = 0;  //necessary
      Selection.CollapseToStart();

      InitializeParagraphs();

      UpdateRTBCaret?.Invoke();


   }

   internal async void InitializeParagraphs()
   {
      UpdateBlockAndInlineStarts(0);

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      SelectionStart_Changed(Selection, 0);
      SelectionEnd_Changed(Selection, 0);

      Selection.UpdateStart();
      Selection.UpdateEnd();

      await Task.Delay(70);  // For caret

      Paragraph firstPar = (Paragraph)Blocks[0];
      firstPar.CallRequestTextBoxFocus();
      firstPar.CallRequestTextLayoutInfoStart();
      firstPar.CallRequestTextLayoutInfoEnd();

   }

   internal void SelectionStart_Changed(TextRange selRange, int newStart)
   {

      Paragraph startPar = GetContainingParagraph(newStart);
      selRange.StartParagraph = startPar;
      startPar.SelectionStartInBlock = newStart - startPar.StartInDoc;
      startPar.CallRequestTextLayoutInfoStart();
      IEditable startInline = selRange.GetStartInline();

      UpdateSelectedParagraphs();

      if (ShowDebugger)
         UpdateDebuggerSelectionParagraphs();

      //Make sure end is not less than start
      if (Selection.Length > 0)
         if (selRange.StartParagraph.SelectionEndInBlock < selRange.StartParagraph.SelectionStartInBlock)
            selRange.StartParagraph.SelectionEndInBlock = selRange.StartParagraph.SelectionStartInBlock;

      //if (selRange.StartParagraph != null)
      //   selRange.StartParagraph.CallRequestTextLayoutInfoStart();

      //Debug.WriteLine("startpar text? = " + selRange.StartParagraph?.Text + "\n________________");

      //Selection.StartParagraph.CallRequestInlinesUpdate();
      Selection.GetStartInline();
      Selection.StartParagraph.CallRequestTextLayoutInfoStart();
      Selection_Changed?.Invoke(Selection);

   }

   internal void SelectionEnd_Changed(TextRange selRange, int newEnd)
   {
            
      selRange.EndParagraph = GetContainingParagraph(newEnd);
      
      selRange.EndParagraph.SelectionEndInBlock = newEnd - selRange.EndParagraph.StartInDoc;
    
      selRange.EndParagraph.CallRequestTextLayoutInfoEnd();
      selRange.GetEndInline();

      UpdateSelectedParagraphs();

      if (ShowDebugger)
         UpdateDebuggerSelectionParagraphs();

      //Make sure end is not less than start
      if (Selection.Length > 0)
         if (selRange.EndParagraph.SelectionEndInBlock < selRange.EndParagraph.SelectionStartInBlock)
            selRange.EndParagraph.SelectionStartInBlock = selRange.EndParagraph.SelectionEndInBlock;


      //Selection.EndParagraph.CallRequestInlinesUpdate();
      Selection.GetEndInline();
      Selection.EndParagraph.CallRequestTextLayoutInfoEnd();
      Selection_Changed?.Invoke(Selection);

   }

   internal void UpdateSelectedParagraphs()
   {
      SelectionParagraphs.Clear();
      SelectionParagraphs.AddRange(Blocks.Where(p => p.StartInDoc + p.BlockLength > Selection.Start && p.StartInDoc <= Selection.End).ToList().ConvertAll(bb => (Paragraph)bb));
   }

   internal string GetText(TextRange tRange)
   {

      List<IEditable> rangeInlines = GetRangeInlines(tRange);
      return string.Join("", rangeInlines.ToList().ConvertAll(il => il.InlineText));

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

