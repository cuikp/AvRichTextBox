using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData;
using RtfDomParserAv;
using System.Text;
using static AvRichTextBox.FlowDocument;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

public partial class RichTextBox
{
   private static readonly ScaleTransform subscriptScaleTransform = new(0.75, 0.75);
   internal static TransformGroup SubscriptTG = new();
   internal static TransformGroup SuperscriptTG = new();

   private void ToggleItalics()
   {
      if (IsReadOnly) return;
      FlowDoc.ToggleItalic();

   }

   private void ToggleBold()
   {
      if (IsReadOnly) return;
      FlowDoc.ToggleBold();

   }

   private void ToggleUnderlining()
   {
      if (IsReadOnly) return;
      FlowDoc.ToggleUnderlining();

   }

   
   private void CopyToClipboard()
   {      
      if (DisableUserCopy) return;
     
      var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
      if (clipboard == null) return;
      //create rtf string
      List<IEditable> newInlines = FlowDoc.GetRangeInlines(FlowDoc.Selection);
      string rtfString = RtfConversions.GetRtfFromInlines(newInlines);
      
      var dataTransfer = new DataTransfer();

      // Rtf format
      var richTextFormat = DataFormat.CreateBytesPlatformFormat("Rich Text Format");
      byte[] rtfbytes = Encoding.ASCII.GetBytes(rtfString + "\0");
      dataTransfer.Add(DataTransferItem.Create(richTextFormat, rtfbytes));

      // Plain text
      dataTransfer.Add(DataTransferItem.CreateText(FlowDoc.Selection.GetText()));

      _ = clipboard.SetDataAsync(dataTransfer);

   }

   readonly static DataFormat<byte[]> richTextFormat = DataFormat.CreateBytesPlatformFormat("Rich Text Format");
   
  
   private async void PasteFromClipboard()
   {
      if (IsReadOnly) return;
      if (FlowDoc.Selection.GetStartInline() is not IEditable startInline) return;

      var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
      if (clipboard == null) return;

      //get paste location properties
      int originalSelectionStart = FlowDoc.Selection.Start;
      TextRange insertRange = FlowDoc.Selection;
      List<Paragraph> originalRangeParagraphs = FlowDoc.GetOverlappingParagraphsInRange(insertRange).ConvertAll(op=>op.FullClone());
      int deleteRangeLength = insertRange.Length;
      Paragraph startPar = insertRange.StartParagraph;
      Paragraph endPar = insertRange.EndParagraph;
      int insertParIndex = FlowDoc.AllParagraphs.IndexOf(startPar);
      bool firstParEmpty = startPar.Inlines[0] is EditableRun erun && erun.Text == "";
      int pastedTextLength = 0;
      List<int> addedBlockIds = [];

      bool contentPasted = false;

      FlowDoc.disableRunTextUndo = true;

      //Get clipboard content
      if (await clipboard.TryGetValueAsync(richTextFormat) is byte[] rtfbytes)
      {
         (int leftId, int rightId) edgeIds = FlowDoc.DeleteRange(insertRange, false);
         int insertIdx = startPar.Inlines.IndexOf(startPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.leftId)!) + 1;
         List<IEditable> rightSplitRuns = endPar.Inlines.ToList()[insertIdx..];

         int blockno = 0;
         List<Block> rtfBlocks = GetRtfContent(rtfbytes);
               
         foreach (Block block in rtfBlocks)
         {
            if (block is Paragraph p)
            {                  
               Paragraph addPar = startPar; 

               //Remove single empty run if present
               if (addPar.Inlines.Count == 1 && addPar.Inlines[0] is EditableRun run && run.InlineText == "")
               {
                  addPar.Inlines.RemoveAt(0);
                  insertIdx = 0;
               }
                        
               bool paragraphCreated = false;

               switch (blockno)
               {
                  case 0:
                     // insert first paragraph into existing paragraph
                     addPar.Inlines.AddOrInsertRange(p.Inlines, insertIdx);
                     break;

                  default:
                     // create new paragraphs for pars 1 onward
                     addPar = (Paragraph)block;
                     pastedTextLength += 1;
                     paragraphCreated = true;
                     break;
               }

               pastedTextLength += p.TextLength;

               if (paragraphCreated)
               {
                  if (blockno == rtfBlocks.Count - 1)
                  {
                     startPar.Inlines.RemoveMany(rightSplitRuns);
                     addPar.Inlines.AddRange(rightSplitRuns);
                  }

                  FlowDoc.Blocks.Insert(insertParIndex + blockno, addPar);
                  addedBlockIds.Add(addPar.Id);

               }
            }
            else
            { // non-Paragraph block always pastes as new block
               FlowDoc.Blocks.Insert(insertParIndex + blockno, block);
               addedBlockIds.Add(block.Id);
               pastedTextLength += block.TextLength;
            }

            blockno++;
         }

         contentPasted = true;
      }

      else if (await clipboard.TryGetBitmapAsync() is Bitmap pasteBitmap) 
      {
         Image pasteImage = new() { Source = pasteBitmap };
         EditableInlineUIContainer newEIUC = new(pasteImage);
         Paragraph newPar = new(FlowDoc);
         newPar.Inlines.Add(newEIUC);
         Paragraph extraPar = new(FlowDoc);
         FlowDoc.Blocks.Insert(insertParIndex + 1, newPar);
         FlowDoc.Blocks.Insert(insertParIndex + 2, extraPar);
         addedBlockIds.Add(newPar.Id);
         addedBlockIds.Add(extraPar.Id);
         pastedTextLength = 2;
         contentPasted = true;
      }

      else if (await clipboard.TryGetTextAsync() is string pasteText)
      {
         FlowDoc.SetRangeToText(FlowDoc.Selection, pasteText);
         contentPasted = true;
      }

      FlowDoc.disableRunTextUndo = false;

      //Update based on pasted content
      if (contentPasted)
      {
         FlowDoc.Undos.Add(new PasteUndo(originalRangeParagraphs, insertParIndex, FlowDoc, originalSelectionStart, deleteRangeLength - pastedTextLength, firstParEmpty, addedBlockIds));

         this.DocIC.UpdateLayout();
         FlowDoc.UpdateBlockAndInlineStarts(insertParIndex);
         FlowDoc.UpdateSelection();
         FlowDoc.Select(originalSelectionStart + pastedTextLength, 0);
         FlowDoc.Selection.BiasForwardStart = false;
         FlowDoc.Selection.BiasForwardEnd = false;
         FlowDoc.SelectionExtendMode = ExtendMode.ExtendModeNone;
         FlowDoc.ScrollFlowDocInDirection(1);

         CreateClient();

         _ = FlowDoc.AsyncUpdateCaret(FlowDoc.Selection);

      }


   }

   private List<Block> GetRtfContent(byte[] rtfbytes)
   {
      int textCount = 0;
      List<Block> returnList = [];
      
      string rtfstring = Encoding.ASCII.GetString(rtfbytes!);
      RTFDomDocument rtfdoc = new();
      rtfdoc.LoadRTFText(rtfstring);

      int domParCount = rtfdoc.Elements.OfType<RTFDomParagraph>().Count();
      int parno = 0;

      foreach (RTFDomElement rtfelm in rtfdoc.Elements)
      {
         switch (rtfelm)
         {
            case RTFDomParagraph rtfpar:

               Paragraph newPar = new(FlowDoc);

               switch (rtfpar.Format.Align)
               {
                  case RTFAlignment.Left: newPar.TextAlignment = TextAlignment.Left; break;
                  case RTFAlignment.Center: newPar.TextAlignment = TextAlignment.Center; break;
                  case RTFAlignment.Right: newPar.TextAlignment = TextAlignment.Right; break;
                  case RTFAlignment.Justify: newPar.TextAlignment = TextAlignment.Justify; break;
               }
               newPar.LineHeight = TwipToPix(PixelsToPoints(rtfpar.Format.LineSpacing)) / 2;
               newPar.FontFamily = new FontFamily(rtfpar.Format.FontName);
               newPar.FontSize = rtfpar.Format.FontSize * 2D;
               //newPar.Margin = new Thickness(rtfpar.Format.xxx);
              
               if (rtfpar.Elements.Count > 0)
               {
                  List<IEditable> addInlines = RtfConversions.GetRtfTextElementsAsInlines(rtfpar.Elements);
                  textCount+= addInlines.Sum(nil => nil.InlineLength);
                  newPar.Inlines.AddRange(addInlines); 
               }
               
               returnList.Add(newPar);

               parno++;

               break;

            case RTFDomTable table:

               break;

         }
      }


      return returnList;
   }

}
