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
    /// rtf bookmark
    /// </summary>
    [Serializable()]
    public class RTFDomBookmark : RTFDomElement
    {
        public RTFDomBookmark()
        { 
        }

        private string strName = null;
        /// <summary>
        /// name
        /// </summary>
        [System.ComponentModel.DefaultValue( null )]
        public string Name
        {
            get
            {
                return strName; 
            }
            set
            {
                strName = value; 
            }
        }

        public override string ToString()
        {
            return "BookMark:" + strName;
        }
    }
}
