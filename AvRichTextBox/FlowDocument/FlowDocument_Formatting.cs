using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.Diagnostics;

namespace AvRichTextBox;

public partial class FlowDocument
{
   Dictionary<AvaloniaProperty, FormatRunsAction> formatRunsActions = [];
   Dictionary<AvaloniaProperty, FormatRunAction> formatRunActions = [];

   private void DefineFormatRunActions()
   {
      formatRunsActions = new Dictionary<AvaloniaProperty, FormatRunsAction>
       {
           { Inline.FontFamilyProperty, ApplyFontFamilyRuns },
           { Inline.FontWeightProperty, ApplyBoldRuns },
           { Inline.FontStyleProperty, ApplyItalicRuns },
           { Inline.TextDecorationsProperty, ApplyTextDecorationRuns },
           { Inline.FontSizeProperty, ApplyFontSizeRuns },
           { Inline.BackgroundProperty, ApplyBackgroundRuns },
           { Inline.ForegroundProperty, ApplyForegroundRuns },
           { Inline.FontStretchProperty, ApplyFontStretchRuns },
           { Inline.BaselineAlignmentProperty, ApplyBaselineAlignmentRuns }
       };


      formatRunActions = new Dictionary<AvaloniaProperty, FormatRunAction>
       {
           { Inline.FontFamilyProperty, ApplyFontFamilyRun },
           { Inline.FontWeightProperty, ApplyBoldRun },
           { Inline.FontStyleProperty, ApplyItalicRun },
           { Inline.TextDecorationsProperty, ApplyTextDecorationRun },
           { Inline.FontSizeProperty, ApplyFontSizeRun },
           { Inline.BackgroundProperty, ApplyBackgroundRun },
           { Inline.ForegroundProperty, ApplyForegroundRun },
           { Inline.FontStretchProperty, ApplyFontStretchRun },
           { Inline.BaselineAlignmentProperty, ApplyBaselineAlignmentRun }
       };


   }


   bool BoldOn = false;
   bool ItalicOn = false;
   bool UnderliningOn = false;

   private bool InsertRunMode = false;
   private ToggleFormatRun? toggleFormatRun;

   private delegate void ToggleFormatRun(IEditable ied);
   private void ToggleApplyBold(IEditable ied) { if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).FontWeight = BoldOn ? FontWeight.Bold : FontWeight.Normal; } }
   private void ToggleApplyItalic(IEditable ied) { if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).FontStyle = ItalicOn ? FontStyle.Italic : FontStyle.Normal; } }
   private void ToggleApplyUnderline(IEditable ied) { if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).TextDecorations = UnderliningOn ? TextDecorations.Underline : null; } }

   internal void ToggleItalic()
   {
      if (Selection.Length == 0)
      {
         ItalicOn = !ItalicOn;
         toggleFormatRun = ToggleApplyItalic;
         InsertRunMode = true;
         if (Selection.GetStartInline() is IEditable startInline)
         {
            if (startInline != Selection.StartParagraph.Inlines.Last() && GetCharPosInInline(startInline, Selection.Start) == startInline.InlineText.Length)
            {
               IEditable nextInline = Selection.StartParagraph.Inlines[Selection.StartParagraph.Inlines.IndexOf(startInline) + 1];
               bool nextRunItalic = nextInline.GetType() == typeof(EditableRun) && ((EditableRun)nextInline).FontStyle == FontStyle.Italic;
               InsertRunMode = (ItalicOn != nextRunItalic);
               Selection.BiasForwardStart = !InsertRunMode;
            }
         }
      }
      else
         Selection.ApplyFormatting(Inline.FontStyleProperty, FontStyle.Italic);

   }

   internal void ToggleBold()
   {
      if (Selection.Length == 0)
      {
         toggleFormatRun = ToggleApplyBold;
         BoldOn = !BoldOn;
         InsertRunMode = true;
         if (Selection.GetStartInline() is IEditable startInline)
         {
            if (startInline != Selection.StartParagraph.Inlines.Last() && GetCharPosInInline(startInline, Selection.Start) == startInline.InlineText.Length)
            {
               IEditable nextInline = Selection.StartParagraph.Inlines[Selection.StartParagraph.Inlines.IndexOf(startInline) + 1];
               bool nextRunBold = nextInline.GetType() == typeof(EditableRun) && ((EditableRun)nextInline).FontWeight == FontWeight.Bold;
               InsertRunMode = (BoldOn != nextRunBold);
               Selection.BiasForwardStart = !InsertRunMode;
            }
         }
      }
      else
         Selection.ApplyFormatting(Inline.FontWeightProperty, FontWeight.Bold);

   }

   internal void ToggleUnderlining()
   {
      if (Selection.Length == 0)
      {
         toggleFormatRun = ToggleApplyUnderline;
         UnderliningOn = !UnderliningOn;
         InsertRunMode = true;

         if (Selection.GetStartInline() is IEditable startInline)
         {
            if (startInline != Selection.StartParagraph.Inlines.Last() && GetCharPosInInline(startInline, Selection.Start) == startInline.InlineText.Length)
            {
               IEditable nextInline = Selection.StartParagraph.Inlines[Selection.StartParagraph.Inlines.IndexOf(startInline) + 1];
               bool nextRunUnderlined = nextInline.GetType() == typeof(EditableRun) && ((EditableRun)nextInline).TextDecorations == TextDecorations.Underline;
               InsertRunMode = (UnderliningOn != nextRunUnderlined);
               Selection.BiasForwardStart = !InsertRunMode;
            }
         }
      }
      else
         Selection.ApplyFormatting(Inline.TextDecorationsProperty, TextDecorations.Underline);
   }


   internal void ApplyFormattingRange(AvaloniaProperty avProperty, object value, TextRange textRange)
   {
      (int idLeft, int idRight) edgeIds;
      List<IEditable> newInlines = GetRangeInlinesAndAddToDoc(textRange, out edgeIds);
      
      //Debug.WriteLine("\nnewlines created:\n" + string.Join("\n", newInlines.ConvertAll(il=> il.InlineText + " :: " + il.Id + "\nEdge ids = L: " + edgeIds.idLeft + ", R: " + edgeIds.idRight)));  


      //create property association for undo
      List<IEditablePropertyAssociation> propertyAssociations = [];
      foreach (EditableRun erun in newInlines.OfType<EditableRun>())
      {
         IEditablePropertyAssociation iedPropAssoc = new(erun.MyParagraphId, erun.Id, null!, null!);
         propertyAssociations.Add(iedPropAssoc);

         if (formatRunActions.TryGetValue(avProperty, out var runAction))
            iedPropAssoc.FormatRun = runAction;
         if (erun.GetValue(avProperty) is object o)
            iedPropAssoc.PropertyValue = o;
      }

      Undos.Add(new ApplyFormattingUndo(this, propertyAssociations, edgeIds, Selection.Start, textRange));


      if (formatRunsActions.TryGetValue(avProperty, out var applyToRunsAction))
         applyToRunsAction(newInlines, value);
      else
         throw new NotSupportedException($"Formatting for {avProperty.Name} is not supported.");

      UpdateBlockAndInlineStarts(Blocks.IndexOf(Blocks.LastOrDefault(p => p.StartInDoc <= textRange.Start)!));
      
      foreach (Paragraph p in GetRangeBlocks(textRange).Where(b=>b.IsParagraph))
         p.CallRequestInlinesUpdate();

      
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;
      Selection.StartParagraph = GetContainingParagraph(Selection.Start);
      Selection.StartParagraph.SelectionStartInBlock = Selection.Start - Selection.StartParagraph.StartInDoc;
      Selection.EndParagraph.SelectionEndInBlock = Selection.End - Selection.EndParagraph.StartInDoc;
    
      UpdateSelectedParagraphs();

      if (ShowDebugger)
         UpdateDebuggerSelectionParagraphs();
      

   }
  
   
   internal void ApplyFormattingInline(FormatRunAction? formatRun, IEditable inlineItem, object value)
   {
      formatRun?.Invoke(inlineItem, value);
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

   }

   internal delegate void FormatRunAction(IEditable ied, object value);
   private void ApplyFontFamilyRun(IEditable ied, object fontfamily ) { if (ied is EditableRun edrun) { edrun.FontFamily = (FontFamily)fontfamily; } }
   private void ApplyBoldRun(IEditable ied, object fontWeight) { if (ied is EditableRun edrun) { edrun.FontWeight = (FontWeight)fontWeight; } }
   private void ApplyItalicRun(IEditable ied, object fontStyle) { if (ied is EditableRun edrun) { edrun.FontStyle = (FontStyle)fontStyle; } }
   private void ApplyTextDecorationRun(IEditable ied, object textDecoration) { if (ied is EditableRun edrun) { edrun.TextDecorations = (TextDecorationCollection)textDecoration; } }
   private void ApplyFontSizeRun(IEditable ied, object fontsize) { if (ied is EditableRun edrun) { edrun.FontSize = (double)fontsize; } }
   private void ApplyBackgroundRun(IEditable ied, object background) { if (ied is EditableRun edrun) { edrun.Background = (SolidColorBrush)background; } }
   private void ApplyForegroundRun(IEditable ied, object foreground) { if (ied is EditableRun edrun) { edrun.Foreground = (SolidColorBrush)foreground; } }
   private void ApplyFontStretchRun(IEditable ied, object fontstretch) { if (ied is EditableRun edrun) { edrun.FontStretch = (FontStretch)fontstretch; } }
   private void ApplyBaselineAlignmentRun(IEditable ied, object baselinealignment) { if (ied is EditableRun edrun) { edrun.BaselineAlignment = (BaselineAlignment)baselinealignment ; } }


   internal delegate void FormatRunsAction(List<IEditable> ieds, object value);
   
   private void ApplyFontFamilyRuns(List<IEditable> ieds, object fontfamily)
   {
      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.FontFamily = (FontFamily)fontfamily; }
   }
      
   private void ApplyBoldRuns(List<IEditable> ieds, object fontweight)
   {
      FontWeight applyFontWeight = FontWeight.Normal;
      if (fontweight is FontWeight.Bold)
         applyFontWeight = (!ieds.Where(ar => ar is EditableRun edrun && edrun.FontWeight == FontWeight.Normal).Any()) ? FontWeight.Normal : FontWeight.Bold;
      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.FontWeight = applyFontWeight; }
   }

   private void ApplyItalicRuns(List<IEditable> ieds, object fontstyle)
   {
      FontStyle applyFontStyle = FontStyle.Normal;
      if (fontstyle is FontStyle.Italic)
         applyFontStyle = (!ieds.Where(ar => ar is EditableRun edrun && edrun.FontStyle == FontStyle.Normal).Any()) ? FontStyle.Normal : FontStyle.Italic;
      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.FontStyle = applyFontStyle; }
   }

   private void ApplyTextDecorationRuns(List<IEditable> ieds, object textdecoration)
   {
      TextDecorationCollection? applyTextDecs = null;
      if (textdecoration == TextDecorations.Underline)
         applyTextDecs = (!ieds.Where(ar => ar is EditableRun edrun && edrun.TextDecorations == null).Any()) ? null! : TextDecorations.Underline;
      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.TextDecorations = applyTextDecs; }
   }
   
   private void ApplyFontSizeRuns(List<IEditable> ieds, object fontsize)
   {
      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.FontSize = (double)fontsize; }
   }
      
   private void ApplyBackgroundRuns(List<IEditable> ieds, object background)
   {
      if (background.GetType() != typeof(SolidColorBrush))
         throw new Exception("Background must be set with a SolidColorBrush");

      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.Background = (SolidColorBrush)background; }
   }

   private void ApplyForegroundRuns(List<IEditable> ieds, object foreground)
   {
      if (foreground.GetType() != typeof(SolidColorBrush))
         throw new Exception("Foreground must be set with a SolidColorBrush");

      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.Foreground = (SolidColorBrush)foreground; }
   }

   private void ApplyFontStretchRuns(List<IEditable> ieds, object fontstretch)
   {
      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.FontStretch = (FontStretch)fontstretch; }
   }

   private void ApplyBaselineAlignmentRuns(List<IEditable> ieds, object baselinealignment)
   {
      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.BaselineAlignment = (BaselineAlignment)baselinealignment; }
   }

   internal void ResetInsertFormatting()
   {
      InsertRunMode = false;
      BoldOn = false;
      ItalicOn = false;
      UnderliningOn = false;

   }


}

