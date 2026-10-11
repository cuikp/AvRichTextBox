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
using System.Runtime.InteropServices ;
using System.IO ;
using System.Text ;

namespace RtfDomParserAv
{
	/// <summary>
    /// some utility functions , this source code evolution from other software.
	/// </summary>
	internal class RTFUtil
	{


        /// <summary>
        /// 判断对象是否有实际内容
        /// </summary>
        /// <param name="rootElement">根元素对蟻E/param>
        /// <returns>是否有实际内容</returns>
        public static bool HasContentElement(RTFDomElement rootElement)
        {
            if (rootElement.Elements.Count == 0)
            {
                return false;
            }
            if (rootElement.Elements.Count == 1)
            {
                if (rootElement.Elements[0] is RTFDomParagraph)
                {
                    RTFDomParagraph p = (RTFDomParagraph)rootElement.Elements[0];
                    if (p.Elements.Count == 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

		/// <summary>
		/// Use the EmfToWmfBits function in the GDI+ specification to convert a 
		/// Enhanced Metafile to a Windows Metafile
		/// </summary>
		/// <param name="_hEmf">
		/// A handle to the Enhanced Metafile to be converted
		/// </param>
		/// <param name="_bufferSize">
		/// The size of the buffer used to store the Windows Metafile bits returned
		/// </param>
		/// <param name="_buffer">
		/// An array of bytes used to hold the Windows Metafile bits returned
		/// </param>
		/// <param name="_mappingMode">
		/// The mapping mode of the image.  This control uses MM_ANISOTROPIC.
		/// </param>
		/// <param name="_flags">
		/// Flags used to specify the format of the Windows Metafile returned
		/// </param>
		[DllImportAttribute("gdiplus.dll")]
		private static extern uint GdipEmfToWmfBits (IntPtr _hEmf, uint _bufferSize,
			byte[] _buffer, int _mappingMode, EmfToWmfBitsFlags _flags);

		// Specifies the flags/options for the unmanaged call to the GDI+ method
		// Metafile.EmfToWmfBits().
		private enum EmfToWmfBitsFlags 
		{

			// Use the default conversion
			EmfToWmfBitsFlagsDefault = 0x00000000,

			// Embedded the source of the EMF metafiel within the resulting WMF
			// metafile
			EmfToWmfBitsFlagsEmbedEmf = 0x00000001,

			// Place a 22-byte header in the resulting WMF file.  The header is
			// required for the metafile to be considered placeable.
			EmfToWmfBitsFlagsIncludePlaceable = 0x00000002,

			// Don't simulate clipping by using the XOR operator.
			EmfToWmfBitsFlagsNoXORClip = 0x00000004
		};



		/// <summary>
		/// Wraps the image in an Enhanced Metafile by drawing the image onto the
		/// graphics context, then converts the Enhanced Metafile to a Windows
		/// Metafile, and finally appends the bits of the Windows Metafile in HEX
		/// to a string and returns the string.
		/// </summary>
		/// <param name="_image"></param>
		/// <returns>
		/// A string containing the bits of a Windows Metafile in HEX
		/// </returns>
		private string GetRtfImage(Avalonia.Controls.Image _image) 
		{

         //// Ensures that the metafile maintains a 1:1 aspect ratio
         ////const int MM_ISOTROPIC = 7;

         //// Allows the x-coordinates and y-coordinates of the metafile to be adjusted
         //// independently
         //const int MM_ANISOTROPIC = 8;



         //StringBuilder _rtf = null;

         //// Used to store the enhanced metafile
         //MemoryStream _stream = null;

         //// Used to create the metafile and draw the image
         //Graphics _graphics = null;

         //// The enhanced metafile
         //Metafile _metaFile = null;

         //// Handle to the device context used to create the metafile
         //IntPtr _hdc;

         //try 
         //{
         //	_rtf = new StringBuilder();
         //	_stream = new MemoryStream();

         //	// Get a graphics context from the RichTextBox
         //	using(_graphics = System.Drawing.Graphics.FromHwnd( new IntPtr( 0 ) ))
         //	{

         //		// Get the device context from the graphics context
         //		_hdc = _graphics.GetHdc();

         //		// Create a new Enhanced Metafile from the device context
         //		_metaFile = new Metafile(_stream, _hdc);

         //		// Release the device context
         //		_graphics.ReleaseHdc(_hdc);
         //	}

         //	// Get a graphics context from the Enhanced Metafile
         //	using(_graphics = Graphics.FromImage(_metaFile)) 
         //	{

         //		// Draw the image on the Enhanced Metafile
         //		_graphics.DrawImage(_image, new Rectangle(0, 0, _image.Width, _image.Height));

         //	}

         //	// Get the handle of the Enhanced Metafile
         //	IntPtr _hEmf = _metaFile.GetHenhmetafile();

         //	// A call to EmfToWmfBits with a null buffer return the size of the
         //	// buffer need to store the WMF bits.  Use this to get the buffer
         //	// size.
         //	uint _bufferSize = GdipEmfToWmfBits(_hEmf, 0, null, MM_ANISOTROPIC,
         //		EmfToWmfBitsFlags.EmfToWmfBitsFlagsDefault);

         //	// Create an array to hold the bits
         //	byte[] _buffer = new byte[_bufferSize];

         //	// A call to EmfToWmfBits with a valid buffer copies the bits into the
         //	// buffer an returns the number of bits in the WMF.  
         //	uint _convertedSize = GdipEmfToWmfBits(_hEmf, _bufferSize, _buffer, MM_ANISOTROPIC,
         //		EmfToWmfBitsFlags.EmfToWmfBitsFlagsDefault);

         //	// Append the bits to the RTF string
         //	for(int i = 0; i < _buffer.Length; ++i) 
         //	{
         //		_rtf.Append(String.Format("{0:X2}", _buffer[i]));
         //	}

         //	return _rtf.ToString();
         //}
         //finally 
         //{
         //	if(_graphics != null)
         //		_graphics.Dispose();
         //	if(_metaFile != null)
         //		_metaFile.Dispose();
         //	if(_stream != null)
         //		_stream.Close();
         //}
         return "";
      }
  

		private RTFUtil()
		{
			 
		}
	}
}
