using Avalonia.Threading;
using DynamicData;
using RtfDomParserAv;
using System.Text;
using System.Xml;

namespace AvRichTextBox;

public partial class FlowDocument
{
   internal int InsertRTF(byte[] rtfbytes, Paragraph startPar, TextRange insertRange, int insertParIndex, List<int> addedBlockIds)
   {
      (int leftId, int rightId) edgeIds = DeleteRange(insertRange, false, false);
      int insertIdx = GetInsertIndexAfterDelete(startPar, edgeIds.leftId, insertRange);

      List<IEditable> rightSplitRuns = startPar.Inlines.ToList()[insertIdx..];

      List<Block> rtfBlocks = GetRtfContent(rtfbytes);

      return ProcessInsertBlocks(rtfBlocks, startPar, insertIdx, insertParIndex, addedBlockIds, rightSplitRuns);

   }


   private int GetInsertIndexAfterDelete(Paragraph startPar, int leftId, TextRange insertRange)
   {
      if (insertRange.Start == startPar.StartInDoc)
         return 0;

      IEditable? leftInline = startPar.Inlines.FirstOrDefault(il => il.Id == leftId);
      if (leftInline != null)
         return startPar.Inlines.IndexOf(leftInline) + 1;

      // The inline referenced by leftId was fully deleted.
      // Calculate insert index from the character position instead.
      int posInPar = insertRange.Start - startPar.StartInDoc;
      IEditable? precedingInline = startPar.Inlines.LastOrDefault(il => il.TextPositionOfInlineInParagraph + il.InlineLength <= posInPar);
      return precedingInline != null ? startPar.Inlines.IndexOf(precedingInline) + 1 : 0;
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
      int insertIdx = GetInsertIndexAfterDelete(startPar, edgeIds.leftId, insertRange);

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

      return ProcessInsertBlocks(xamlBlocks, startPar, insertIdx, insertParIndex, addedBlockIds, rightSplitRuns);

   }

   internal void InsertText(string? insertText)
   {
      if (Selection.GetStartInline() is not IEditable startInline || startInline.GetType() == typeof(EditableInlineUIContainer)) return;
     

      if (insertText != null)
      {
         if (Selection.Length > 0)
         {
            DeleteRange(Selection, true, false);
            Selection.CollapseToStart();
            SelectionExtendMode = ExtendMode.ExtendModeNone;
            startInline = Selection.GetStartInline() ?? startInline;
         }

         int insertIdx = 0;
         if (InsertRunMode)
         {  
            disableRunTextUndo = true;

            if (startInline.CloneWithId() is not EditableRun startInlineRunClone) return;
            int originalStart = Selection.Start;
            int runIdx = Selection.StartParagraph.Inlines.IndexOf(startInline);

            (int idLeft, int idRight) edgeIds;

            List<IEditable> applyInlines = GetRangeInlinesAndAddToDoc(Selection, out edgeIds);

            if (applyInlines.Count == 0)
            {
               applyInlines.Add(new EditableRun(""));
               Selection.StartParagraph.Inlines.Insert(runIdx, applyInlines[0]);
            }

            if (applyInlines.Count > 0 && applyInlines[0] is EditableRun erun)
               startInline = erun;

            int addedId = applyInlines[0].Id;
            
            Undos.Add(new InsertNewFormattedTextUndo(Selection.StartParagraph.Id, startInlineRunClone, edgeIds, addedId, runIdx, this, originalStart));

            startInline.InlineText = insertText;

            toggleFormatRun?.Invoke(startInline);
            InsertRunMode = false;

            disableRunTextUndo = false;
         }
         else
         {
            try
            {
               insertIdx = GetCharPosInInline(startInline, Selection.Start);
               startInline.InlineText = startInline.InlineText.Insert(insertIdx, insertText); // undo handled by PropertyChanged: Text
            }
            catch (Exception ex){ Debug.WriteLine("insert Error: startInlinetext = " + startInline.InlineText + ", idx = " + insertIdx + "\n" + ex.Message + "*");}
         }

         UpdateTextRanges(Selection.Start, insertText.Length);

         Selection.StartParagraph.CallRequestInlinesUpdate();
         UpdateBlockAndInlineStarts(Selection.StartParagraph);

         for (int i = 0; i < insertText.Length; i++) 
            MoveSelectionRight(true);
         

      }

   }

   internal void InsertLineBreak()
   {
      Paragraph startPar = Selection.StartParagraph;

      if (startPar.Inlines.Count == 1 && startPar.Inlines[0] is EditableInlineUIContainer euic)
         return; // Don't mess container edges

      if (Selection.GetStartInline() is not IEditable startInline) 
         return; 

      int runIdx = startPar.Inlines.IndexOf(startInline);
      IEditable originalInlineClone = startInline.CloneWithId();

      List<IEditable> eruns = SplitRunAtPos(Selection.Start, startInline, GetCharPosInInline(startInline, Selection.Start)); // creates an empty inline

      //Debug.WriteLine("split runs\n" + string.Join("\n", eruns.OfType<EditableRun>().ToList().ConvertAll(er => er.Text)));

      var newELB = new EditableLineBreak();
      int insertIdx = runIdx + 1;
      startPar.Inlines.Insert(insertIdx, newELB);

      List<int> addedRuns = eruns.ConvertAll(erun => erun.Id);

      if (insertIdx == startPar.Inlines.Count - 1 || startPar.Inlines[insertIdx + 1].IsLineBreak)
      {
         EditableRun newErun = new ("");
         startPar.Inlines.Insert(insertIdx + 1, newErun);
         addedRuns.Add(newErun.Id);
      }
      
      Undos.Add(new InsertLineBreakUndo(Selection.StartParagraph.Id, newELB.Id, addedRuns, runIdx, originalInlineClone, this, Selection.Start));

      UpdateTextRanges(Selection.Start, 1);

      SelectionExtendMode = ExtendMode.ExtendModeNone;

      startPar.UpdateEditableRunPositions();
      startPar.CallRequestInlinesUpdate();
      startPar.CallRequestTextLayoutInfoStart();
      startPar.CallRequestTextLayoutInfoEnd();

      Select(Selection.Start + 2, 0);
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      ScrollInDirection?.Invoke(1);


   }

   internal void InsertParagraph(bool addUndo, int insertCharIndex)
   {  //The delete range and InsertParagraph should constitute one Undo operation

      disableRunTextUndo = true;

      if (GetContainingParagraph(insertCharIndex) is not Paragraph insertPar) return;

      if (insertPar.IsTableCellBlock) return;
      
      if (insertPar.Inlines.Count == 1 && insertPar.Inlines[0] is EditableInlineUIContainer euic)
         return; // Don't mess with container edges

      List<IEditable> keepParInlineClones = [.. insertPar.Inlines.Select(il=>il.CloneWithId())]; 

      int originalSelStart = insertCharIndex;
      int parIndex = Blocks.IndexOf(insertPar);
      int selectionLength = 0;

      if (addUndo)
      {
         selectionLength = Selection.Length;
         if (Selection.Length > 0)
         {
            DeleteRange(Selection, false, false);
            Selection.CollapseToStart();
            SelectionExtendMode = ExtendMode.ExtendModeNone;
         }
      }

      if (GetStartInline(insertCharIndex) is not IEditable startInline) return;

      int StartRunIdx = insertPar.Inlines.IndexOf(startInline);

      //Split at selection
      List<IEditable> parSplitRuns = SplitRunAtPos(insertCharIndex, startInline, GetCharPosInInline(startInline, insertCharIndex));


      List<IEditable> RunList1 = [.. insertPar.Inlines.Take(new Range(0, StartRunIdx)).ToList().ConvertAll(r => r)];
      if (parSplitRuns[0].InlineText != "" || RunList1.Count == 0)
         RunList1.Add(parSplitRuns[0]);
      List<IEditable> RunList2 = [.. insertPar.Inlines.Take(new Range(StartRunIdx + 1, insertPar.Inlines.Count)).ToList().ConvertAll(r => r as IEditable)];
      
      Paragraph originalPar = insertPar;
      
      originalPar.Inlines.Clear();
      originalPar.Inlines.AddRange(RunList1);

      if (originalPar.Inlines.Last() is EditableLineBreak elb)
         originalPar.Inlines.Insert(originalPar.Inlines.Count, new EditableRun(""));

      Paragraph parToInsert = originalPar.PropertyClone();

      parToInsert.Inlines.AddRange(RunList2);
      Blocks.Insert(parIndex + 1, parToInsert);

      if (parToInsert.Inlines.Count == 0)
      {
         EditableRun erun = (EditableRun)originalPar.Inlines.Last().Clone();
         erun.Text = "";
         parToInsert.Inlines.Add(erun);
      }
      
      UpdateTextRanges(insertCharIndex, 1);
      
      UpdateBlockAndInlineStarts(parIndex);

      originalPar.CallRequestInlinesUpdate();
      parToInsert.CallRequestInlinesUpdate();


      if (addUndo)
         Undos.Add(new InsertParagraphUndo(this, originalPar.Id, parToInsert.Id, keepParInlineClones, originalSelStart, selectionLength - 1));

      originalPar.CallRequestTextLayoutInfoStart();
      originalPar.CallRequestTextLayoutInfoEnd();
      parToInsert.CallRequestTextLayoutInfoStart();
      parToInsert.CallRequestTextLayoutInfoEnd();
            

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;
                  
 
      Dispatcher.UIThread.Post(() =>
      {
         Selection.End += 1;
         Selection.CollapseToEnd();
      });
            
      ScrollInDirection?.Invoke(1);

      disableRunTextUndo = false;


   }


}