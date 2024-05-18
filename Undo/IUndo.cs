using Avalonia.Controls.Documents;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvRichTextBox;

public interface IUndo
{
   public void PerformUndo();
   public int UndoEditOffset { get; }
   public bool UpdateTextRanges { get; }
}

internal class IEditablePropertyAssociation
{
   internal IEditable InlineItem { get; set; }
   internal object PropertyValue { get; set; }
   internal FlowDocument.FormatRun? FormatRun { get; set; }  

   internal IEditablePropertyAssociation(IEditable inlineItem, FlowDocument.FormatRun formatRun, object propertyValue)
   {
      InlineItem = inlineItem;
      FormatRun = formatRun;
      PropertyValue = propertyValue;
   }
}