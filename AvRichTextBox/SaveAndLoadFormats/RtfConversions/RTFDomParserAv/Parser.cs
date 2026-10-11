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

namespace RtfDomParserAv
{
    /// <summary>
    /// RTF plain text container
    /// </summary>
    /// <remarks>
    /// initialize instance
    /// </remarks>
    /// <param name="doc">owner document</param>
    internal class RTFTextContainer(RTFDomDocument doc)
    {
        private readonly ByteBuffer myBuffer = new();

        private RTFDomDocument myDocument = doc;
        public RTFDomDocument Document
        {
            get => myDocument;
            set => myDocument = value;
        }

        private System.Text.StringBuilder myStr = new();
        /// <summary>
        /// Append text content
        /// </summary>
        /// <param name="text"></param>
        public void Append(string text)
        {
            if (string.IsNullOrEmpty(text) == false)
            {
                CheckBuffer();
                myStr.Append(text);
            }
        }

        /// <summary>
        /// Accept rtf token
        /// </summary>
        /// <param name="token">RTF token</param>
        /// <returns>Is accept it?</returns>
        public bool Accept(RTFToken token , RTFReader reader )
        {
            if (token == null)
                return false;

            if (token.Type == RTFTokenType.Text)
            {

                //Debug.WriteLine($"TYPE={token.Type}, KEY=[{token.Key}], " + $"PARAM={token.Param}, HASPARAM={token.HasParam}, " + $"UC={reader.CurrentLayerInfo.UCValue}, " + $"COUNT={reader.CurrentLayerInfo.UCValueCount}");

                if (reader != null)
                {
                    if ( token.Key[0] == '?' )
                    {
                        if (reader.LastToken != null)
                        {
                            if (reader.LastToken.Type == RTFTokenType.Keyword  && reader.LastToken.Key == "u" && reader.LastToken.HasParam)
                            {   // 紧跟在在“\uN”后面的问号忽略掉
                                if (token.Key.Length > 0)
                                {
                                    CheckBuffer();
                                    //myStr.Append(token.Key.Substring(1));
                                }
                                return true;
                            }
                        }
                    }
                    //else if (token.Key == "\"")
                    //{
                    //    // 双引号开头,一直读取内容到双引号结束
                    //    CheckBuffer();
                    //    while (true)
                    //    {
                    //        int v = reader.InnerReader.Read();
                    //        if (v > 0)
                    //        {
                    //            if (v == (int)'"')
                    //            {
                    //                break;
                    //            }
                    //            else
                    //            {
                    //                myStr.Append((char)v);
                    //            }
                    //        }
                    //        else
                    //        {
                    //            break;
                    //        }
                    //    }//while
                    //    return true;
                    //}
                }
                CheckBuffer();
                myStr.Append(token.Key);
                return true ;
            }
            else if (token.Type == RTFTokenType.Control && token.Key == "'" && token.HasParam )
            {
                if( reader.CurrentLayerInfo.CheckUCValueCount())
                    myBuffer.Add((byte)token.Param);

                return true;
            }
            
            if (token.Key == RTFConsts._u && token.HasParam)
            {
                // Unicode char
                CheckBuffer();

                // 不忽略 \u 指令
                myStr.Append( (char)token.Param);
                reader.CurrentLayerInfo.UCValueCount = reader.CurrentLayerInfo.UCValue;
                return true;
            }
            if (token.Key == "tab")
            {
                CheckBuffer();
                myStr.Append('\t');
                return true;
            }
            if (token.Key == "emdash")
            {
                CheckBuffer();
                myStr.Append('—');
                return true;
            }
            if (token.Key == "")
            {
                // 提示未识别的字符
                CheckBuffer();
                myStr.Append('-');
                return true;
            }
            return false;
        }

        /// <summary>
        /// this container has some text
        /// </summary>
        public bool HasContent
        {
            get
            {
                CheckBuffer();
                return myStr.Length > 0;
            }
        }

        /// <summary>
        /// text value
        /// </summary>
        public string Text
        {
            get
            {
                CheckBuffer();   
                return myStr.ToString();
            }
        }

        private int intLevel = 0;
        public int Level
        {
            get => intLevel;
            set => intLevel = value;
        }

        /// <summary>
        /// clear value
        /// </summary>
        public void Clear()
        {
            myBuffer.Clear();
            myStr = new System.Text.StringBuilder();
        }
        private void CheckBuffer()
        {
            if (myBuffer.Count > 0)
            {
                string txt = myBuffer.GetString(myDocument.RuntimeEncoding);
                myStr.Append(txt);
                myBuffer.Clear();
            }
        }
    }

	public class RTFReader : System.IDisposable
	{
		/// <summary>
		/// initialize instance
		/// </summary>
		public RTFReader( )
		{
		}

        public RTFReader(string fileName)
        {
            LoadRTFFile(fileName);
        }

        public RTFReader(System.IO.Stream stream)
        {
            System.IO.StreamReader reader = new(stream, System.Text.Encoding.ASCII);
            LoadReader(reader);
            myBaseStream = stream;
        }

        public RTFReader(System.IO.TextReader reader)
        {
            LoadReader(reader);
        }

		private RTFLex myLex = null!;
		//private RTFToken myToken = null ;

        private TextReader myReader = null!;
        public TextReader InnerReader => myReader;

        //private System.Collections.ArrayList myTokenStack = new System.Collections.ArrayList();
        //public System.Collections.ArrayList TokenStack
        //{
        //    get
        //    {
        //        return myTokenStack ;
        //    }
        //}

        private System.IO.Stream myBaseStream = null!;

		/// <summary>
		/// load rtf document
		/// </summary>
		/// <param name="strFileName">spcial file name</param>
		/// <returns>is operation successful</returns>
		public bool LoadRTFFile( string strFileName )
		{
			//myTokenStack.Clear();
			myCurrentToken = null! ;
			if( File.Exists( strFileName ))
			{
				FileStream stream = new( strFileName , System.IO.FileMode.Open , System.IO.FileAccess.Read );
				myReader = new System.IO.StreamReader( stream , System.Text.Encoding.ASCII );
                myBaseStream = stream;
				myLex = new RTFLex( myReader );
				return true ;
			}
			return false;
		}
		/// <summary>
		/// load rtf document
		/// </summary>
		/// <param name="reader">text reader</param>
		/// <returns>is operation successful</returns>
		public bool LoadReader( System.IO.TextReader reader )
		{
			//myTokenStack.Clear();
			myCurrentToken = null!;
			if( reader != null )
			{
				myReader = reader;
				myLex = new RTFLex( myReader );
				return true ;
			}
			return false;
		}

		/// <summary>
		/// load rtf text
		/// </summary>
		/// <param name="strText">RTF text</param>
		/// <returns>is operation successful</returns>
		public bool LoadRTFText( string strText )
		{
			//myTokenStack.Clear();
			myCurrentToken = null!;
			if( strText != null && strText.Length > 3 )
			{
				myReader = new StringReader( strText );
				myLex = new RTFLex( myReader );
				return true ;
			}
			return false ;
		}

		public void Close()
		{
			if( myReader != null )
			{
				myReader.Close();
				myReader = null! ;
			}
		}
		private RTFToken myCurrentToken = null!;
		public RTFToken CurrentToken => myCurrentToken;
		
		public RTFTokenType TokenType
		{
			get
			{
				if( myCurrentToken == null )
					return RTFTokenType.None ;
				else
					return myCurrentToken.Type ;
			}
		}

		public string Keyword
		{
			get
			{
				if( myCurrentToken == null )
					return null!;
				else
					return myCurrentToken.Key ;
			}
		}

		/// <summary>
		/// is current token has a parameter
		/// </summary>
		public bool HasParam
		{
			get
			{
				if( myCurrentToken == null )
					return false ;
				else
					return myCurrentToken.HasParam ;
			}
		}

		public int Parameter
		{
			get
			{
				if( myCurrentToken == null )
					return 0 ;
				else
					return myCurrentToken.Param ;
			}
		}

        public int ContentPosition
        {
            get
            {
                if (myBaseStream == null)
                    return 0;
                else
                    return ( int ) myBaseStream.Position;
            }
        }

        public int ContentLength
        {
            get
            {
                if (myBaseStream == null)
                    return 0;
                else
                    return ( int ) myBaseStream.Length;
            }
        }

        /// <summary>
        /// next token type
        /// </summary>
        /// <returns></returns>
        public RTFTokenType PeekTokenType() => myLex.PeekTokenType();

        //private RTFToken myLastToken = null;
        //public RTFToken LastToken
        //{
        //    get
        //    {
        //        return myLastToken;
        //    }
        //}

        private bool bolFirstTokenInGroup = false;
        /// <summary>
        /// Current token is the first token in owner group
        /// </summary>
        public bool FirstTokenInGroup => bolFirstTokenInGroup; 
        

        private RTFToken myLastToken = null!;
        public RTFToken LastToken => myLastToken; 

        private int intLevel = 0;
        public int Level => intLevel;

        private int intTokenCount = 0;
        /// <summary>
        /// total of this object handle tokens
        /// </summary>
        public int TokenCount
        {
            get => intTokenCount; 
            set => intTokenCount = value;
        }

        private bool _EnableDefaultProcess = true;

        public bool EnableDefaultProcess
        {
            get => _EnableDefaultProcess;
            set => _EnableDefaultProcess = value;
        }

        public void DefaultProcess()
        {
            if (this.CurrentToken != null)
            {
                switch (this.CurrentToken.Key)
                {
                    case "uc":
                        this.CurrentLayerInfo.UCValue = this.Parameter;
                        break;
                }
            }
        }
        private Stack<RTFRawLayerInfo> _LayerStack = new();
        public RTFRawLayerInfo CurrentLayerInfo
        {
            get
            {
                if (_LayerStack.Count == 0)
                    _LayerStack.Push(new RTFRawLayerInfo());

                return _LayerStack.Peek();
            }
        }
		/// <summary>
		/// read token
		/// </summary>
		/// <returns>token readed</returns>
		public RTFToken ReadToken()
		{
            bolFirstTokenInGroup = false;
            myLastToken = myCurrentToken;

            if (myLastToken != null && myLastToken.Type == RTFTokenType.GroupStart)
                bolFirstTokenInGroup = true;

			myCurrentToken = myLex.NextToken( );
			if( myCurrentToken == null || myCurrentToken.Type == RTFTokenType.Eof )
			{
				myCurrentToken = null!;
				return null!;
			}
            intTokenCount++;
            if (myCurrentToken.Type == RTFTokenType.GroupStart)
            {
                if (_LayerStack.Count == 0)
                    _LayerStack.Push(new RTFRawLayerInfo());
                else
                {
                    RTFRawLayerInfo info = _LayerStack.Peek();
                    _LayerStack.Push(info.Clone());
                }
                intLevel++;
            }
            else if (myCurrentToken.Type == RTFTokenType.GroupEnd)
            {
                if (_LayerStack.Count > 0)
                    _LayerStack.Pop();

                intLevel--;
            }
       
            if (this.EnableDefaultProcess)
                this.DefaultProcess();

            //if (myTokenStack.Count > 0)
            //{
            //    myCurrentToken.Parent = (RTFToken)myTokenStack[myTokenStack.Count - 1];
            //}
            //myTokenStack.Add( myCurrentToken );

			return myCurrentToken ;
		}

        /// <summary>
        /// read and ignore data , until just the end of current group,preserve the end.
        /// </summary>
        public void ReadToEndGround( )
        {
            int level = 0;
            while (true)
            {
                int c = myReader.Peek();
                if (c == -1)
                    break;

                else if (c == '{')
                    level++;

                else if (c == '}')
                {
                    level--;
                    if (level < 0)
                        break;
                }
                c = myReader.Read();
            }
        }

		public void Dispose()
		{
			this.Close();
		}

        public override string ToString() => $"RTFReader Level: {intLevel} {this.Keyword}" ;
	}

    /// <summary>
    /// Initialize instance
    /// </summary>
    /// <param name="reader">reader</param>
    public class RTFLex(System.IO.TextReader reader)
    {
        private TextReader myReader = reader;
		private const int EOF = -1 ;

        public RTFTokenType PeekTokenType()
        {
            int c = myReader.Peek();

            while (c is '\r' or '\n' or '\t' or '\0')
            {
                c = myReader.Read();
                c = myReader.Peek();
            }

            if (c == EOF)
                return RTFTokenType.Eof;
            else
            {
                return c switch
                {
                    '{' => RTFTokenType.GroupStart,
                    '}' => RTFTokenType.GroupEnd,
                    '\\' => RTFTokenType.Control,
                    _ => RTFTokenType.Text,
                };
            }
        }


		public RTFToken NextToken( )
		{
			int c = 0 ;
			RTFToken token = new();

			c = myReader.Read();

            if (c == '\"')
            {
                // 以双引号开头，读取连续的字符
                System.Text.StringBuilder str = new();
                while (true)
                {
                    c = myReader.Read();
                    if (c < 0)
                        break;

                    if (c == '\"')
                        break;
                    else
                        str.Append((char)c);

                }//while
                token.Type = RTFTokenType.Text;
                token.Key = str.ToString();
                return token;
            }

			while(c is '\r' or '\n' or '\t' or '\0')
			{
				c = myReader.Read();
				//c = myReader.Peek();
			}

			//
//			c = myReader.Read();
//			while( c == '\r'
//				|| c == '\n' 
//				|| c == '\t'
//				|| c == '\0')
//			{
//				c = myReader.Read();
//			}
			if( c != EOF )
			{
				switch( c )
				{
					case '{' :
						token.Type = RTFTokenType.GroupStart ;
						break;
					
                    case '}' :
						token.Type = RTFTokenType.GroupEnd ;
						break;
					
                    case '\\' :

                        int p = myReader.Peek();
                        ParseKeyword(token);
                        //Debug.WriteLine($"AFTER ParseKeyword: TYPE={token.Type}, " + $"KEY=[{token.Key}], PARAM={token.Param}, " + $"HASPARAM={token.HasParam}");
                        break;
					
                    default:
						token.Type = RTFTokenType.Text ;
                        ParseText(c, token);
						break;
				}
			}
			else
				token.Type = RTFTokenType.Eof ;

			return token;
		}

		private void ParseKeyword( RTFToken token )
		{
			int c = 0 ;
			bool ext = false;
			c = myReader.Peek();
			if( char.IsLetter( ( char ) c ) == false )
			{
				myReader.Read();
				if( c == '*' )
				{
					// expend keyword
					token.Type = RTFTokenType.Keyword ;
					myReader.Read();
					//myReader.Read();
					ext = true ;
					goto ReadKeywrod ;
				}
				else if( c is '\\' or '{' or '}')
				{
					// special character
					token.Type = RTFTokenType.Text ;
					token.Key = ( ( char ) c).ToString();
				}
				else
				{
					token.Type = RTFTokenType.Control ;
					token.Key = ( ( char ) c ).ToString();
					if( token.Key == "\'" )
					{
						// read 2 hex characters
						System.Text.StringBuilder text = new();
						text.Append( ( char ) myReader.Read());
						text.Append( ( char ) myReader.Read());
						token.HasParam = true ;
						token.Param = Convert.ToInt32( text.ToString().ToLower() , 16 );

						if( token.Param == 0 )
							token.Param = 0 ;
					}
				}
				return ;
			}//if( char.IsLetter( ( char ) c ) == false )

		ReadKeywrod :

			// read keyword
			System.Text.StringBuilder Keyword = new();
			c = myReader.Peek();
			while( char.IsLetter ( ( char ) c ))
			{
				myReader.Read();
				Keyword .Append( ( char ) c );
				c = myReader.Peek();
			}

			if( ext )
				token.Type = RTFTokenType.ExtKeyword ;
			else
				token.Type = RTFTokenType.Keyword ;
			token.Key = Keyword.ToString();

			// read a interger
			if( char.IsDigit( ( char ) c ) || c == '-' )
			{
				token.HasParam = true ;
				bool Negative = false;
				if( c == '-' )
				{
					Negative = true ;
					myReader.Read();
				}
				c = myReader.Peek();
				System.Text.StringBuilder text = new();
				while( char.IsDigit( ( char ) c ))
				{
					myReader.Read();
					text.Append( ( char ) c );
					c = myReader.Peek();
				}
				int p = Convert.ToInt32( text.ToString());
				if( Negative )
					p = - p ;
				token.Param = p ;
			}//if( char.IsDigit( ( char ) c ) || c == '-' )

			if( c == ' ' )
			{
				myReader.Read();
			}
		}

		private void ParseText( int c , RTFToken token )
		{
			System.Text.StringBuilder myStr = new( ( ( char ) c ).ToString());

			c = ClearWhiteSpace();

			while( c is not '\\' and not '}' and not '{' and not EOF)
			{
				myReader.Read();
				myStr.Append( ( char ) c );
				c = ClearWhiteSpace();
			}

			token.Key = myStr.ToString();
		}
		
		private int ClearWhiteSpace( )
		{
			int c = myReader.Peek();
			while( c is '\r' or '\n' or '\t' or '\0')
			{
				myReader.Read();
				c = myReader.Peek();
			}
			return c ;
		}
	}
	/// <summary>
	/// rtf token type
	/// </summary>
	public class RTFToken
	{
		private RTFTokenType intType = RTFTokenType.None ;
		/// <summary>
		/// type
		/// </summary>
		public RTFTokenType Type
		{
			get{ return intType ;}
			set{ intType = value;}
		}

		private string strKey = null!;
		/// <summary>
		/// keyword
		/// </summary>
		public string Key
		{
			get{ return strKey ;}
			set{ strKey = value;}
		}

		private bool bolHasParam = false;
		/// <summary>
		/// 
		/// </summary>
		public bool HasParam
		{
			get{ return bolHasParam ;}
			set{ bolHasParam = value;}
		}

		private int intParam = 0 ;
		public int Param
		{
			get{ return intParam ;}
			set{ intParam = value;}
		}

        private RTFToken myParent = null!;
        /// <summary>
        /// parent token
        /// </summary>
        public RTFToken Parent
        {
            get { return myParent; }
            set { myParent = value; }
        }
        public bool IsTextToken
        {
            get
            {
                if (intType == RTFTokenType.Text)
                    return true;
                if (intType == RTFTokenType.Control && strKey == "'" && bolHasParam)
                    return true;
                return false;
            }
        }

        public override string ToString()
        {
            if (intType == RTFTokenType.Keyword)
                return this.Key + this.Param;
            else if (intType == RTFTokenType.GroupStart)
                return "{";
            else if (intType == RTFTokenType.GroupEnd)
                return "}";
            else if (intType == RTFTokenType.Text)
                return "Text:" + this.Param;

            return intType.ToString() + ":" + this.Key + " " + this.Param;
        }
	}

	/// <summary>
	/// rtf token type
	/// </summary>
	public enum RTFTokenType
	{
		None ,
		Keyword ,
		ExtKeyword ,
		Control ,
		Text ,
		Eof ,
		GroupStart ,
		GroupEnd 
	}
	
}