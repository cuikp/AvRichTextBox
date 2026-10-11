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
using System.Collections;
using System.ComponentModel;

namespace RtfDomParserAv
{
    /// <summary>
    /// RTF dom element
    /// </summary>
    /// <remarks>this is the most base element type</remarks>
    public abstract class RTFDomElement
    {
        private RTFAttributeList myAttributes = new RTFAttributeList();
        /// <summary>
        /// RTF native attribute
        /// </summary>
        public RTFAttributeList Attributes
        {
            get
            {
                return myAttributes;
            }
            set
            {
                myAttributes = value;
            }
        }

        public bool HasAttribute(string name)
        {
            return myAttributes.Contains(name);
        }

        public int GetAttributeValue( string name , int defaultValue )
        {
            if( myAttributes.Contains( name ))
                return myAttributes[ name ] ;
            else
                return defaultValue ;
        }

        private RTFDomElementList myElements = new RTFDomElementList();
        /// <summary>
        /// child elements list
        /// </summary>
        public RTFDomElementList Elements
        {
            get
            {
                return myElements;
            }
        }

        private RTFDomDocument myOwnerDocument = null;
        /// <summary>
        /// the docuemnt which owned this element
        /// </summary>
        [System.ComponentModel.Browsable( false )]
        [System.Xml.Serialization.XmlIgnore()]
        public RTFDomDocument OwnerDocument
        {
            get
            {
                return myOwnerDocument;
            }
            set
            {
                myOwnerDocument = value;
                foreach (RTFDomElement element in this.Elements)
                {
                    element.OwnerDocument = value;
                }
            }
        }
        /// <summary>
        /// append child element
        /// </summary>
        /// <param name="element">child element</param>
        /// <returns>index of element</returns>
        public int AppendChild(RTFDomElement element)
        {
            CheckLocked();
            element.myParent = this;
            element.OwnerDocument = this.myOwnerDocument;
            return myElements.Add(element);
        }

        /// <summary>
        /// set attribute
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="Value">value</param>
        public void SetAttribute(string name, int Value)
        {
            CheckLocked();
            this.myAttributes[name] = Value;
        }

        private RTFDomElement myParent = null;
        /// <summary>
        /// parent element
        /// </summary>
        [System.ComponentModel.Browsable( false )]
        public RTFDomElement Parent
        {
            get
            {
                return myParent;
            }
        }

        [System.ComponentModel.Browsable( false )]
        public virtual string InnerText
        {
            get
            {
                StringBuilder str = new StringBuilder();
                if (myElements != null)
                {
                    foreach (RTFDomElement element in this.myElements)
                    {
                        str.Append(element.InnerText);
                    }
                }
                return str.ToString();
            }
        }


        private void CheckLocked()
        {
            if (bolLocked)
            {
                throw new InvalidOperationException("Element locked");
            }
        }

        private bool bolLocked = false;
        /// <summary>
        /// Whether element is locked , if element is lock , it can not append chidl element
        /// </summary>
        [System.Xml.Serialization.XmlIgnore( )]
        [System.ComponentModel.Browsable( false )]
        public bool Locked
        {
            get 
            {
                return bolLocked; 
            }
            set
            {
                bolLocked = value; 
            }
        }

        public void SetLockedDeeply( bool locked )
        {
            this.bolLocked = locked;
            if (this.myElements != null)
            {
                foreach (RTFDomElement element in myElements)
                {
                    element.SetLockedDeeply(locked);
                }
            }
        }

        public void PrintDomString()
        {
            System.Console.WriteLine(this.ToDomString());
        }

        public virtual string ToDomString()
        {
            System.Text.StringBuilder builder = new StringBuilder();
            builder.Append(this.ToString());
            ToDomString(this.Elements, builder, 1);
            return builder.ToString();
        }

        protected void ToDomString(RTFDomElementList elements, System.Text.StringBuilder builder, int level)
        {
            foreach (RTFDomElement element in elements)
            {
                builder.Append(Environment.NewLine);
                builder.Append( new string( ' ' , level * 4 ));
                builder.Append(element.ToString());
                ToDomString(element.Elements, builder, level + 1);
            }
        }

        /// <summary>
        /// Native level in RTF document
        /// </summary>
        [NonSerialized()]
        internal int NativeLevel = -1;
    }
}
