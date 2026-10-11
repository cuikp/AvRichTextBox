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
    /// table
    /// </summary>
    [Serializable()]
    public class RTFDomTable : RTFDomElement
    {
        /// <summary>
        /// initialize instance
        /// </summary>
        public RTFDomTable()
        {
        }

        private RTFDomElementList myColumns = new RTFDomElementList();
        /// <summary>
        /// column list
        /// </summary>
        public RTFDomElementList Columns
        {
            get
            {
                return myColumns; 
            }
            set
            {
                myColumns = value; 
            }
        }

        public override string ToString()
        {
            return "Table(Rows:" + this.Elements.Count + " Columns:" + myColumns.Count + ")";
        }
    }
}
