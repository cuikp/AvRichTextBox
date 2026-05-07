using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using SkiaSharp;

namespace AvRichTextBox;

public partial class FlowDocument
{
   Dictionary<AvaloniaProperty, FormatRunsAction> formatRunsActions = [];  

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
   }


   bool BoldOn = false;
   bool ItalicOn = false;
   bool UnderliningOn = false;

   private bool InsertRunMode = false;
   private ToggleFormatRun? toggleFormatRun;

   private delegate void ToggleFormatRun(IEditable ied);
   // used for inline toggling while typing
   private void ToggleInlineApplyBold(IEditable ied) { if (ied is EditableRun erun) { erun.FontWeight = BoldOn ? FontWeight.Bold : FontWeight.Normal; } }
   private void ToggleInlineApplyItalic(IEditable ied) { if (ied is EditableRun erun) { erun.FontStyle = ItalicOn ? FontStyle.Italic : FontStyle.Normal; } }
   private void ToggleInlineApplyUnderline(IEditable ied) { if (ied is EditableRun erun) { erun.TextDecorations = UnderliningOn ? TextDecorations.Underline : null; } }


   internal void ToggleUnderlining()
   {
      if (Selection.Length == 0)
      {
         UnderliningOn = !UnderliningOn;
         toggleFormatRun = ToggleInlineApplyUnderline;

         InsertRunMode = true;

         if (Selection.StartInline is IEditable startInline)
         {
            if (startInline != Selection.StartParagraph.Inlines.Last() && GetCharPosInInline(startInline, Selection.Start) == startInline.InlineText.Length)
            {
               IEditable nextInline = Selection.StartParagraph.Inlines[Selection.StartParagraph.Inlines.IndexOf(startInline) + 1];
               bool nextRunUnderlined = nextInline is EditableRun nextrun && nextrun.TextDecorations == TextDecorations.Underline;
               InsertRunMode = (UnderliningOn != nextRunUnderlined) || nextInline is not EditableRun;
               Selection.BiasForwardStart = !InsertRunMode;
            }
         }
      }
      else
         Selection.ApplyFormatting(Inline.TextDecorationsProperty, TextDecorationLocation.Underline);
   }


   internal void ToggleItalic()
   {
      if (Selection.Length == 0)
      {
         ItalicOn = !ItalicOn;
         toggleFormatRun = ToggleInlineApplyItalic;
         InsertRunMode = true;
         if (Selection.StartInline is IEditable startInline)
         {
            if (startInline != Selection.StartParagraph.Inlines.Last() && GetCharPosInInline(startInline, Selection.Start) == startInline.InlineText.Length)
            {
               IEditable nextInline = Selection.StartParagraph.Inlines[Selection.StartParagraph.Inlines.IndexOf(startInline) + 1];
               bool nextRunItalic = nextInline is EditableRun nextrun && nextrun.FontStyle == FontStyle.Italic;
               InsertRunMode = (ItalicOn != nextRunItalic) || nextInline is not EditableRun;
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
         BoldOn = !BoldOn;
         toggleFormatRun = ToggleInlineApplyBold;
         
         InsertRunMode = true;
         if (Selection.StartInline is IEditable startInline)
         {
            if (startInline != Selection.StartParagraph.Inlines.Last() && GetCharPosInInline(startInline, Selection.Start) == startInline.InlineText.Length)
            {
               IEditable nextInline = Selection.StartParagraph.Inlines[Selection.StartParagraph.Inlines.IndexOf(startInline) + 1];
               bool nextRunBold = nextInline is EditableRun nextrun && nextrun.FontWeight == FontWeight.Bold;
               InsertRunMode = (BoldOn != nextRunBold) || nextInline is not EditableRun;
               Selection.BiasForwardStart = !InsertRunMode;
            }
         }
      }
      else
         Selection.ApplyFormatting(Inline.FontWeightProperty, FontWeight.Bold);

   }
     
   internal void ApplyFormattingRange(AvaloniaProperty avProperty, object value, TextRange textRange)
   {
      disableRunTextUndo = true;

      (List<IEditable> createdInlines, (int idLeft, int idRight) edgeIds) createdInlinesResult = GetTextRangeInlines(textRange, true);
      List<IEditable> newInlines = createdInlinesResult.createdInlines;
      (int idLeft, int idRight) edgeIds = createdInlinesResult.edgeIds;


      //Debug.WriteLine("\nnewlines created:\n" + string.Join("\n", newInlines.ConvertAll(il=> il.InlineText + " :: " + il.Id + "\nEdge ids = L: " + edgeIds.idLeft + ", R: " + edgeIds.idRight)));  

      //create property association for undo 
      List<EditablePropertyAssociation> propertyAssociations = [];
      foreach (EditableRun erun in newInlines.OfType<EditableRun>())
      {
         EditablePropertyAssociation edPropAssoc = new(erun.MyParagraphId, erun.Id, null!, null!);
         propertyAssociations.Add(edPropAssoc);

         if (formatRunsActions.TryGetValue(avProperty, out var runsAction))
            edPropAssoc.FormatRuns = runsAction;

         if (erun.GetValue(avProperty) is object o)
            edPropAssoc.PropertyValue = o;
      }

      Undos.Add(new ApplyFormattingUndo(this, propertyAssociations, edgeIds, Selection.Start, textRange));


      if (formatRunsActions.TryGetValue(avProperty, out var applyToRunsAction))
         applyToRunsAction(newInlines, value);
      else
         throw new NotSupportedException($"Formatting for {avProperty.Name} is not supported.");

      //UpdateBlockAndInlineStarts(AllParagraphs.IndexOf(AllParagraphs.LastOrDefault(p => p.StartInDoc <= textRange.Start)!));
      
      foreach (Paragraph p in GetOverlappingParagraphsInRange(textRange).OfType<Paragraph>())
         p.CallRequestInlinesUpdate();

      disableRunTextUndo = false;

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      if (GetContainingParagraph(Selection.Start) is Paragraph startPar)
      {
         Selection.StartParagraph = startPar;
         Selection.StartParagraph.SelectionStartInBlock = Selection.Start - Selection.StartParagraph.StartInDoc;
         Selection.EndParagraph.SelectionEndInBlock = Selection.End - Selection.EndParagraph.StartInDoc;
      }
    
      UpdateSelectedParagraphs();


      // Finally must update the selection rectangles/caret for some formatting changes (bold, fontsize, etc.)
      if (textRange == Selection)
      {
         Dispatcher.UIThread.Post(() =>
         {
            SelectionChanged?.Invoke(Selection);

         }, DispatcherPriority.Background);
      }

   }
     
   internal void ApplyFormattingInlines(FormatRunsAction? formatRunsAction, List<IEditable> inlineItems, object value)
   {
      formatRunsAction?.Invoke(inlineItems, value);
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

   }

   internal delegate void FormatRunsAction(List<IEditable> ieds, object value);
   
   private void ApplyFontFamilyRuns(List<IEditable> ieds, object fontfamily)
   {
      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.FontFamily = (FontFamily)fontfamily; }
   }
      
   private void ApplyBoldRuns(List<IEditable> ieds, object fontweight)
   {
      if (fontweight is not FontWeight applyFontWeight) return;

      // if all of the runs are bold, force style to Normal
      if (fontweight is FontWeight.Bold)
         applyFontWeight = ieds.All(ar => ar is EditableRun edrun && edrun.FontWeight == FontWeight.Bold) ? FontWeight.Normal : FontWeight.Bold;
      // if all of the runs are normal, force style to Bold
      else if (fontweight is FontWeight.Normal)
         applyFontWeight = ieds.All(ar => ar is EditableRun edrun && edrun.FontWeight == FontWeight.Normal) ? FontWeight.Bold : FontWeight.Normal;


      foreach (EditableRun erun in ieds.OfType<EditableRun>())
         erun.FontWeight = applyFontWeight;

   }

   private void ApplyItalicRuns(List<IEditable> ieds, object fontstyle)
   {
      if (fontstyle is not FontStyle applyFontStyle) return;

      // if all of the runs are italic, force style to Normal
      if (fontstyle is FontStyle.Italic)
         applyFontStyle = ieds.All(ar => ar is EditableRun edrun && edrun.FontStyle == FontStyle.Italic) ? FontStyle.Normal : FontStyle.Italic;
      // if all of the runs are normal, force style to Italic
      else if (fontstyle is FontStyle.Normal)
         applyFontStyle = ieds.All(ar => ar is EditableRun edrun && edrun.FontStyle == FontStyle.Normal) ? FontStyle.Italic : FontStyle.Normal;
      // otherwise (if mixed), apply italic to all runs
      foreach (EditableRun erun in ieds.OfType<EditableRun>())
            erun.FontStyle = applyFontStyle; 
      
      
   }

   private void ApplyTextDecorationRuns(List<IEditable> ieds, object textDecLocation)
   {
      if (textDecLocation is not TextDecorationLocation applyTextDecLoc) return;

      // Get all runs without this new text dec
      List<EditableRun> applyToRuns = [.. ieds.OfType<EditableRun>().Where(edrun => edrun.TextDecorations != null && edrun.TextDecorations.Any(tdec => tdec.Location == applyTextDecLoc))];
      if (applyToRuns.Count == ieds.Count)
      { // all runs contain textdec, so remove from all 
         foreach (EditableRun erun in ieds.OfType<EditableRun>())
         {
            if (erun.TextDecorations is TextDecorationCollection currentDec)
            {
               currentDec.RemoveAll(currentDec.Where(cdec => cdec.Location == applyTextDecLoc));
               if (currentDec.Count == 0) currentDec = null!;
            }
         }
      }
      else
      { // none of the runs contain textdec, or mixed, so add to all which lack it
         foreach (EditableRun erun in ieds.OfType<EditableRun>())
         {
            var currentDec = erun.TextDecorations == null ? new TextDecorationCollection() : [.. erun.TextDecorations];

            bool alreadyHasDecoration = currentDec.Any(x => x.Location == applyTextDecLoc);
            if (!alreadyHasDecoration)
               currentDec.Add(new TextDecoration() { Location = applyTextDecLoc });

            erun.TextDecorations = currentDec;
         }
      }


   }

   private void ApplyFontSizeRuns(List<IEditable> ieds, object fontsize)
   {
      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.FontSize = (double)fontsize; }
   }
      
   private void ApplyBackgroundRuns(List<IEditable> ieds, object background)
   {
      ISolidColorBrush applyBrush = Brushes.Transparent;
      if (background is ISolidColorBrush solidBrush)
         applyBrush = solidBrush;

      foreach (IEditable ied in ieds)
         if (ied is EditableRun edrun) { edrun.Background = applyBrush; }
     
   }

   private void ApplyForegroundRuns(List<IEditable> ieds, object foreground)
   {
      ISolidColorBrush applyBrush = Brushes.Transparent;
      if (foreground is SolidColorBrush solidBrush)
         applyBrush = solidBrush;
      foreach (IEditable ied in ieds)
            if (ied is EditableRun edrun) { edrun.Foreground = applyBrush; }
      
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

   internal static void CopyRunPropsToHyperlinkText(EditableRun linkRun, ref EditableHyperlink elink)
   {
      elink.Text = linkRun.Text;
      elink.FontStyle = linkRun.FontStyle;
      elink.FontWeight = linkRun.FontWeight;
      elink.TextDecorations = linkRun.TextDecorations;
      elink.FontSize = linkRun.FontSize;
      elink.FontFamily = linkRun.FontFamily;
      elink.Background = linkRun.Background;
      elink.BaselineAlignment = linkRun.BaselineAlignment;
      elink.Foreground = linkRun.Foreground;
   }

}

