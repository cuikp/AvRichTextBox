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
    /// document field element
    /// </summary>
    [Serializable()]
    public class RTFDomField : RTFDomElement
    {
        /// <summary>
        /// initialize instance
        /// </summary>
        public RTFDomField()
        {
        }

        private RTFDomFieldMethod intMethod = RTFDomFieldMethod.None;
        /// <summary>
        /// method
        /// </summary>
        [System.ComponentModel.DefaultValue(RTFDomFieldMethod.None)]
        public RTFDomFieldMethod Method
        {
            get
            {
                return intMethod;
            }
            set
            {
                intMethod = value;
            }
        }

        //private string strInstructions = null;
        /// <summary>
        /// instructions
        /// </summary>
        [System.ComponentModel.DefaultValue(null)]
        public string Instructions
        {
            get
            {
                foreach (RTFDomElement element in this.Elements)
                {
                    if (element is RTFDomElementContainer)
                    {
                        RTFDomElementContainer c = (RTFDomElementContainer)element;
                        if (c.Name == RTFConsts._fldinst)
                        {
                            return c.InnerText;
                        }
                    }
                }
                return null ;
            }
            //set
            //{
            //    strInstructions = value;
            //}
        }

        /// <summary>
        /// result
        /// </summary>
        [System.ComponentModel.DefaultValue(null)]
        public RTFDomElementContainer Result
        {
            get
            {
                foreach (RTFDomElement element in this.Elements)
                {
                    if (element is RTFDomElementContainer)
                    {
                        RTFDomElementContainer c = (RTFDomElementContainer)element;
                        if (c.Name == RTFConsts._fldrslt)
                        {
                            return c;
                        }
                    }
                }
                return null;
            }
            //set
            //{
            //    strResult = value;
            //}
        }

        public string ResultString
        {
            get
            {
                RTFDomElementContainer c = this.Result;
                if (c != null)
                {
                    return c.InnerText;
                }
                else
                {
                    return null;
                }
            }
        }
        public override string ToString()
        {
            return "Field";// +strInstructions + " Result:" + this.ResultString;
        }

    }//public class RTFDomField : RTFDomElement


    public enum RTFDomFieldMethod
    {
        None,
        Dirty,
        Edit,
        Lock,
        Priv,
    }

}
