using DynamicData;
using RtfDomParserAv;
using System.Text;
using System.Xml;

namespace AvRichTextBox;

public partial class FlowDocument
{  
   internal void SetRangeToText(TextRange tRange, string newText)
   {  //The delete range and SetRangeToText should constitute one Undo operation

      Paragraph startPar = tRange.StartParagraph;
      int rangeStart = tRange.Start;
      int rangeEnd = tRange.End;
      int deleteRangeLength = tRange.Length;
      int parIndex = Blocks.IndexOf(startPar);
      bool firstParEmpty = startPar.Inlines[0] is EditableRun erun && erun.Text == "";
      bool firstParWasDeleted = startPar.StartInDoc == rangeStart && startPar.EndInDoc <= rangeEnd && !firstParEmpty; 

      //Undos.Add(new PasteUndo(GetOverlappingParagraphsInRange(tRange), parIndex, this, rangeStart, deleteRangeLength - newText.Length, firstParEmpty, [], firstParWasDeleted));

      //Delete any selected text first
      if (tRange.Length > 0)
      {
         DeleteRange(tRange, false, false);
         tRange.CollapseToStart();
         SelectionExtendMode = ExtendMode.ExtendModeNone;
      }

      //Debug.WriteLine("first par deleted: " + firstParWasDeleted);

      if (tRange.GetStartInline() is not IEditable startInline) return;

      List<IEditable> splitInlines = SplitRunAtPos(tRange.Start, startInline, GetCharPosInInline(startInline, tRange.Start));

      int startInlineIndex = startPar.Inlines.IndexOf(splitInlines[0]) + 1;

      if (splitInlines[0] is EditableRun sRun)
      {
         EditableRun newEditableRun = new(newText)
         {
            FontFamily = sRun.FontFamily,
            FontWeight = sRun.FontWeight,
            FontStyle = sRun.FontStyle,
            FontSize = sRun.FontSize,
            TextDecorations = sRun.TextDecorations,
            Background = sRun.Background,
            BaselineAlignment = sRun.BaselineAlignment,
            Foreground = sRun.Foreground
         };

         startPar.Inlines.Insert(startInlineIndex, newEditableRun);

         if (splitInlines[0].InlineText == "")
            startPar.Inlines.Remove(splitInlines[0]);
      }

      startPar.CallRequestInvalidateVisual();
      startPar.CallRequestTextLayoutInfoStart();
      startPar.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(startPar);

   }

   internal void Undo()
   {
      if (Undos.Count > 0)
      {
         disableRunTextUndo = true;
         
         Undos.Last().PerformUndo();

         UpdateSelection();

         if (Undos.Last().UpdateTextRanges)
            UpdateTextRanges(Selection.Start, Undos.Last().UndoEditOffset);

         Undos.RemoveAt(Undos.Count - 1);

         UpdateSelectedParagraphs();
         

         ScrollInDirection?.Invoke(1);
         ScrollInDirection?.Invoke(-1);

         disableRunTextUndo = false;

      }
   }

   internal void RestoreDeletedBlocks(List<Paragraph> parClones, int blockIndex, bool firstParWasDeleted)
   {
      //If first paragraph was not deleted, it needs to be removed before restoring previous state
      if (!firstParWasDeleted)
         Blocks.RemoveAt(blockIndex);

      //Restore all of the previous paragraphs
      Blocks.AddOrInsertRange(parClones, blockIndex);

      foreach (Paragraph p in parClones)
      {
         p.CallRequestInlinesUpdate();
         p.ClearSelection();
      }
  
      UpdateBlockAndInlineStarts(blockIndex);

   }

   private int ProcessBlocks(List<Block> blocks, Paragraph startPar, int insertIdx, int insertParIndex, List<int> addedBlockIds, List<IEditable> rightSplitRuns)
   {
      int pastedTextLength = 0;
      int blockno = 0;
      foreach (Block block in blocks)
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

               //case int bno when blockno == blocks.Count - 1:
               //   startPar.Inlines.AddOrInsertRange(rightSplitRuns, insertIdx + blockno);
               //   break;

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
               if (blockno == blocks.Count - 1)
               {
                  startPar.Inlines.RemoveMany(rightSplitRuns);
                  addPar.Inlines.AddRange(rightSplitRuns);
               }

               Blocks.Insert(insertParIndex + blockno, addPar);
               addedBlockIds.Add(addPar.Id);

            }
         }
         else
         { // non-Paragraph block always pastes as new block
            Blocks.Insert(insertParIndex + blockno, block);
            addedBlockIds.Add(block.Id);
            pastedTextLength += block.TextLength;
         }

         blockno++;
      }

      return pastedTextLength;
   }

   private List<Block> GetRtfContent(byte[] rtfbytes)
   {
      int textCount = 0;
      List<Block> rtfBlockList = [];

      string rtfstring = Encoding.ASCII.GetString(rtfbytes);
      RTFDomDocument rtfdoc = new();
      rtfdoc.LoadRTFText(rtfstring);

      int domParCount = rtfdoc.Elements.OfType<RTFDomParagraph>().Count();
      int parno = 0;

      foreach (RTFDomElement rtfelm in rtfdoc.Elements)
      {
         switch (rtfelm)
         {
            case RTFDomParagraph rtfpar:

               Paragraph rtfPar = RtfConversions.GetParagraphFromRtfDom(rtfpar, this);
               rtfBlockList.Add(rtfPar);
               textCount += (rtfBlockList.Count + rtfBlockList.OfType<Paragraph>().ToList().SelectMany(p => p.Inlines).Sum(il => il.InlineLength));
               parno++;

               break;

            case RTFDomTable rtftable:
               Table rtfTable = RtfConversions.GetTableFromRtfDom(rtftable, this, rtfdoc.ColorTable);
               break;
         }
      }

      return rtfBlockList;
   }


   internal int InsertXaml(byte[] xamlbytes, Paragraph startPar, Paragraph endPar, TextRange insertRange, int insertParIndex, List<int> addedBlockIds)
   {
      (int leftId, int rightId) edgeIds = DeleteRange(insertRange, false, false);
      int insertIdx = startPar.Inlines.IndexOf(startPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.leftId)!) + 1;
      List<IEditable> rightSplitRuns = endPar.Inlines.ToList()[insertIdx..];

      string xamlString = Encoding.ASCII.GetString(xamlbytes);
           
      List<Block> xamlBlocks = [];

      XmlDocument xamlDocument = new();

      xamlDocument.LoadXml(xamlString);
      
      if (xamlDocument.ChildNodes.Count == 1)
      {
         XmlNode? SectionNode = xamlDocument.ChildNodes[0];
         if (SectionNode!.Name == "Section")
         {
            foreach (XmlNode blockNode in SectionNode.ChildNodes.OfType<XmlNode>())
            {
               switch (blockNode.Name)
               {
                  case "Paragraph":

                     xamlBlocks.Add(XamlConversions.GetParagraph(blockNode, this));
                     break;

                  case "Table":

                     xamlBlocks.Add(XamlConversions.GetTable(blockNode, this));
                     break;
               }
            }
         }
      }

      return ProcessBlocks(xamlBlocks, startPar, insertIdx, insertParIndex, addedBlockIds, rightSplitRuns);
            
   }


}