using System.Diagnostics;
using RtfDomParser;
using System.Text;
using System.Collections.Generic;
using Avalonia.Controls.Documents;
using Avalonia.Media;


namespace AvRichTextBox;

internal partial class RtfConversions
{

   internal static void TestBuildRTF(RTFWriter w)
   {
      w.Encoding = System.Text.Encoding.GetEncoding(936);
      // write header
      w.WriteStartGroup();
      w.WriteKeyword("rtf1");
      w.WriteKeyword("ansi");
      w.WriteKeyword("ansicpg" + w.Encoding.CodePage);
      // wirte font table
      w.WriteStartGroup();
      w.WriteKeyword("fonttbl");
      w.WriteStartGroup();
      w.WriteKeyword("f0");
      w.WriteText("Arial;");
      w.WriteEndGroup();
      w.WriteStartGroup();
      w.WriteKeyword("f1");
      w.WriteText("Times New Roman;");
      w.WriteEndGroup();
      w.WriteEndGroup();
      // write color table
      w.WriteStartGroup();
      w.WriteKeyword("colortbl");
      w.WriteText(";");
      w.WriteKeyword("red0");
      w.WriteKeyword("green0");
      w.WriteKeyword("blue255");
      w.WriteText(";");
      w.WriteEndGroup();
      // write content
      w.WriteKeyword("qc"); // set alignment center
      w.WriteKeyword("f0"); // set font
      w.WriteKeyword("fs30"); // set font size
      w.WriteText("This is the first paragraph text ");
      w.WriteKeyword("cf1"); // set text color
      w.WriteText("Arial ");
      w.WriteKeyword("cf0"); // set default color
      w.WriteKeyword("f1"); // set font
      w.WriteText("Align center ABC12345");
      w.WriteKeyword("par"); // new paragraph
      w.WriteKeyword("pard"); // clear format
      w.WriteKeyword("f1"); // set font 
      w.WriteKeyword("fs20"); // set font size
      w.WriteKeyword("cf1");
      w.WriteText("This is the secend paragraph Arial left alignment ABC12345");
      // finish
      w.WriteEndGroup();
   }

   public static string ConvertBlocksToRtf(IEnumerable<Block> blocks)
   {
      var sb = new StringBuilder();
      //sb.Append(@"{\rtf1\ansi ");
      sb.Append(@"{\rtf1\ansi\deff0 {\fonttbl{\f0\fswiss Arial;}}");    

      bool BoldOn = false;
      bool ItalicOn = false;
      bool UnderlineOn = false;

      foreach (Block block in blocks)
      {
         if (block.GetType() == typeof(Paragraph))
         {
            Paragraph p = (Paragraph)block;
            foreach (Inline iline in p.Inlines)
            {
               if (iline.GetType() == typeof(EditableRun))
               {
                  EditableRun run = (EditableRun)iline;

                  if (!BoldOn && run.FontWeight == FontWeight.Bold) { sb.Append(@"\b "); BoldOn = true; }
                  if (!ItalicOn && run.FontStyle == FontStyle.Italic) {sb.Append(@"\i "); ; ItalicOn = true; }
                  if (!UnderlineOn && run.TextDecorations == TextDecorations.Underline) { sb.Append(@"\ul "); ; UnderlineOn = true;}
            
                  if (run.FontSize > 0) sb.Append($@"\fs{(int)(run.FontSize * 2)} ");

                  if (!string.IsNullOrEmpty(run.Text))
                     sb.Append(GetRtfRunText(run.Text!));

                  //Debug.WriteLine("text = " + run.Text);

                  if (BoldOn && run.FontWeight == FontWeight.Normal) { sb.Append(@"\b0 "); BoldOn = false; }
                  if (ItalicOn && run.FontStyle == FontStyle.Normal) { sb.Append(@"\i0 "); ItalicOn = false; }
                  if (UnderlineOn && run.TextDecorations != TextDecorations.Underline) {sb.Append(@"\ul0 "); UnderlineOn = false; }

         }
            }
            sb.Append(@"\par ");
         }
      }

      sb.Append('}');
      return sb.ToString();
   }

   private static string EscapeRtf(string text)
   {
      return text.Replace(@"\", @"\\").Replace("{", @"\{").Replace("}", @"\}");
   }

   private static string GetRtfRunText(string text)
   {
      StringBuilder sb = new ();
      int currentLang = 1033;

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
         {
            sb.Append(@"\u" + (int)c + "?"); // Unicode escape
         }

         else
            sb.Append(c);
            
      }
      return sb.ToString();
   }


  
   // Determine the appropriate \lang tag based on Unicode range
   private static int GetLanguageForChar(char c)
   {
      if ((c >= 0x4E00 && c <= 0x9FFF) || // CJK Unified Ideographs (Common Chinese, Japanese, Korean)
          (c >= 0x3400 && c <= 0x4DBF))  // CJK Extension A (Rare Chinese characters)
      {
         return 2052; // Simplified Chinese (zh-CN)
      }
      else if (c >= 0x3040 && c <= 0x30FF) // Hiragana & Katakana (Japanese)
      {
         return 1041; // Japanese (ja-JP)
      }
      else if (c >= 0xAC00 && c <= 0xD7AF) // Hangul Syllables (Korean)
      {
         return 1042; // Korean (ko-KR)
      }
      else if (c >= 0x3100 && c <= 0x312F) // Bopomofo (Traditional Chinese phonetic)
      {
         return 1028; // Traditional Chinese (zh-TW)
      }
      else
      {
         return 1033; // Default to English (en-US)
      }
   }

}


