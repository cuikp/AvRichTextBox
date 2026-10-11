/*
 * 
 *  RTFDomParserAv, modified from original:
 *   DCSoft RTF DOM v1.0
 *   Author : Yuan yong fu.
 *   Email  : yyf9989@hotmail.com
 *   blog site:http://www.cnblogs.com/xdesigner.
 * 
 */



using System;
using System.Text;

namespace RtfDomParserAv
{
    /// <summary>
    /// paragraph element
    /// </summary>
    [Serializable()]
    public class RTFDomParagraph : RTFDomElement
    {
        /// <summary>
        /// initialize instance
        /// </summary>
        public RTFDomParagraph()
        {
        }

        internal bool TemplateGenerated = false;
        /// <summary>
        /// 是否是临时生成的段落对蟻E
        /// </summary>
        public bool IsTemplateGenerated
        {
            get
            {
                return TemplateGenerated;
            }
        }
        private DocumentFormatInfo myFormat = new DocumentFormatInfo();
        /// <summary>
        /// format
        /// </summary>
        public DocumentFormatInfo Format
        {
            get
            {
                return myFormat; 
            }
            set
            {
                myFormat = value; 
            }
        }
        public override string InnerText
        {
            get
            {
                return base.InnerText + Environment.NewLine ;
            }
        }
        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append("Paragraph");
            if (this.Format != null)
            {
                str.Append("(" + this.Format.Align + ")");
                if (this.Format.ListID >= 0)
                {
                    str.Append("ListID:" + this.Format.ListID);
                }
                //if (this.Format.NumberedList)
                //{
                //    str.Append("(NumberedList)");
                //}
                //else if (this.Format.BulletedList)
                //{
                //    str.Append("(BulletedList)");
                //}
            }
            
            return str.ToString();
        }
    }
}
