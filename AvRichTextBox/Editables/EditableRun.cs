using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace AvRichTextBox;

public class EditableRun : Run, IEditable
{
   
   public EditableRun() { InitializeRun();  }

   public EditableRun(string text)
   {
      this.Text = text;

      InitializeRun();

   }

   private void InitializeRun()
   {
      Id = ++FlowDocument.InlineIdCounter;
      BaselineAlignment = BaselineAlignment.Baseline;
      //FontFamily = "Meiryo";
      FontSize = 16;

      PropertyChanged += EditableRun_PropertyChanged;
   }

   private void EditableRun_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
   {
      
      switch (e.Property)
      {
         case AvaloniaProperty tp when tp == Run.TextProperty:

            if (MyFlowDoc == null || MyFlowDoc.disableRunTextUndo) return;

            if (e.Property == Run.TextProperty && e.Sender is EditableRun run)
            {
               var oldText = (string?)e.OldValue ?? "";
               var newText = (string?)e.NewValue ?? "";
               //Debug.WriteLine("\noldText: " + oldText + "\n" + "newText: " + newText);

               var (start, deleteLen, insertText, deletedText) = GetDiff(oldText, newText);

               if (deleteLen == 0 && insertText.Length == 0)
                  return;

               run.MyFlowDoc.Undos.Add(new TextChangedUndo(run.MyFlowDoc, run.MyParagraphId, run.Id, start, deletedText, insertText, run.MyFlowDoc.Selection.Start));
            }
            break;
      }

   }

   public int Id { get; set; }
   public int MyParagraphId { get; set; }
   public FlowDocument MyFlowDoc { get; set; } = null!;
   public int TextPositionOfInlineInParagraph { get; set; }
   
   public virtual string InlineText { get => Text!; set => Text = value; }
   public virtual int InlineLength => InlineText.Length;
   public double InlineHeight => FontSize;

   internal IEditable PreviousInline { get; set; } = null!;
   internal IEditable NextInline { get; set; } = null!;

   public bool IsEmpty => InlineText.Length == 0;
   public string FontName => FontFamily?.Name == null ? "" : FontFamily?.Name!;
      
   public bool IsLastInlineOfParagraph { get; set; }
   public bool IsTableCellInline { get; set; } = false;


   public virtual IEditable Clone() => 

      new EditableRun(this.Text!)
      {
         FontStyle = this.FontStyle,
         FontWeight = this.FontWeight,
         TextDecorations = this.TextDecorations,
         FontSize = this.FontSize,
         FontFamily = this.FontFamily,
         Background = this.Background,
         MyParagraphId = this.MyParagraphId,
         MyFlowDoc = this.MyFlowDoc,
         TextPositionOfInlineInParagraph = this.TextPositionOfInlineInParagraph,  //necessary because clone is produced when calculating range inline positions
         IsLastInlineOfParagraph = this.IsLastInlineOfParagraph,
         BaselineAlignment = this.BaselineAlignment,
         Foreground = this.Foreground,
      };
   
   public virtual IEditable CloneWithId()
   {
      IEditable IdClone = this.Clone();
      IdClone.Id = this.Id;
      return IdClone;

   }

#if DEBUG
   // FOR DEBUGGER PANEL
   public InlineVisualizationProperties InlineVP { get; set; } = new();
   public string InlineToolTip => $"Background: {Background}\nForeground: {Foreground}\nFontFamily: {FontFamily}\nPrevInlineLineBreak?: {PreviousInline?.IsLineBreak}\nNextInlineLineBreak?: {NextInline?.IsLineBreak}";
   public virtual string DisplayInlineText => IsEmpty ? "{>EMPTY<}" : (InlineText.Length == 1 ? Text!.Replace(" ", "{>SPACE<}").Replace("\t", "{>TAB<}") : Text!.Replace("\t", "{>TAB<}"));

   IEditable IEditable.PreviousInline { get => PreviousInline; set => PreviousInline = value; }
   IEditable IEditable.NextInline { get => NextInline; set => NextInline = value; }
#endif


   internal static (int start, int deleteLen, string insertText, string deletedText) GetDiff(string oldText, string newText)
   {
      oldText ??= "";
      newText ??= "";

      int start = 0;
      int oldLen = oldText.Length;
      int newLen = newText.Length;

      while (start < oldLen && start < newLen && oldText[start] == newText[start])
         start++;

      int endOld = oldLen - 1;
      int endNew = newLen - 1;

      while (endOld >= start && endNew >= start && oldText[endOld] == newText[endNew])
      {
         endOld--;
         endNew--;
      }

      int deleteLen = Math.Max(0, endOld - start + 1);
      int insertLen = Math.Max(0, endNew - start + 1);

      string deletedText = deleteLen == 0 ? "" : oldText.Substring(start, deleteLen);
      string insertText = insertLen == 0 ? "" : newText.Substring(start, insertLen);

      //Debug.WriteLine("insertedText = " + insertText + ", deletedText = " + deletedText + ", deleteLen = " + deleteLen + ", start: " + start);

      return (start, deleteLen, insertText, deletedText);

   }


}




