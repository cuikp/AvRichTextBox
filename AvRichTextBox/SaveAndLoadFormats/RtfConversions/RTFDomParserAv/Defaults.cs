/*
 * 
 *  RTFDomParserAv, modified from original:
 *   DCSoft RTF DOM v1.0
 *   Author : Yuan yong fu.
 *   Email  : yyf9989@hotmail.com
 *   blog site:http://www.cnblogs.com/xdesigner.
 * 
 */

using System.Text;

namespace RtfDomParserAv
{
    public static class Defaults
    {
        public static string FontName { get; set; } = "Times New Roman";

        // RTF default code page (\ansi). Encoding.Default is UTF-8 on .NET Core and decodes \'xx bytes as U+FFFD.
        internal static Encoding AnsiEncoding
        {
            get
            {
                LoadEncodings();
                return Encoding.GetEncoding(1252);
            }
        }

        public static void LoadEncodings()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}