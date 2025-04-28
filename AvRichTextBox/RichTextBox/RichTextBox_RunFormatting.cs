using Avalonia.Controls;
using Avalonia.Input;
using DocumentFormat.OpenXml.VariantTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using RtfDomParserAv;
using System.Linq;
using System.Threading.Tasks;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

public partial class RichTextBox
{
    
   private void ToggleItalics()
   {
      FlowDoc.ToggleItalic();

   }

   private void ToggleBold()
   {
      FlowDoc.ToggleBold();

   }

   private void ToggleUnderlining()
   {
      FlowDoc.ToggleUnderlining();

   }

   private void CopyToClipboard()
   {      
      
      var dataObject = new DataObject();

      //create rtf string
      List<IEditable> newInlines = FlowDoc.GetRangeInlines(FlowDoc.Selection);
      string rtfString = RtfConversions.GetRtfFromInlines(newInlines);
      byte[] rtfbytes = System.Text.Encoding.Default.GetBytes(rtfString);

      dataObject.Set("Rich Text Format", rtfbytes);
      dataObject.Set("Text", FlowDoc.Selection.GetText());
            
      TopLevel.GetTopLevel(this)!.Clipboard!.SetDataObjectAsync(dataObject);
      
   }

   
   private async void PasteFromClipboard()
   {
      bool TextPasted = false;
      int originalSelectionStart = FlowDoc.Selection.Start;
      int newSelPoint = originalSelectionStart;

      string[] formats = await TopLevel.GetTopLevel(this)!.Clipboard!.GetFormatsAsync();
      if (formats.Contains ("Rich Text Format"))
      {
         object? rtfobj = await TopLevel.GetTopLevel(this)!.Clipboard!.GetDataAsync("Rich Text Format");
         if (rtfobj != null)
         {
            byte[] rtfbytes = (byte[])rtfobj;
            string rtfstring = System.Text.Encoding.Default.GetString(rtfbytes!);

            RTFDomDocument dom = new();
            dom.LoadRTFText(rtfstring);
            List<IEditable> insertInlines = RtfConversions.GetInlinesFromRtf(dom);
            insertInlines.Reverse();
            int addedchars = FlowDoc.SetRangeToInlines(FlowDoc.Selection, insertInlines);

            newSelPoint = Math.Min(newSelPoint + addedchars, FlowDoc.DocEndPoint - 1);

            TextPasted = true;
         }
      }
      else if (formats.Contains("Text"))
      {
         object? textobj = await TopLevel.GetTopLevel(this)!.Clipboard!.GetDataAsync("Text");

         if (textobj != null)
         {
            string pasteText = textobj.ToString()!;
            FlowDoc.SetRangeToText(FlowDoc.Selection, pasteText);

            newSelPoint = Math.Min(newSelPoint + pasteText.Length, FlowDoc.DocEndPoint - 1);

            TextPasted = true;
         }
      }
      
      if (TextPasted)
      {
         this.DocIC.UpdateLayout();
         await Task.Delay(100); //necessary for following operations
         
         FlowDoc.Selection.EndParagraph.CallRequestInlinesUpdate();  // important
         FlowDoc.Selection.EndParagraph.UpdateEditableRunPositions();

         FlowDoc.Select(newSelPoint, 0);
         FlowDoc.UpdateSelection();

         FlowDoc.Selection.BiasForwardStart = false;
         FlowDoc.Selection.BiasForwardEnd = false;
         FlowDoc.SelectionExtendMode = ExtendMode.ExtendModeNone;

         CreateClient();

        
      }

   }


}
