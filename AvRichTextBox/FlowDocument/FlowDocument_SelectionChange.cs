
using DocumentFormat.OpenXml.Math;

namespace AvRichTextBox;

public partial class FlowDocument
{
   internal void UpdateSelection()
   {
      UpdateBlockAndInlineStarts(Selection.StartParagraph);

      Selection.StartParagraph.CallRequestInlinesUpdate();
      Selection.StartParagraph.CallRequestTextLayoutInfoStart();
      Selection.EndParagraph.CallRequestInlinesUpdate();
      Selection.EndParagraph.CallRequestTextLayoutInfoEnd();
      Selection.StartParagraph.CallRequestTextBoxFocus();

   }

   internal void SelectionStart_Changed(TextRange selRange, int newStart)
   {  //Debug.WriteLine("SELECTION START CHANGED");

      switch (GetContainingParagraph(newStart))
      {
         case Paragraph startPar:

            selRange.StartParagraph = startPar;
            startPar.SelectionStartInBlock = newStart - startPar.StartInDoc;
            startPar.CallRequestTextLayoutInfoStart();
            break;
      }

      UpdateSelectedParagraphs();

      //Make sure end is not less than start
      if (selRange.Length > 0)
         if (selRange.StartParagraph.SelectionEndInBlock < selRange.StartParagraph.SelectionStartInBlock)
            selRange.StartParagraph.SelectionEndInBlock = selRange.StartParagraph.SelectionStartInBlock;


      selRange.GetStartInline();

      selRange.StartParagraph?.CallRequestTextLayoutInfoStart();
      SelectionChanged?.Invoke(selRange);



   }

   internal void SelectionEnd_Changed(TextRange selRange, int newEnd)
   {

      switch (GetContainingParagraph(newEnd))
      {
         case Paragraph endPar:

            selRange.EndParagraph = endPar;
            endPar.SelectionEndInBlock = newEnd - endPar.StartInDoc;
            endPar.CallRequestTextLayoutInfoEnd();
            break;
      }

      UpdateSelectedParagraphs();

      ////Make sure start is not greater than end
      //if (Selection.Length > 0)
      //   if (selRange.EndParagraph.SelectionEndInBlock < selRange.EndParagraph.SelectionStartInBlock)
      //      selRange.EndParagraph.SelectionStartInBlock = selRange.EndParagraph.SelectionEndInBlock;

      selRange.GetEndInline();
      
      selRange.EndParagraph?.CallRequestTextLayoutInfoEnd();
      SelectionChanged?.Invoke(selRange);

   }


}

