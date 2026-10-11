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
    /// rtf attribute
    /// </summary>
    [Serializable()]
    public class RTFAttribute
    {
        /// <summary>
        /// initialize instance
        /// </summary>
        public RTFAttribute()
        {
        }

        private string strName = null!;
        /// <summary>
        /// attribute's name
        /// </summary>
        [System.ComponentModel.DefaultValue( null)]
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

        private int intValue = int.MinValue ;
        /// <summary>
        /// value
        /// </summary>
        [System.ComponentModel.DefaultValue( int.MinValue )]
        public int Value
        {
            get
            {
                return intValue; 
            }
            set
            {
                intValue = value; 
            }
        }
        public override string ToString()
        {
            return strName + "=" + intValue;
        }
    }

    /// <summary>
    /// RTF attribute list
    /// </summary>
    [Serializable()]
    [System.Diagnostics.DebuggerTypeProxy(typeof(RTFInstanceDebugView))]
    public class RTFAttributeList : System.Collections.CollectionBase
    {
        /// <summary>
        /// initialize instance
        /// </summary>
        public RTFAttributeList()
        {
        }

        public RTFAttribute GetItem(int index)
        {
            return (RTFAttribute)this.List[index];
        }

        public int this[string name]
        {
            get
            {
                foreach (RTFAttribute a in this)
                {
                    if (a.Name == name)
                        return a.Value;
                }
                return int.MinValue;
            }
            set
            {
                foreach (RTFAttribute a in this)
                {
                    if (a.Name == name)
                    {
                        a.Value = value;
                        return;
                    }
                }
                RTFAttribute item = new RTFAttribute
                {
                    Name = name,
                    Value = value
                };
                this.List.Add(item);
            }
        }

        public int Add(RTFAttribute item)
        {
            return this.List.Add(item);
        }

        public int Add(string name, int v)
        {
            RTFAttribute item = new()
            {
                Name = name,
                Value = v
            };
            return this.List.Add(item);
        }

        public void Remove(RTFAttribute item)
        {
            this.List.Remove(item);
        }

        public void Remove(string name)
        {
            for (int iCount = this.Count - 1; iCount >= 0; iCount--)
            {
                RTFAttribute item = (RTFAttribute)this.List[iCount]!;
                if (item?.Name == name)
                {
                    this.List.RemoveAt(iCount);
                }
            }
        }

        public bool Contains(RTFAttribute item)
        {
            return this.List.Contains(item);
        }

        public bool Contains(string name)
        {
            foreach (RTFAttribute a in this)
            {
                if (a.Name == name)
                    return true;
            }
            return false;
        }

        public RTFAttributeList Clone()
        {
            RTFAttributeList list = [];
            foreach (RTFAttribute item in this)
            {
                RTFAttribute newItem = new RTFAttribute();
                newItem.Name = item.Name;
                newItem.Value = item.Value;
                list.List.Add(newItem);
            }
            return list;
        }
    }
}
