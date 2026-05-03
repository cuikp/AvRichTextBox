using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace AvRichTextBox;

public class EditableInlineUIContainer : InlineUIContainer, IEditable
{
   public EditableInlineUIContainer() { Id = ++FlowDocument.InlineIdCounter; }

   public EditableInlineUIContainer(Control c) { Child = c; Id = ++FlowDocument.InlineIdCounter; }

   public int Id { get; set; }
   public int MyParagraphId { get; set; }
   public FlowDocument MyFlowDoc { get; set; } = null!;
   public int TextPositionOfInlineInParagraph { get; set; }
   public string InlineText { get; set; } = "@";
   public bool IsTableCellInline { get; set; } = false;
   public object Tag { get; set; } = null!;

   [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
   public string FontName => "---";

   public int InlineLength => 1;
   public bool IsEmpty => false;
   public bool IsLastInlineOfParagraph { get; set; }
   //public double InlineHeight => (this.Child != null && this.Child.GetType() == typeof(Image) ? : this.Child.Bounds.Height;
   public double InlineHeight => Child == null ? 0 : this.Child.Bounds.Height;

   internal IEditable PreviousInline { get; set; } = null!;
   internal IEditable NextInline { get; set; } = null!;

   public int ImageNo;

   public IEditable Clone() => new EditableInlineUIContainer(this.Child) { MyParagraphId = this.MyParagraphId, MyFlowDoc = this.MyFlowDoc, };

   public IEditable CloneWithId()
   {  
      IEditable IdClone = this.Clone();
      IdClone.Id = this.Id;
      return IdClone;
            
   }

   public bool IsSelected { get; set; } = false;

#if DEBUG
   // FOR DEBUGGER PANEL
   public InlineVisualizationProperties InlineVP { get; set; } = new();
   public string InlineToolTip => "";
   public string DisplayInlineText { get => $"<UICONTAINER> => {(this.Child != null && this.Child.GetType() == typeof(Image) ? "Image" : "NoChild")}"; }
   IEditable IEditable.PreviousInline { get => PreviousInline; set => PreviousInline = value; }
   IEditable IEditable.NextInline { get => NextInline; set => NextInline = value; }
#endif


}


