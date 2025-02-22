using Avalonia.Controls.Documents;

namespace AvRichTextBox;

public interface IEditable
{

   Inline BaseInline { get; }

   internal Paragraph? myParagraph { get; set; }
   internal bool IsStartInline { get; set; }
   internal bool IsEndInline { get; set; }
   internal bool IsFirstInlineOfParagraph { get; set; }
   internal int TextPositionOfInlineInParagraph { get; set; }
   internal int GetRangeStartInInline(TextRange trange) => trange.Start - myParagraph!.StartInDoc - TextPositionOfInlineInParagraph;
   internal int GetRangeEndInInline(TextRange trange) => trange.End - myParagraph!.StartInDoc - TextPositionOfInlineInParagraph;
   public string InlineText { get; set; }
   public int InlineLength { get; }
   public IEditable Clone();
   public bool IsRun => this.GetType() == typeof(EditableRun);
   public bool IsUIContainer => this.GetType() == typeof(EditableInlineUIContainer);
   public bool IsLineBreak => this.GetType() == typeof(EditableRun) && ((EditableRun)this).Text == "\v";
}

