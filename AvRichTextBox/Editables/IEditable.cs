using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.ComponentModel;

namespace AvRichTextBox;

public interface IEditable : INotifyPropertyChanged
{

   Inline BaseInline { get; }

   internal Paragraph? myParagraph { get; set; }

   internal bool IsStartInline { get; set; }
   internal bool IsEndInline { get; set; }
   internal bool IsWithinSelectionInline { get; set; }
   internal bool IsLastInlineOfParagraph { get; set; }
   internal int TextPositionOfInlineInParagraph { get; set; }
   internal int GetCharPosInInline(int charPos) => charPos - myParagraph!.StartInDoc - TextPositionOfInlineInParagraph;
   public string InlineText { get; set; }
   public bool IsEmpty { get; }
   public int InlineLength { get; }
   public double InlineHeight { get; }
   public IEditable Clone();
   public bool IsRun => this.GetType() == typeof(EditableRun);
   public bool IsUIContainer => this.GetType() == typeof(EditableInlineUIContainer);
   public bool IsLineBreak => this.GetType() == typeof(EditableLineBreak);

   public Thickness InlineSelectedBorderThickness { get; }
   public SolidColorBrush BackBrush { get; }
   public string DisplayInlineText { get; }
   public string InlineToolTip { get; }
   
}

