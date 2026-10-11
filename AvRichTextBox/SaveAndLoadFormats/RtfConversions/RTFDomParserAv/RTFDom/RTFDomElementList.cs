/*
 * 
 *  RTFDomParserAv, modified from original:
 *   DCSoft RTF DOM v1.0
 *   Author : Yuan yong fu.
 *   Email  : yyf9989@hotmail.com
 *   blog site:http://www.cnblogs.com/xdesigner.
 * 
 */

namespace RtfDomParserAv
{
    [Serializable()]
    [System.Diagnostics.DebuggerTypeProxy(typeof(RTFInstanceDebugView))]
    public class RTFDomElementList : System.Collections.CollectionBase
    {
        public RTFDomElementList()
        {
        }

        public RTFDomElement this[int index] => (RTFDomElement)this.List[index]!;
        
        public RTFDomElement LastElement
        {
            get
            {
                if (this.Count > 0)
                    return (RTFDomElement)this.List[this.Count - 1]!;
                else
                    return null!;
            }
        }

        public int Add(RTFDomElement element ) => this.List.Add( element );

        public void Insert(int index, RTFDomElement element)
        {
            this.List.Insert(index, element);
        }

        /// <summary>
        /// Get the index of special element that starts with 0.
        /// </summary>
        /// <param name="element">element</param>
        /// <returns>index , if not find element , then return -1</returns>
        public int IndexOf(RTFDomElement element) => this.List.IndexOf(element);
        
        public void Remove(RTFDomElement node)
        {
            this.List.Remove(node);
        }
      
        public RTFDomElement[] ToArray() => (RTFDomElement[])this.InnerList.ToArray(typeof(RTFDomElement));

    }
}
