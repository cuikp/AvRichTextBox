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
using Avalonia.Media;

namespace RtfDomParserAv
{
	/// <summary>
    /// RTF raw document,this source code evolution from other software.
	/// </summary>
	public class RTFRawDocument : RTFNodeGroup
	{ 

		/// <summary>
		/// test 
		/// </summary>
		internal static void Test()
		{
			RTFRawDocument doc = new RTFRawDocument();
			doc.Load(@"d:\abc.rtf");
			//System.Console.WriteLine( doc.Text );
			RTFWriter writer = new RTFWriter(@"d:\a.rtf");
			writer.Indent = true ;
			doc.Write( writer );
			writer.Close();

		}
		
		/// <summary>
		/// initialize instance
		/// </summary>
		public RTFRawDocument()
		{
			myOwnerDocument = this ;
			myParent = null ;
            myColorTable.CheckValueExistWhenAdd = false;
		}

		/// <summary>
		/// this owner document is myself
		/// </summary>
		public override RTFRawDocument OwnerDocument
		{
			get
			{
				return this ;
			}
			set
			{
				
			}
		}
		/// <summary>
		/// no parent node
		/// </summary>
		public override RTFNodeGroup Parent
		{
			get
			{
				return null;
			}
			set
			{
				
			}
		}

		/// <summary>
		/// color table
		/// </summary>
		protected RTFColorTable myColorTable = new RTFColorTable();
		/// <summary>
		/// color table
		/// </summary>
		public RTFColorTable ColorTable
		{
			get{ return myColorTable ;}
		}

		/// <summary>
		/// font table
		/// </summary>
		protected RTFFontTable myFontTable = new RTFFontTable();
		/// <summary>
		/// font table
		/// </summary>
		public RTFFontTable FontTable
		{
			get{ return myFontTable ;}
		}

		/// <summary>
		/// document information
		/// </summary>
		protected RTFDocumentInfo myInfo = new RTFDocumentInfo();
		/// <summary>
		/// document information
		/// </summary>
		public RTFDocumentInfo Info
		{
			get{ return myInfo ;}
		}

        /// <summary>
        /// read font table
        /// </summary>
        /// <param name="group"></param>
        private void ReadFontTable(RTFNodeGroup group)
        {
            myFontTable.Clear();
            foreach (RTFNode node in group.Nodes)
            {
                if (node is RTFNodeGroup)
                {
                    int index = -1;
                    string name = null;
                    int charset = 0;
                    foreach (RTFNode item in node.Nodes)
                    {
                        if (item.Keyword == "f" && item.HasParameter)
                        {
                            index = item.Parameter;
                        }
                        else if (item.Keyword == RTFConsts._fcharset)
                        {
                            charset = item.Parameter;
                        }
                        else if (item.Type == RTFNodeType.Text)
                        {
                            if (item.Keyword != null && item.Keyword.Length > 0)
                            {
                                name = item.Keyword;
                                break;
                            }
                        }
                    }
                    if (index >= 0 && name != null)
                    {
                        if (name.EndsWith(";"))
                            name = name.Substring(0, name.Length - 1);
                        name = name.Trim();
                        //System.Console.WriteLine( "Index:" + index + "  Name:" + name );
                        RTFFont font = new RTFFont(index, name);
                        font.Charset = charset;
                        myFontTable.Add(font);
                    }
                }
            }
        }

        /// <summary>
        /// read color table
        /// </summary>
        /// <param name="group"></param>
        private void ReadColorTable( RTFNodeGroup group )
        {
            myColorTable.Clear();
            int r = -1;
            int g = -1;
            int b = -1;
            foreach (RTFNode node in group.Nodes)
            {
                if (node.Keyword == "red")
                {
                    r = node.Parameter;
                }
                else if (node.Keyword == "green")
                {
                    g = node.Parameter;
                }
                else if (node.Keyword == "blue")
                {
                    b = node.Parameter;
                }
                if (node.Keyword == ";")
                {
                    if (r >= 0 && g >= 0 && b >= 0)
                    {
                        Color c = Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
                        myColorTable.Add(c);
                        r = -1;
                        g = -1;
                        b = -1;
                    }
                }
            }
            if (r >= 0 && g >= 0 && b >= 0)
            {
                // read the last color
                Color c = Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
                myColorTable.Add(c);
            }
        }

        /// <summary>
        /// read document information
        /// </summary>
        /// <param name="group"></param>
        private void ReadDocumentInfo(RTFNodeGroup group)
        {
            myInfo.Clear();
            RTFNodeList list = group.GetAllNodes(false);
            foreach (RTFNode node in group.Nodes)
            {
                if ((node is RTFNodeGroup) == false)
                {
                    continue;
                }
                if (node.Keyword == "creatim")
                {
                    myInfo.Creatim = ReadDateTime(node);
                }
                else if (node.Keyword == "revtim")
                {
                    myInfo.Revtim = ReadDateTime(node);
                }
                else if (node.Keyword == "printim")
                {
                    myInfo.Printim = ReadDateTime(node);
                }
                else if (node.Keyword == "buptim")
                {
                    myInfo.Buptim = ReadDateTime(node);
                }
                else
                {
                    if (node.HasParameter)
                        myInfo.SetInfo(node.Keyword, node.Parameter.ToString());
                    else
                    {
                        myInfo.SetInfo(node.Keyword, node.Nodes.Text);
                    }
                }
            }
        }
         
		private DateTime ReadDateTime( RTFNode g )
		{
			int yr = g.Nodes.GetParameter( "yr" , 1900 );
			int mo = g.Nodes.GetParameter( "mo" , 1 );
			int dy = g.Nodes.GetParameter( "dy" , 1 );
			int hr = g.Nodes.GetParameter( "hr" , 0 );
			int min = g.Nodes.GetParameter( "min" , 0 );
			int sec = g.Nodes.GetParameter( "sec" , 0 );
			return new DateTime( yr , mo , dy , hr , min ,sec );
		}
 
		/// <summary>
		/// load rtf text
		/// </summary>
		/// <param name="strText">text in rtf format</param>
		public void LoadRTFText( string strText )
		{
			myEncoding = null ;
			using( RTFReader reader = new RTFReader())
			{
				if( reader.LoadRTFText( strText ))
				{
					Load( reader );
					reader.Close();
				}
				reader.Close();
			}
		}

		/// <summary>
		/// load rtf file
		/// </summary>
		/// <param name="strFileName">file name</param>
		public void Load( string strFileName )
		{
			myEncoding = null ;
			using( RTFReader reader = new RTFReader() )
			{
				if( reader.LoadRTFFile( strFileName ) )
				{
					Load( reader );
					reader.Close();
				}
				reader.Close();
			}
		}

		private System.Text.Encoding myEncoding = null;
		/// <summary>
		/// text encoding
		/// </summary>
		public System.Text.Encoding Encoding
		{
			get
			{
				if( myEncoding == null )
				{
					RTFNode node = myNodes[ RTFConsts._ansicpg ];
					if( node != null && node.HasParameter )
					{
						myEncoding = System.Text.Encoding.GetEncoding( node.Parameter );
					}
				}
				if( myEncoding == null )
					myEncoding = System.Text.Encoding.Default ;
				return myEncoding ;
			}
		}

        /// <summary>
        /// text encoding for current font
        /// </summary>
        private System.Text.Encoding myFontChartset = null;
        /// <summary>
        /// text encoding for current associate font
        /// </summary>
        private System.Text.Encoding myAssociateFontChartset = null;
        /// <summary>
        /// current text encoding
        /// </summary>
        internal System.Text.Encoding RuntimeEncoding
        {
            get
            {
                if (myFontChartset != null)
                {
                    return myFontChartset;
                }
                if (myAssociateFontChartset != null)
                {
                    return myAssociateFontChartset;
                }
                return this.Encoding;
            }
        }

		public void Load( System.IO.TextReader reader )
		{
			RTFReader myReader = new RTFReader();
			myReader.LoadReader( reader );
			Load( myReader );
		}
		/// <summary>
		/// load rtf
		/// </summary>
		/// <param name="reader">RTF text reader</param>
		public void Load( RTFReader reader )
		{
			myNodes.Clear();
			System.Collections.Stack groups = new System.Collections.Stack();
			RTFNodeGroup NewGroup = null ;
			RTFNode NewNode = null;
			while( reader.ReadToken() != null )
			{
				if( reader.TokenType == RTFTokenType.GroupStart )
				{
					// begin group
					if( NewGroup == null)
					{
						NewGroup = this ;
					}
					else
					{
						NewGroup = new RTFNodeGroup();
						NewGroup.OwnerDocument = this ;
					}
					if( NewGroup != this )
					{
						RTFNodeGroup g = ( RTFNodeGroup ) groups.Peek();
						g.AppendChild( NewGroup );
					}
					groups.Push( NewGroup );
				}
				else if( reader.TokenType == RTFTokenType.GroupEnd )
				{
					// end group
					NewGroup = ( RTFNodeGroup ) groups.Pop();
					NewGroup.MergeText();
                    if (NewGroup.FirstNode is RTFNode)
                    {
                        switch (NewGroup.Keyword)
                        {
                            case RTFConsts._fonttbl:
                                // read font table
                                ReadFontTable(NewGroup);
                                break;
                            case RTFConsts._colortbl:
                                // read color table
                                ReadColorTable(NewGroup);
                                break;
                            case RTFConsts._info :
                                // read document information
                                ReadDocumentInfo(NewGroup);
                                break;
                        }
                    }
                    if (groups.Count > 0)
                    {
                        NewGroup = (RTFNodeGroup)groups.Peek();
                    }
                    else
                    {
                        break;
                    }
					//NewGroup.MergeText();
				}
				else
				{
					// read content
					 
					NewNode = new RTFNode( reader.CurrentToken );
					NewNode.OwnerDocument = this ;
					NewGroup.AppendChild( NewNode );
                    if (NewNode.Keyword == RTFConsts._f )
                    {
                        RTFFont font = this.FontTable[NewNode.Parameter];
                        if (font != null)
                        {
                            myFontChartset = font.Encoding;
                        }
                        else
                        {
                            myFontChartset = null;
                        }
                        //myFontChartset = RTFFont.GetRTFEncoding( NewNode.Parameter );
                    }
                    else if (NewNode.Keyword == RTFConsts._af)
                    {
                        RTFFont font = this.FontTable[NewNode.Parameter];
                        if (font != null)
                        {
                            myAssociateFontChartset = font.Encoding;
                        }
                        else
                        {
                            myAssociateFontChartset = null;
                        }
                    }
				}
			}// while( reader.ReadToken() != null )
			while( groups.Count > 0 )
			{
				NewGroup = ( RTFNodeGroup ) groups.Pop();
				NewGroup.MergeText();
			}
			//this.UpdateInformation();
		}

		/// <summary>
		/// write rtf
		/// </summary>
		/// <param name="writer">RTF writer</param>
		public override void Write(RTFWriter writer)
		{
			writer.Encoding = this.Encoding ;
			base.Write (writer);
		}

		/// <summary>
		/// save rtf file
		/// </summary>
		/// <param name="strFileName">file name</param>
		public void Save( string strFileName )
		{
			using( RTFWriter writer = new RTFWriter( strFileName ))
			{
				this.Write( writer );
				writer.Close();
			}
		}
		/// <summary>
		/// Save rtf to a stream
		/// </summary>
		/// <param name="stream">stream</param>
		public void Save( System.IO.Stream stream )
		{
			using( RTFWriter writer = new RTFWriter( new System.IO.StreamWriter( stream , this.Encoding )))
			{
				this.Write( writer );
				writer.Close();
			}
		}
	}
}