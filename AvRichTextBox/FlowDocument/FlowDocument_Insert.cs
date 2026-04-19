using DynamicData;

namespace AvRichTextBox;

public partial class FlowDocument
{
   internal void InsertText(string? insertText)
   {
      if (Selection.GetStartInline() is not IEditable startInline || startInline.GetType() == typeof(EditableInlineUIContainer)) return;

      if (insertText != null)
      {
         if (Selection.Length > 0)
         {
            DeleteRange(Selection, true);
            Selection.CollapseToStart();
            SelectionExtendMode = ExtendMode.ExtendModeNone;
            startInline = Selection.GetStartInline() ?? startInline;
         }

         int insertIdx = 0;
         if (InsertRunMode)
         {
            (int idLeft, int idRight) edgeIds;
            List<IEditable> applyInlines = GetRangeInlinesAndAddToDoc(Selection, out edgeIds);
            if (applyInlines.Count == 0)
            {
               applyInlines.Add(new EditableRun(""));
               Selection.StartParagraph.Inlines.Insert(0, applyInlines[0]);
            }
            startInline = applyInlines[0];
            startInline.InlineText = insertText;
            toggleFormatRun!(startInline);
            InsertRunMode = false;
         }
         else
         {
            try
            {
               insertIdx = GetCharPosInInline(startInline, Selection.Start);
               startInline.InlineText = startInline.InlineText.Insert(insertIdx, insertText); // undo handled by PropertyChanged: Text
            }
            catch(Exception ex){ Debug.WriteLine("insert Error: startInlinetext = " + startInline.InlineText + ", idx = " + insertIdx + "\n" + ex.Message + "*");}
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
      startPar.Inlines.Insert(runIdx + 1, newELB);

      Undos.Add(new InsertLineBreakUndo(Selection.StartParagraph.Id, newELB.Id, (eruns[0].Id, eruns[1].Id), runIdx, originalInlineClone, this, Selection.Start));
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
            DeleteRange(Selection, false);
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
      originalPar.SelectionStartInBlock = 0;
      originalPar.CollapseToStart();

      if (originalPar.Inlines.Last() is EditableLineBreak elb)
      {
         originalPar.Inlines.Insert(originalPar.Inlines.Count, new EditableRun(""));
      }

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

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;
      Selection.End += 1;
      Selection.CollapseToEnd();

      originalPar.CallRequestTextLayoutInfoStart();
      parToInsert.CallRequestTextLayoutInfoStart();
      originalPar.CallRequestTextLayoutInfoEnd();
      parToInsert.CallRequestTextLayoutInfoEnd();

      ScrollInDirection?.Invoke(1);

      disableRunTextUndo = false;

   }


}