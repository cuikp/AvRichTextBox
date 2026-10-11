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
using System.ComponentModel ;

namespace RtfDomParserAv
{
    /// <summary>
    /// shape element
    /// </summary>
    [Serializable()]
    public class RTFDomShape : RTFDomElement
    {
        /// <summary>
        /// initialize instance
        /// </summary>
        public RTFDomShape()
        {
        }

        private int intLeft = 0;
        /// <summary>
        /// left position
        /// </summary>
        [DefaultValue( 0 )]
        public int Left
        {
            get
            {
                return intLeft;
            }
            set
            {
                intLeft = value;
            }
        }

        private int intTop = 0;
        /// <summary>
        /// top position
        /// </summary>
        [DefaultValue( 0 )]
        public int Top
        {
            get
            {
                return intTop;
            }
            set
            {
                intTop = value;
            }
        }

        private int intWidth = 0;
        /// <summary>
        /// width
        /// </summary
        [DefaultValue(0)]
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

        private int intHeight = 0;
        /// <summary>
        /// height
        /// </summary>
        [DefaultValue(0)]
        public int Height
        {
            get
            {
                return intHeight;
            }
            set
            {
                intHeight = value;
            }
        }

        private int intZIndex = 0;
        /// <summary>
        /// Z index
        /// </summary>
        [DefaultValue(0)]
        public int ZIndex
        {
            get
            {
                return intZIndex;
            }
            set
            {
                intZIndex = value;
            }
        }

        private int intShapeID = 0;
        /// <summary>
        /// shape id
        /// </summary>
        [DefaultValue(0)]
        public int ShapeID
        {
            get
            {
                return intShapeID;
            }
            set
            {
                intShapeID = value;
            }
        }

        private StringAttributeCollection myExtAttrbutes = new StringAttributeCollection();
        /// <summary>
        /// ext attribute
        /// </summary>
        public StringAttributeCollection ExtAttrbutes
        {
            get
            {
                return myExtAttrbutes; 
            }
            set
            {
                myExtAttrbutes = value; 
            }
        }

        public override string ToString()
        {
            return "Shape:Left:" + intLeft + " Top:" + intTop + " Width:" + intWidth + " Height:" + intHeight;
        }
    }
}
