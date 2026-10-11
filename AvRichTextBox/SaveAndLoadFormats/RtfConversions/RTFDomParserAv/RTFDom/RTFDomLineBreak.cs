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
    /// line element
    /// </summary>
    [Serializable()]
    public class RTFDomLineBreak : RTFDomElement
    {
        /// <summary>
        /// initialize instance
        /// </summary>
        public RTFDomLineBreak()
        {
            this.Locked = true;
        }

        public override string InnerText
        {
            get
            {
                return Environment.NewLine;
            }
        }
        public override string ToString()
        {
            return "linebreak";
        }
    }
}
