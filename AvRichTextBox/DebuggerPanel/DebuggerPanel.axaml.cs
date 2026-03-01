using Avalonia.Controls;

namespace AvRichTextBox;

public partial class DebuggerPanel : UserControl
{
   public DebuggerPanel()
   {
      InitializeComponent();

      ParagraphsLB.DataContextChanged += ParagraphsLB_DataContextChanged;
   }

   private void ParagraphsLB_DataContextChanged(object? sender, EventArgs e)
   {
      if (ParagraphsLB.DataContext != null)
      {
         if (ParagraphsLB.DataContext is FlowDocument fdoc)
         {
            fdoc.SelectionParagraphs.CollectionChanged -= SelectionParagraphs_CollectionChanged;
            fdoc.SelectionParagraphs.CollectionChanged += SelectionParagraphs_CollectionChanged;
         }
      }

   }

   private void SelectionParagraphs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   {
      if (this.IsVisible)
         ParagraphsLB.UpdateLayout();
   }


}