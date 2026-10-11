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
    /// cell 
    /// </summary>
    [Serializable()]
    public class RTFDomTableCell : RTFDomElement
    {
        /// <summary>
        /// initialize instance
        /// </summary>
        public RTFDomTableCell()
        {
            myFormat.BorderWidth = 1;
        }

        private int intRowSpan = 1;
        /// <summary>
        /// row span
        /// </summary>
        [System.ComponentModel.DefaultValue(1)]
        public int RowSpan
        {
            get
            {
                return intRowSpan;
            }
            set
            {
                intRowSpan = value;
            }
        }

        private int intColSpan = 1;
        /// <summary>
        /// col span
        /// </summary>
        [System.ComponentModel.DefaultValue(1)]
        public int ColSpan
        {
            get
            {
                return intColSpan;
            }
            set
            {
                intColSpan = value;
            }
        }

        private int intPaddingLeft = int.MinValue;
        /// <summary>
        /// left padding
        /// </summary>
        [System.ComponentModel.DefaultValue(int.MinValue)]
        public int PaddingLeft
        {
            get
            {
                return intPaddingLeft;
            }
            set
            {
                intPaddingLeft = value;
            }
        }

        /// <summary>
        /// left padding in fact
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        [System.Xml.Serialization.XmlIgnore()]
        public int RuntimePaddingLeft
        {
            get
            {
                if (intPaddingLeft != int.MinValue)
                {
                    return intPaddingLeft;
                }
                else if (this.Parent != null)
                {
                    int p = ((RTFDomTableRow)this.Parent).PaddingLeft;
                    if (p != int.MinValue)
                    {
                        return p;
                    }
                }
                return 0;
            }
        }

        private int intPaddingTop = int.MinValue;
        /// <summary>
        /// top padding
        /// </summary>
        [System.ComponentModel.DefaultValue(int.MinValue)]
        public int PaddingTop
        {
            get
            {
                return intPaddingTop;
            }
            set
            {
                intPaddingTop = value;
            }
        }

        /// <summary>
        /// top padding in fact
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        [System.Xml.Serialization.XmlIgnore()]
        public int RuntimePaddingTop
        {
            get
            {
                if (intPaddingTop != int.MinValue)
                {
                    return intPaddingTop;
                }
                else if (this.Parent != null)
                {
                    int p = ((RTFDomTableRow)this.Parent).PaddingTop;
                    if (p != int.MinValue)
                    {
                        return p;
                    }
                }
                return 0;
            }
        }

        private int intPaddingRight = int.MinValue;
        /// <summary>
        /// right padding
        /// </summary>
        [System.ComponentModel.DefaultValue(int.MinValue)]
        public int PaddingRight
        {
            get
            {
                return intPaddingRight;
            }
            set
            {
                intPaddingRight = value;
            }
        }

        /// <summary>
        /// right padding in fact
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        [System.Xml.Serialization.XmlIgnore()]
        public int RuntimePaddingRight
        {
            get
            {
                if (intPaddingRight != int.MinValue)
                {
                    return intPaddingRight;
                }
                else if (this.Parent != null)
                {
                    int p = ((RTFDomTableRow)this.Parent).PaddingRight;
                    if (p != int.MinValue)
                    {
                        return p;
                    }
                }
                return 0;
            }
        }

        private int intPaddingBottom = int.MinValue;
        /// <summary>
        /// bottom padding
        /// </summary>
        [System.ComponentModel.DefaultValue(int.MinValue)]
        public int PaddingBottom
        {
            get
            {
                return intPaddingBottom;
            }
            set
            {
                intPaddingBottom = value;
            }
        }

        /// <summary>
        /// bottom padding in fact
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        [System.Xml.Serialization.XmlIgnore()]
        public int RuntimePaddingBottom
        {
            get
            {
                if (intPaddingBottom != int.MinValue)
                {
                    return intPaddingBottom;
                }
                else if (this.Parent != null)
                {
                    int p = ((RTFDomTableRow)this.Parent).PaddingBottom;
                    if (p != int.MinValue)
                    {
                        return p;
                    }
                }
                return 0;
            }
        }

        //private bool bolLeftBorder = false;
        //[System.ComponentModel.DefaultValue(false)]
        //public bool LeftBorder
        //{
        //    get
        //    {
        //        return bolLeftBorder;
        //    }
        //    set
        //    {
        //        bolLeftBorder = value;
        //    }
        //}

        //private bool bolTopBorder = false;
        //[System.ComponentModel.DefaultValue(false)]
        //public bool TopBorder
        //{
        //    get
        //    {
        //        return bolTopBorder;
        //    }
        //    set
        //    {
        //        bolTopBorder = value;
        //    }
        //}


        //private bool bolRightBorder = false;
        //[System.ComponentModel.DefaultValue(false)]
        //public bool RightBorder
        //{
        //    get
        //    {
        //        return bolRightBorder;
        //    }
        //    set
        //    {
        //        bolRightBorder = value;
        //    }
        //}

        //private bool bolBottomBorder = false;

        //[System.ComponentModel.DefaultValue(false)]
        //public bool BottomBorder
        //{
        //    get
        //    {
        //        return bolBottomBorder;
        //    }
        //    set
        //    {
        //        bolBottomBorder = value;
        //    }
        //}

        private RTFVerticalAlignment intVerticalAlignment = RTFVerticalAlignment.Top;
        /// <summary>
        /// vertial alignment
        /// </summary>
        [System.ComponentModel.DefaultValue(RTFVerticalAlignment.Top)]
        public RTFVerticalAlignment VerticalAlignment
        {
            get
            {
                return intVerticalAlignment;
            }
            set
            {
                intVerticalAlignment = value;
            }
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

        private bool bolMultiline = true;
        /// <summary>
        /// allow multiline
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool Multiline
        {
            get
            {
                return bolMultiline;
            }
            set
            {
                bolMultiline = value;
            }
        }

        //private System.Drawing.Color intBorderColor = System.Drawing.Color.Black;
        ///// <summary>
        ///// border color
        ///// </summary>
        //[System.ComponentModel.DefaultValue(typeof(System.Drawing.Color), "Black")]
        //public System.Drawing.Color BorderColor
        //{
        //    get
        //    {
        //        return intBorderColor;
        //    }
        //    set
        //    {
        //        intBorderColor = value;
        //    }
        //}

        //private System.Drawing.Color intBackColor = System.Drawing.Color.Transparent;
        ///// <summary>
        ///// back color
        ///// </summary>
        //[System.ComponentModel.DefaultValue(typeof(System.Drawing.Color), "Transparent")]
        //public System.Drawing.Color BackColor
        //{
        //    get
        //    {
        //        return intBackColor;
        //    }
        //    set
        //    {
        //        intBackColor = value;
        //    }
        //}
         

        private int intLeft = 0;
        /// <summary>
        /// left position
        /// </summary>
        [System.ComponentModel.DefaultValue(0)]
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

        private int intHeight = 0;
        /// <summary>
        /// height
        /// </summary>
        [System.ComponentModel.DefaultValue(0)]
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

        private RTFDomTableCell myOverrideCell = null;
        /// <summary>
        /// this cell merged by another cell which this property specify
        /// </summary>
        [System.ComponentModel.Browsable( false )]
        [System.Xml.Serialization.XmlIgnore()]
        public RTFDomTableCell OverrideCell
        {
            get
            {
                return myOverrideCell;
            }
            set
            {
                myOverrideCell = value;
            }
        }

        public override string ToString()
        {
            if (myOverrideCell == null)
            {
                if (intRowSpan != 1 || intColSpan != 1)
                {
                    return "Cell: RowSpan:" + intRowSpan + " ColSpan:" + intColSpan + " Width:" + this.Width ;
                }
                else
                {
                    return "Cell:Width:" + this.Width ;
                }
            }
            else
            {
                return "Cell:Overrided";
            }
        }
    }

}
