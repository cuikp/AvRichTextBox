using Avalonia.Controls;
using Avalonia.Controls.Documents;
using static AvRichTextBox.IEditable;

namespace AvRichTextBox;

public class EditableInlineUIContainer : InlineUIContainer, IEditable
{

   public EditableInlineUIContainer(Control c) { Child = c; Id = ++FlowDocument.InlineIdCounter; }

   public int Id { get; set; }
   public Inline BaseInline => this;
   public int MyParagraphId { get; set; }
   public int TextPositionOfInlineInParagraph { get; set; }
   public string InlineText { get; set; } = "@";
   
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
   public string FontName => "---";

   public int InlineLength => 1;
   public bool IsEmpty => false;
   public bool IsLastInlineOfParagraph { get; set; }
   //public double InlineHeight => (this.Child != null && this.Child.GetType() == typeof(Image) ? : this.Child.Bounds.Height;
   public double InlineHeight => Child == null ? 0 : this.Child.Bounds.Height;
   

   public int ImageNo;

   public IEditable Clone() => new EditableInlineUIContainer(this.Child) { MyParagraphId = this.MyParagraphId }; //, Id = this.Id }; }

   public IEditable CloneWithId()
   {
      IEditable IdClone = this.Clone();
      IdClone.Id = this.Id;
      return IdClone;
   }

   public bool IsSelected { get; set { field = value; this.Child.Opacity = value ? 0.2 : 1; } } = false;


#if DEBUG
   // FOR DEBUGGER PANEL
   public InlineVisualizationProperties InlineVP { get; set; } = new();
   public string InlineToolTip => "";
   public string DisplayInlineText { get => $"<UICONTAINER> => {(this.Child != null && this.Child.GetType() == typeof(Image) ? "Image" : "NoChild")}"; }
#endif


}


