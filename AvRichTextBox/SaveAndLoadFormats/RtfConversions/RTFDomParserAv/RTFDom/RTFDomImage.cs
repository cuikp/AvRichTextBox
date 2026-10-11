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
    /// image element
    /// </summary>
    [Serializable()]
    public class RTFDomImage : RTFDomElement
    {
        /// <summary>
        /// initialize instance
        /// </summary>
        public RTFDomImage()
        {
        }

        private string strID = null;
        /// <summary>
        /// id
        /// </summary>
        [System.ComponentModel.DefaultValue(null)]
        public string ID
        {
            get
            {
                return strID;
            }
            set
            {
                strID = value;
            }
        }

        private byte[] bsData = null;
        /// <summary>
        /// data
        /// </summary>
        [System.ComponentModel.Browsable( false )]
        [System.Xml.Serialization.XmlIgnore()]
        public byte[] Data
        {
            get
            {
                return bsData; 
            }
            set
            {
                bsData = value; 
            }
        }

        [System.ComponentModel.Browsable( false )]
        [System.Xml.Serialization.XmlElement()]
        [System.ComponentModel.DesignerSerializationVisibility( System.ComponentModel.DesignerSerializationVisibility.Hidden )]
        public string Base64Data
        {
            get
            {
                if (bsData == null)
                    return null;
                else
                    return Convert.ToBase64String(bsData);
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    bsData = Convert.FromBase64String(value);
                }
                else
                {
                    bsData = null;
                }
            }
        }

        private int intScaleX = 100;
        /// <summary>
        /// scale rate at the X coordinate, in percent unit.
        /// </summary>
        [System.ComponentModel.DefaultValue(100)]
        public int ScaleX
        {
            get
            {
                return intScaleX;
            }
            set
            {
                intScaleX = value;
            }
        }

        private int intScaleY = 100;
        /// <summary>
        /// scale rate at the Y coordinate , in percent unit.
        /// </summary>
        [System.ComponentModel.DefaultValue(100)]
        public int ScaleY
        {
            get
            {
                return intScaleY;
            }
            set
            {
                intScaleY = value;
            }
        }

        private int intDesiredWidth = 0;
        /// <summary>
        /// desired width
        /// </summary>
        [System.ComponentModel.DefaultValue( 0 )]
        public int DesiredWidth
        {
            get
            {
                return intDesiredWidth; 
            }
            set
            {
                intDesiredWidth = value; 
            }
        }

        private int intDesiredHeight = 0;
        /// <summary>
        /// desired height
        /// </summary>
        [System.ComponentModel.DefaultValue(0)]
        public int DesiredHeight
        {
            get
            {
                return intDesiredHeight; 
            }
            set
            {
                intDesiredHeight = value; 
            }
        }

        private int intWidth = 0;
        /// <summary>
        /// width
        /// </summary>
        [System.ComponentModel.DefaultValue(0)]
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

        private RTFPicType _PicType = RTFPicType.Jpegblip;
        /// <summary>
        /// Õº∆¨∏Ò Ω
        /// </summary>
        public RTFPicType PicType
        {
            get
            {
                return _PicType; 
            }
            set
            {
                _PicType = value; 
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

        public override string ToString()
        {
            string txt = "Image:" + intWidth + "*" + intHeight;
            if (bsData != null && bsData.Length > 0)
                txt = txt + " " + Convert.ToDouble( bsData.Length / 1024.0).ToString("0.00") + "KB";
            return txt;
        }
    }

    public enum RTFPicType
    {
        /// <summary>
        /// Source of the picture is an EMF (enhanced metafile).
        /// </summary>
        Emfblip ,
        /// <summary>
        /// Source of the picture is a PNG.
        /// </summary>
        Pngblip ,
        /// <summary>
        /// Source of the picture is a JPEG.
        /// </summary>
        Jpegblip ,
        /// <summary>
        /// ource of the picture is QuickDraw.
        /// </summary>
        Macpict ,
        /// <summary>
        /// Source of the picture is an OS/2 metafile. The N argument identifies the metafile type. The N values are described in the \pmmetafile table further on in this section.
        /// </summary>
        Pmmetafile ,
        /// <summary>
        /// Source of the picture is a Windows metafile. The N argument identifies the metafile type (the default type is 1).
        /// </summary>
        Wmetafile ,
        /// <summary>
        /// Source of the picture is a Windows device-independent bitmap. The N argument identifies the bitmap type, which must equal 0.
        /// The information to be included in RTF from a Windows device-independent bitmap is the concatenation of the BITMAPINFO structure followed by the actual pixel data.
        /// </summary>
        Dibitmap ,
        /// <summary>
        /// Source of the picture is a Windows device-dependent bitmap. The N argument identifies the bitmap type (must equal 0).
        /// The information to be included in RTF from a Windows device-dependent bitmap is the result of the GetBitmapBits function.
        /// </summary>
        Wbitmap
    }
}
