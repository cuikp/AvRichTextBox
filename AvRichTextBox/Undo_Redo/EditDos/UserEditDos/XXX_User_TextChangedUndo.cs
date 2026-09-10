
namespace AvRichTextBox; 


//internal class TextChangedUndo(FlowDocument flowDoc, int parId, int runId, int start, string deletedText, string insertText, int origSelectionStart) : IEditDo
//{
//    public int UndoEditOffset => -1;
//    public bool UpdateTextRanges => true;

//    public void PerformUndo()
//    {
//        if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

//        int thisParLengthBefore = thisPar.TextLength;

//        if (thisPar.Inlines.FirstOrDefault(il => il.Id == runId) is not EditableRun thisRun) return;

//        flowDoc.disableRunTextUndo = true;

//        if (insertText.Length > 0)
//            thisRun.Text = thisRun.Text!.Remove(start, insertText.Length);
//        if (deletedText.Length > 0)
//            thisRun.Text = thisRun.Text!.Insert(start, deletedText);

//        flowDoc.disableRunTextUndo = false;

//        int thisParLengthAfter = thisPar.TextLength;

//        thisPar.CallRequestInlinesUpdate();
//        flowDoc.UpdateBlockAndInlineStarts(thisPar);
//        flowDoc.UpdateTextRanges(thisPar.StartInDoc, thisParLengthAfter - thisParLengthBefore);

//        Dispatcher.UIThread.Post(() =>
//        {
//            flowDoc.Selection.Start = origSelectionStart;
//            flowDoc.Selection.End = flowDoc.Selection.Start;
//        });

//    }

//    public void PerformRedo()
//    {
//    }

//}

