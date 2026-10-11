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
    /// RTF element container
    /// </summary>
    [Serializable()]
    public class RTFDomElementContainer : RTFDomElement
    {
        /// <summary>
        /// initialize instance
        /// </summary>
        public RTFDomElementContainer()
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
            return "Container : " + strName;
        }
    }
}
