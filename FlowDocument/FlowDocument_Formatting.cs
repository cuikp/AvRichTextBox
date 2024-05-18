using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AvRichTextBox;

public partial class FlowDocument
{

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
         IEditable startInline = Selection.GetStartInline();
         if (startInline != Selection.StartParagraph.Inlines.Last() && startInline.GetRangeStartInInline(Selection) == startInline.InlineText.Length)
         {
            IEditable nextInline = Selection.StartParagraph.Inlines[Selection.StartParagraph.Inlines.IndexOf(startInline) + 1];
            bool nextRunItalic = nextInline.GetType() == typeof(EditableRun) && ((EditableRun)nextInline).FontStyle == FontStyle.Italic;
            InsertRunMode = (ItalicOn != nextRunItalic);
            Selection.BiasForward = !InsertRunMode;
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
         IEditable startInline = Selection.GetStartInline();

         if (startInline != Selection.StartParagraph.Inlines.Last() && startInline.GetRangeStartInInline(Selection) == startInline.InlineText.Length)
         {
            IEditable nextInline = Selection.StartParagraph.Inlines[Selection.StartParagraph.Inlines.IndexOf(startInline) + 1];
            bool nextRunBold = nextInline.GetType() == typeof(EditableRun) && ((EditableRun)nextInline).FontWeight == FontWeight.Bold;
            InsertRunMode = (BoldOn != nextRunBold);
            Selection.BiasForward = !InsertRunMode;
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

         IEditable startInline = Selection.GetStartInline();

         if (startInline != Selection.StartParagraph.Inlines.Last() && startInline.GetRangeStartInInline(Selection) == startInline.InlineText.Length)
         {
            IEditable nextInline = Selection.StartParagraph.Inlines[Selection.StartParagraph.Inlines.IndexOf(startInline) + 1];
            bool nextRunUnderlined = nextInline.GetType() == typeof(EditableRun) && ((EditableRun)nextInline).TextDecorations == TextDecorations.Underline;
            InsertRunMode = (UnderliningOn != nextRunUnderlined);
            Selection.BiasForward = !InsertRunMode;
         }
      }
      else
         Selection.ApplyFormatting(Inline.TextDecorationsProperty, TextDecorations.Underline);
   }


   internal void ApplyFormattingRange(AvaloniaProperty avProperty, object value, TextRange textRange)
   {
      List<IEditable> newInlines = CreateNewInlinesForRange(textRange);

      List<IEditablePropertyAssociation> propertyAssociations = [];
         
      foreach (IEditable inline in newInlines)
      {
         IEditablePropertyAssociation iedPropAssoc = new(inline, null!, null!);
         propertyAssociations.Add(iedPropAssoc);
         if (inline.GetType() == typeof(EditableRun))
         {
            if (formatRunActions.TryGetValue(avProperty, out var runAction))
               iedPropAssoc.FormatRun = runAction;
            iedPropAssoc.PropertyValue = ((EditableRun)inline).GetValue(avProperty)!;
         }
      }
      
      Undos.Add(new ApplyFormattingUndo(this, propertyAssociations, Selection.Start, textRange));


      if (formatRunsActions.TryGetValue(avProperty, out var runsAction))
         runsAction(newInlines, value);
      else
         throw new NotSupportedException($"Formatting for {avProperty.Name} is not supported.");

      UpdateBlockAndInlineStarts(Blocks.IndexOf(Blocks.LastOrDefault(p => p.StartInDoc <= textRange.Start)!));
      
      foreach (Paragraph p in GetRangeBlocks(textRange).Where(b=>b.IsParagraph))
         p.RequestInlinesUpdate = true;


      Selection.BiasForward = true;
      Selection.StartParagraph = GetContainingParagraph(Selection.Start);
      Selection.StartParagraph.SelectionStartInBlock = Selection.Start - Selection.StartParagraph.StartInDoc;
      Selection.EndParagraph.SelectionEndInBlock = Selection.End - Selection.EndParagraph.StartInDoc;
      Selection.GetEndInline();
      Selection.GetEndInline();
      UpdateSelectionParagraphs();
      

   }
  
   
   internal void ApplyFormattingInline(FormatRun formatRun, IEditable inlineItem, object value)
   {
      formatRun(inlineItem, value);
      Selection.BiasForward = true;

   }

   internal delegate void FormatRun(IEditable ied, object value);
   private void ApplyBoldRun(IEditable ied, object fontWeight) { if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).FontWeight = (FontWeight)fontWeight; } }
   private void ApplyItalicRun(IEditable ied, object fontStyle) { if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).FontStyle = (FontStyle)fontStyle; } }
   private void ApplyUnderlineRun(IEditable ied, object textDecoration) { if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).TextDecorations = (TextDecorationCollection)textDecoration; } }
   private void ApplyFontSizeRun(IEditable ied, object value) { if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).FontSize = (double)value; } }
   private void ApplyHighlightingRun(IEditable ied, object value) { if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).Background = (ImmutableSolidColorBrush)value; } }


   internal delegate void FormatRuns(List<IEditable> ieds, object value);
   
   private void ApplyBoldRuns(List<IEditable> ieds, object value)
   {
      FontWeight applyFontWeight = FontWeight.Normal;
      if (value is FontWeight.Bold)
         applyFontWeight = (ieds.Where(ar => ar.GetType() == typeof(EditableRun) && ((EditableRun)ar).FontWeight == FontWeight.Normal).Count() == 0) ?
            FontWeight.Normal : FontWeight.Bold;
      foreach (IEditable ied in ieds)
         if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).FontWeight = applyFontWeight; }
   }

   private void ApplyItalicRuns(List<IEditable> ieds, object value)
   {
      FontStyle applyFontStyle = FontStyle.Normal;
      if (value is FontStyle.Italic)
         applyFontStyle = (ieds.Where(ar => ar.GetType() == typeof(EditableRun) && ((EditableRun)ar).FontStyle == FontStyle.Normal).Count() == 0) ? FontStyle.Normal : FontStyle.Italic;
      foreach (IEditable ied in ieds)
         if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).FontStyle = applyFontStyle; }
   }

   private void ApplyUnderlineRuns(List<IEditable> ieds, object value)
   {
      TextDecorationCollection applyTextDecs = null!;
      if (value == TextDecorations.Underline)
         applyTextDecs = (ieds.Where(ar => ar.GetType() == typeof(EditableRun) && ((EditableRun)ar).TextDecorations == null).Count() == 0) ? null! : TextDecorations.Underline;
      foreach (IEditable ied in ieds)
         if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).TextDecorations = applyTextDecs; }
   }
   
   private void ApplyFontSizeRuns(List<IEditable> ieds, object value)
   {
      foreach (IEditable ied in ieds)
         if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).FontSize = (double)value; }
   }
      
   private void ApplyHighlightingRuns(List<IEditable> ieds, object value)
   {
      foreach (IEditable ied in ieds)
         if (ied.GetType() == typeof(EditableRun)) { ((EditableRun)ied).Background = (ImmutableSolidColorBrush)value; }
   }



}

