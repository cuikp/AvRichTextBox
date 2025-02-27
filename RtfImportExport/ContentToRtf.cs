using Avalonia;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace AvRichTextBox;

internal static partial class RtfConversions
{
   internal static string GetRtfFromFlowDocumentBlocks(IEnumerable<Block> blocks)
   {

      var sb = new StringBuilder();

      sb.Append(@"{\rtf1\ansi\deff0 {\fonttbl{\f0\fswiss Arial;}}");

      bool BoldOn = false;
      bool ItalicOn = false;
      bool UnderlineOn = false;
      int CurrentLang = 1033;

      foreach (Block block in blocks)
      {
         if (block.GetType() == typeof(Paragraph))
         {
            Paragraph p = (Paragraph)block;
            foreach (IEditable ied in p.Inlines)
               sb.Append(GetIEditableRtf(ied, ref BoldOn, ref ItalicOn, ref UnderlineOn, ref CurrentLang));

            sb.Append(@"\par ");
         }
      }
      sb.Remove(sb.Length - 5, 5);  // remove final \par
      sb.Append('}');
      return sb.ToString();
   }

   internal static string GetRtfFromInlines(List<IEditable> inlines)
   {
      var sb = new StringBuilder();

      //sb.Append(@"{\rtf1\ansi\deff0 {\fonttbl{\f0\fswiss Arial;}}");
      sb.Append(@"{\rtf1\ansi\deff0 {\fonttbl{\f0\fnil Arial;}}");

      bool BoldOn = false;
      bool ItalicOn = false;
      bool UnderlineOn = false;
      int CurrentLang = 1033;

      foreach (IEditable ied in inlines)
      {
         sb.Append(GetIEditableRtf(ied, ref BoldOn, ref ItalicOn, ref UnderlineOn, ref CurrentLang));
         
         if (ied.InlineText.EndsWith('\r'))
            sb.Append(@"\par ");
      }
      
      sb.Append('}');

      return sb.ToString();
   }

   private static string GetIEditableRtf(IEditable ied, ref bool BoldOn, ref bool ItalicOn, ref bool UnderlineOn, ref int currentLang)
   {
      StringBuilder iedSB = new();

      if (ied.GetType() == typeof(EditableRun))
      {
         EditableRun run = (EditableRun)ied;

         if (!BoldOn && run.FontWeight == FontWeight.Bold) { iedSB.Append(@"\b "); BoldOn = true; }
         if (!ItalicOn && run.FontStyle == FontStyle.Italic) { iedSB.Append(@"\i "); ; ItalicOn = true; }
         if (!UnderlineOn && run.TextDecorations == TextDecorations.Underline) { iedSB.Append(@"\ul "); ; UnderlineOn = true; }

         if (BoldOn && run.FontWeight == FontWeight.Normal) { iedSB.Append(@"\b0 "); BoldOn = false; }
         if (ItalicOn && run.FontStyle == FontStyle.Normal) { iedSB.Append(@"\i0 "); ItalicOn = false; }
         if (UnderlineOn && run.TextDecorations != TextDecorations.Underline) { iedSB.Append(@"\ul0 "); UnderlineOn = false; }

         if (run.FontSize > 0) iedSB.Append($@"\fs{(int)(run.FontSize * 2)} ");

         if (!string.IsNullOrEmpty(run.Text))
            iedSB.Append(GetRtfRunText(run.Text!, ref currentLang));
      }

      return iedSB.ToString();
   }

   private static string GetRtfRunText(string text, ref int currentLang)
   {
      StringBuilder sb = new();

      foreach (char c in text)
      {
         int newLang = GetLanguageForChar(c);

         if (newLang != currentLang)
         {
            sb.Append($@"\lang{newLang} ");
            currentLang = newLang;
         }

         if (c is '\\' or '{' or '}')
            sb.Append(@"\" + c); // RTF control characters
         else if (c > 127) // Non-ASCII (double-byte characters)
            sb.Append(@"\u" + (int)c + "?"); // Unicode escape
         else
            sb.Append(c);

      }
      return sb.ToString();
   }


   private static int GetLanguageForChar(char c)
   {
      if (c is >= (char)0x4E00 and <= (char)0x9FFF or // CJK Unified Ideographs (Common Chinese, Japanese, Korean)
          >= (char)0x3400 and <= (char)0x4DBF)  // CJK Extension A (Rare Chinese characters)
         return 2052; // Simplified Chinese (zh-CN)
      else if (c is >= (char)0x3040 and <= (char)0x30FF) // Hiragana & Katakana (Japanese)
         return 1041; // Japanese (ja-JP)
      else if (c is >= (char)0xAC00 and <= (char)0xD7AF) // Hangul Syllables (Korean)
         return 1042; // Korean (ko-KR)
      else if (c is >= (char)0x3100 and <= (char)0x312F) // Bopomofo (Traditional Chinese phonetic)
         return 1028; // Traditional Chinese (zh-TW)
      else
         return 1033; // Default to English (en-US)
   }

}