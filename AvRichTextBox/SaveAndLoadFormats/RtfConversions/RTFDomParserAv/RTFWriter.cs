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
    /// RTF text writer ,  this source code evolution from other software.
	/// </summary>
	public class RTFWriter : System.IDisposable
	{
        static RTFWriter() => Defaults.LoadEncodings();

        /// <summary>
		/// Initialize instance
		/// </summary>
		/// <param name="w">text writer</param>
		public RTFWriter( System.IO.TextWriter w )
		{
			myWriter = w ;
		}

		/// <summary>
		/// Initialize instance
		/// </summary>
		/// <param name="strFileName">file name</param>
		public RTFWriter( string strFileName )
		{
			myWriter = new System.IO.StreamWriter(
				strFileName , 
				false , 
				System.Text.Encoding.ASCII );
		}

        /// <summary>
        /// for chinese , can use System.Text.Encoding.GetEncoding( 936 );
        /// </summary>
        private System.Text.Encoding myEncoding = System.Text.Encoding.Default ;
		/// <summary>
		/// text encoding
		/// </summary>
		public System.Text.Encoding Encoding
		{
			get{ return myEncoding ;}
			set{ myEncoding = value;}
		}

		/// <summary>
		/// inner text writer
		/// </summary>
		private System.IO.TextWriter myWriter = null;

		private bool bolIndent = false;
		/// <summary>
		/// whether output rtf code with indent style
		/// </summary>
		/// <remarks>
        /// In rtf formation , recommend do not use indent . this option just to debugger , 
        /// in software development , use this option can genereate indented rtf code friendly to read,
        /// but after debug , recommend clear this option and set this attribute = false.
        /// </remarks>
		public bool Indent
		{
			get
            {
                return bolIndent ;
            }
			set
            {
                bolIndent = value;
            }
		}

		private string strIndentString = "   ";
		/// <summary>
		/// string used to indent
		/// </summary>
		public string IndentString
		{
			get
            {
                return strIndentString ;
            }
			set
            {
                strIndentString = value;
            }
		}

		/// <summary>
		/// current group level
		/// </summary>
		private int intGroupLevel = 0 ;
        /// <summary>
        /// 当前组合等级
        /// </summary>
        public int GroupLevel
        {
            get
            {
                return intGroupLevel;
            }
        }

		/// <summary>
		/// close 
		/// </summary>
		public void Close()
		{
			if(this.intGroupLevel > 0 )
				throw new System.Exception("Some group does not finish");
			if( myWriter != null )
			{
				myWriter.Close();
				myWriter = null;
			}
		}

        public void Flush()
        {
            if (myWriter != null)
            {
                myWriter.Flush();
            }
        }

		/// <summary>
		/// write completed group wich one keyword
		/// </summary>
		/// <param name="KeyWord">keyword</param>
		public void WriteGroup( string KeyWord )
		{
			this.WriteStartGroup();
			this.WriteKeyword( KeyWord );
			this.WriteEndGroup();
		}

		/// <summary>
		/// begin write group
		/// </summary>
		public void WriteStartGroup( )
		{
            if (bolIndent)
            {
                InnerWriteNewLine();
                myWriter.Write("{");
            }
            else
            {
                myWriter.Write("{");
            }
			intGroupLevel ++ ;
		}

		/// <summary>
		/// end write group
		/// </summary>
        public void WriteEndGroup()
        {
            intGroupLevel--;
            if (intGroupLevel < 0)
            {
                throw new System.Exception("group level error");
            }
            if (bolIndent)
            {
                InnerWriteNewLine();
                InnerWrite("}");
            }
            else
            {
                InnerWrite("}");
            }
        }

		/// <summary>
		/// write raw text
		/// </summary>
		/// <param name="txt">text</param>
		public void WriteRaw( string txt )
		{
			if( txt != null && txt.Length > 0 )
			{
				InnerWrite( txt );
			}
		}
		/// <summary>
		/// write keyword
		/// </summary>
		/// <param name="Keyword">keyword</param>
		public void WriteKeyword( string Keyword )
		{
			WriteKeyword( Keyword , false );
		}
		/// <summary>
		/// write keyword
		/// </summary>
		/// <param name="Keyword">keyword</param>
		/// <param name="Ext">whether extern key word</param>
		public void WriteKeyword( string Keyword , bool Ext)
		{
			if( Keyword == null || Keyword.Length == 0)
				throw new System.ArgumentNullException("值不得为空");
			if( bolIndent == false && ( Keyword == "par" || Keyword == "pard" ) )
			{
                // at the front of par or pard can write new line , will not effect rtf render.
				InnerWrite( System.Environment.NewLine );
			}
			if( this.bolIndent )
			{
				if( Keyword == "par" || Keyword == "pard" )
				{
					this.InnerWriteNewLine();
				}
			}
			if( Ext )
				InnerWrite("\\*\\");
			else
				InnerWrite("\\");
			InnerWrite( Keyword );
		}

//		public void WriteRaw( string raw )
//		{
//			InnerWrite( raw );
//		}

		/// <summary>
		/// text encoding
		/// </summary>
		private System.Text.Encoding Unicode = System.Text.Encoding.Unicode ;
		/// <summary>
		/// write plain text
		/// </summary>
		/// <param name="Text">文本值</param>
		public void WriteText( string Text )
		{
			if( Text == null || Text.Length == 0 )
				return ;

			WriteText( Text , true );
		}

        public void WriteUnicodeText(string text)
        {
            if (string.IsNullOrEmpty(text) == false)
            {
                WriteKeyword("uc1");
                foreach (char c in text)
                {
                    if (c > 127)
                    {
                        int v = (int)c;
                        short v2 = (short)v;
                        WriteKeyword("u" + v2.ToString());
                        WriteRaw(" ?");
                    }
                    else
                    {
                        InnerWriteChar(c);
                    }
                }
            }
        }

		/// <summary>
		/// write plain text, can choose write a white space automatic
		/// </summary>
		/// <param name="Text">text</param>
		/// <param name="AutoAddWhitespace">wirte a white space automatic</param>
		public void WriteText( string Text , bool AutoAddWhitespace )
		{
			if( Text == null || Text.Length == 0 )
				return ;
			
			if( AutoAddWhitespace )
			{
				InnerWrite( ' ' );
			}

			for( int iCount = 0 ; iCount < Text.Length ; iCount ++ )
			{
				char c = Text[ iCount ] ;
                InnerWriteChar(c);

                //if( c == '\t')
                //{
                //    this.WriteKeyword("tab");
                //    InnerWrite(' ');
                //}
                //if( c > 32 && c < 127 )
                //{
                //    // some specify characters , must be convert
                //    if( c == '\\' || c == '{' || c == '}' )
                //        InnerWrite( '\\');
                //    InnerWrite( c );
                //}
                //else
                //{
                //    byte[] bs = myEncoding.GetBytes( c.ToString());
                //    for(int iCount2 = 0 ; iCount2 < bs.Length ; iCount2 ++ )
                //    {
                //        InnerWrite("\\\'");
                //        WriteByte( bs[ iCount2 ] );
                //    }
                //}
			}//for( int iCount = 0 ; iCount < Text.Length ; iCount ++ )
		}

        private void InnerWriteChar(char c)
        {
            if (c == '\t')
            {
                this.WriteKeyword("tab");
                InnerWrite(' ');
            }
            if (c > 32 && c < 127)
            {
                // some specify characters , must be convert
                if (c == '\\' || c == '{' || c == '}')
                {
                    InnerWrite('\\');
                }
                InnerWrite(c);
            }
            else
            {
                byte[] bs = myEncoding.GetBytes(c.ToString());
                for (int iCount2 = 0; iCount2 < bs.Length; iCount2++)
                {
                    InnerWrite("\\\'");
                    WriteByte(bs[iCount2]);
                }
            }
        }

		/// <summary>
		/// current position
		/// </summary>
		private int intPosition = 0 ;
		/// <summary>
		/// current line head
		/// </summary>
		private int intLineHead = 0 ;

		/// <summary>
		/// hex characters
		/// </summary>
		private const string Hexs = "0123456789abcdef";

		/// <summary>
		/// write binary data
		/// </summary>
		/// <param name="bs">binary data</param>
		public void WriteBytes( byte[] bs )
		{
			if( bs == null || bs.Length == 0 )
				return ;
			WriteRaw( " " );
			for( int iCount = 0 ; iCount < bs.Length ; iCount ++ )
			{
				if( ( iCount % 32 ) == 0 )
				{
					this.WriteRaw( System.Environment.NewLine );
					this.WriteIndent();
				}
				else if( ( iCount % 8 ) == 0 )
				{
					this.WriteRaw(" ");
				}
				byte b = bs[ iCount ] ;
				int h = ( b & 0xf0 ) >> 4  ;
				int l = b & 0xf ;
				myWriter.Write( Hexs[ h ] );
				myWriter.Write( Hexs[ l ] );
				intPosition += 2 ;
			}
		}

		/// <summary>
		/// write a byte data
		/// </summary>
		/// <param name="b">byte data</param>
		public void WriteByte( byte b )
		{
			int h = ( b & 0xf0 ) >> 4 ;
			int l = b & 0xf ;
			myWriter.Write( Hexs[ h ] );
			myWriter.Write( Hexs[ l ] );
			intPosition += 2 ;
			//FixIndent();
		}

		#region internal function ******************************************************

		private void InnerWrite( char c )
		{
			intPosition ++ ;
			myWriter.Write( c );
		}
		private void InnerWrite( string txt )
		{
			intPosition += txt.Length ;
			myWriter.Write( txt );
		}

		private void FixIndent()
		{
			if( this.bolIndent )
			{
				if( intPosition - intLineHead > 100 )
					InnerWriteNewLine();
			}
		}

		private void InnerWriteNewLine()
		{
			if( this.bolIndent )
			{
				if( intPosition > 0 )
				{
					InnerWrite( System.Environment.NewLine );
					intLineHead = intPosition ;
					WriteIndent();
				}
			}
		}

		private void WriteIndent( )
		{
			if( bolIndent )
			{
				for( int iCount = 0 ; iCount < intGroupLevel ; iCount ++ )
				{
					InnerWrite( this.strIndentString );
				}
			}
		}

		#endregion 

		/// <summary>
		/// dispose instance
		/// </summary>
		public void Dispose()
		{
			this.Close();
		}
	}
}