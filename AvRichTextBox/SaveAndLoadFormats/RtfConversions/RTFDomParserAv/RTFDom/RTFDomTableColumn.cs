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
    /// table column
    /// </summary>
    [Serializable()]
    public class RTFDomTableColumn : RTFDomElement
    {
        /// <summary>
        /// initialize instance
        /// </summary>
        public RTFDomTableColumn()
        {
        }

        private int intWidth = 0;
        /// <summary>
        /// width
        /// </summary>
        [System.ComponentModel.DefaultValue( 0 )]
        public int Width
        {
            get
            {
                return intWidth; 
            }
            set
            {
                intWidth = value; 
            }
        }

        public override string ToString()
        {
            return "Column";
        }
    }
}
