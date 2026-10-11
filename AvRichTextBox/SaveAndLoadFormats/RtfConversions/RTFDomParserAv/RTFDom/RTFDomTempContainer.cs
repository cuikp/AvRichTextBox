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
using System.Collections.Generic;
using System.Text;

namespace RtfDomParserAv
{
    public class RTFDomTempContainer: RTFDomElement
    {

        public RTFDomTempContainer()
        { 
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

        public override string ToString()
        {
            return "TempContainer";
        }

    }
}
