using Avalonia.Controls;

namespace AvRichTextBox;

internal partial class DebuggerPanel : UserControl
{
   internal DebuggerPanel()
   {
      InitializeComponent();

      ParagraphsLB.DataContextChanged += ParagraphsLB_DataContextChanged;
   }

   private void ParagraphsLB_DataContextChanged(object? sender, EventArgs e)
   {
        if (ParagraphsLB.DataContext is FlowDocument fdoc)
        {
            fdoc.SelectionParagraphs.CollectionChanged -= SelectionParagraphs_CollectionChanged;
            fdoc.SelectionParagraphs.CollectionChanged += SelectionParagraphs_CollectionChanged;
        }
   }

   private void SelectionParagraphs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   {
        if (this.IsVisible)
            ParagraphsLB.UpdateLayout();
    }

    private void Disable()
    {
        ParagraphsLB.ItemsSource = null!; 
    }

    private void Enable(FlowDocument fdoc)
    {
        ParagraphsLB.ItemsSource = fdoc.SelectionParagraphs;
    }
}
