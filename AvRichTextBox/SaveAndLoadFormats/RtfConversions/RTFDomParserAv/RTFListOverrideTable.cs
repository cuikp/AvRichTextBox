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
    public class RTFListOverrideTable : List<RTFListOverride>
    {
        public RTFListOverrideTable()
        {
        }

        public RTFListOverride GetByID(int id)
        {
            foreach (RTFListOverride item in this)
            {
                if (item.ID == id)
                {
                    return item;
                }
            }
            return null;
        }
    }

    public class RTFListOverride
    {
        private int _ListID = 0;

        public int ListID
        {
            get { return _ListID; }
            set { _ListID = value; }
        }

        private int _ListOverriedCount = 0;

        public int ListOverriedCount
        {
            get { return _ListOverriedCount; }
            set { _ListOverriedCount = value; }
        }

        private int _ID = 1;

        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        public override string ToString()
        {
            return "ID:" + this.ID + " ListID:" + this.ListID + " Count:" + this.ListOverriedCount;
        }
    }
}
