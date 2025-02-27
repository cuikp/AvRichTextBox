//using System.Diagnostics;
//using RtfDomParser;
//using System.Text;
//using System.Collections.Generic;
//using Avalonia.Controls.Documents;
//using Avalonia.Media;


//namespace AvRichTextBox;

//internal partial class RtfConversions
//{
  
//   public static string ConvertBlocksToRtf(IEnumerable<Block> blocks)
//   {
//      var sb = new StringBuilder();
//      //sb.Append(@"{\rtf1\ansi ");
//      sb.Append(@"{\rtf1\ansi\deff0 {\fonttbl{\f0\fswiss Arial;}}");    

//      bool BoldOn = false;
//      bool ItalicOn = false;
//      bool UnderlineOn = false;
//      int CurrentLang = 1033;

//      foreach (Block block in blocks)
//      {
//         if (block.GetType() == typeof(Paragraph))
//         {
//            Paragraph p = (Paragraph)block;
//            foreach (IEditable ied in p.Inlines)
//            {
//               if (ied.GetType() == typeof(EditableRun))
//               {
//                  EditableRun run = (EditableRun)ied;

//                  if (!BoldOn && run.FontWeight == FontWeight.Bold) { sb.Append(@"\b "); BoldOn = true; }
//                  if (!ItalicOn && run.FontStyle == FontStyle.Italic) {sb.Append(@"\i "); ; ItalicOn = true; }
//                  if (!UnderlineOn && run.TextDecorations == TextDecorations.Underline) { sb.Append(@"\ul "); ; UnderlineOn = true;}
            
//                  if (run.FontSize > 0) sb.Append($@"\fs{(int)(run.FontSize * 2)} ");

//                  if (!string.IsNullOrEmpty(run.Text))
//                     sb.Append(GetRtfRunText(run.Text!, CurrentLang));

//                  //Debug.WriteLine("text = " + run.Text);

//                  if (BoldOn && run.FontWeight == FontWeight.Normal) { sb.Append(@"\b0 "); BoldOn = false; }
//                  if (ItalicOn && run.FontStyle == FontStyle.Normal) { sb.Append(@"\i0 "); ItalicOn = false; }
//                  if (UnderlineOn && run.TextDecorations != TextDecorations.Underline) {sb.Append(@"\ul0 "); UnderlineOn = false; }

//         }
//            }
//            sb.Append(@"\par ");
//         }
//      }

//      sb.Append('}');
//      return sb.ToString();
//   }
    
//   private static string GetRtfRunText(string text, int currentLang)
//   {
//      StringBuilder sb = new ();

//      foreach (char c in text)
//      {
//         int newLang = GetLanguageForChar(c);

//         if (newLang != currentLang)
//         {
//            sb.Append($@"\lang{newLang} ");
//            currentLang = newLang;
//         }

//         if (c is '\\' or '{' or '}')
//            sb.Append(@"\" + c); // RTF control characters
//         else if (c > 127) // Non-ASCII (double-byte characters)
//         {
//            sb.Append(@"\u" + (int)c + "?"); // Unicode escape
//         }

//         else
//            sb.Append(c);
            
//      }
//      return sb.ToString();
//   }

   
//   private static int GetLanguageForChar(char c)
//   {
//      if ((c >= 0x4E00 && c <= 0x9FFF) || // CJK Unified Ideographs (Common Chinese, Japanese, Korean)
//          (c >= 0x3400 && c <= 0x4DBF))  // CJK Extension A (Rare Chinese characters)
//      {
//         return 2052; // Simplified Chinese (zh-CN)
//      }
//      else if (c >= 0x3040 && c <= 0x30FF) // Hiragana & Katakana (Japanese)
//      {
//         return 1041; // Japanese (ja-JP)
//      }
//      else if (c >= 0xAC00 && c <= 0xD7AF) // Hangul Syllables (Korean)
//      {
//         return 1042; // Korean (ko-KR)
//      }
//      else if (c >= 0x3100 && c <= 0x312F) // Bopomofo (Traditional Chinese phonetic)
//      {
//         return 1028; // Traditional Chinese (zh-TW)
//      }
//      else
//      {
//         return 1033; // Default to English (en-US)
//      }
//   }

//}


