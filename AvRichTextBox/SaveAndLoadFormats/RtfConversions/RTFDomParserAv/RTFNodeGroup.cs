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

namespace RtfDomParserAv
{
	/// <summary>
    /// RTF node group , this source code evolution from other software.
	/// </summary>
	public class RTFNodeGroup : RTFNode
	{
		/// <summary>
		/// initialize instance
		/// </summary>
		public RTFNodeGroup()
		{
			this.intType = RTFNodeType.Group ;
		}
		/// <summary>
		/// child node list
		/// </summary>
		protected RTFNodeList myNodes = new RTFNodeList();
		/// <summary>
		/// child node list
		/// </summary>
		public override RTFNodeList Nodes
		{
			get
			{
				return myNodes ;
			}
		}

		/// <summary>
		/// get all child node deeply
		/// </summary>
		/// <param name="IncludeGroupNode">contains group type node</param>
		/// <returns>child nodes list</returns>
		public RTFNodeList GetAllNodes( bool IncludeGroupNode )
		{
				RTFNodeList list = new RTFNodeList();
				this.AddAllNodes( list , IncludeGroupNode );
				return list ;
		}

		private void AddAllNodes( RTFNodeList list , bool IncludeGroupNode )
		{
			foreach( RTFNode node in myNodes )
			{
				if( node is RTFNodeGroup )
				{
					if( IncludeGroupNode )
						list.Add( node );
					( ( RTFNodeGroup ) node ).AddAllNodes( list , IncludeGroupNode );
				}
				else
					list.Add( node );
			}
		}

		/// <summary>
		/// first child node
		/// </summary>
		public RTFNode FirstNode
		{
			get
			{
				if( myNodes.Count > 0 )
					return myNodes[ 0 ];
				else
					return null;
			}
		}

		public override string Keyword
		{
			get
			{
				if( myNodes.Count > 0 )
					return myNodes[ 0 ].Keyword ;
				else
					return null;
			}
			set
			{
				
			}
		}

		public override bool HasParameter
		{
			get
			{
				if( myNodes.Count > 0 )
					return myNodes[ 0 ].HasParameter ;
				else
					return false;
			}
			set
			{
			}
		}

		public override int Parameter
		{
			get
			{
				if( myNodes.Count > 0 )
					return myNodes[ 0 ].Parameter ;
				else
					return 0 ;
			}
		}


		public virtual string Text
		{
			get
			{
				System.Text.StringBuilder myStr = new System.Text.StringBuilder();
				foreach( RTFNode node in myNodes )
				{
					if( node is RTFNodeGroup )
					{
						myStr.Append( ( ( RTFNodeGroup ) node ).Text );
					}
					if( node.Type == RTFNodeType.Text )
						myStr.Append( node.Keyword );
				}
				return myStr.ToString();
			}
		}

		internal void MergeText()
		{
			RTFNodeList list = new RTFNodeList();
			System.Text.StringBuilder myStr = new System.Text.StringBuilder();
			ByteBuffer buffer = new ByteBuffer();
			//System.IO.MemoryStream ms = new System.IO.MemoryStream();
			//System.Text.Encoding encode = myOwnerDocument.Encoding ;
			foreach( RTFNode node in myNodes )
			{
				if( node.Type == RTFNodeType.Text )
				{
					AddString( myStr , buffer );
					myStr.Append( node.Keyword );
					continue ;
				}
				if( node.Type == RTFNodeType.Control 
					&& node.Keyword == "\'"
					&& node.HasParameter )
				{
					buffer.Add( ( byte ) node.Parameter );
					continue ;
				}
				else if( node.Type == RTFNodeType.Control || node.Type == RTFNodeType.Keyword )
				{
					if( node.Keyword == "tab" )
					{
						AddString( myStr , buffer );
						myStr.Append( '\t' );
						continue ;
					}
					if( node.Keyword == "emdash")
					{
						AddString( myStr , buffer );
						// notice!! This code may cause compiler error in OS which not support chinese character.
						// you can change to myStr.Append('-');
						myStr.Append( 'j');
						continue ;
					}
					if( node.Keyword == "" )
					{
						AddString( myStr , buffer );
						// notice!! This code may cause compiler error in OS which not support chinese character.
						// you can change to myStr.Append('-');
						myStr.Append( 'Ƀ' );
						continue ;
					}
				}
				AddString( myStr , buffer );
				if( myStr.Length > 0 )
				{
					list.Add( new RTFNode( RTFNodeType.Text , myStr.ToString()));
					myStr = new System.Text.StringBuilder();
				}
				list.Add( node );
			}//foreach( RTFNode node in myNodes )

			AddString( myStr , buffer );
			if( myStr.Length > 0 )
			{
				list.Add( new RTFNode( RTFNodeType.Text , myStr.ToString()));
			}
			myNodes.Clear();
			foreach( RTFNode node in list )
			{
				node.Parent = this ;
				node.OwnerDocument = myOwnerDocument ;
				myNodes.Add( node );
			}
		}
 
		private void AddString( System.Text.StringBuilder myStr , ByteBuffer buffer )
		{
			if( buffer.Count > 0 )
			{
                //if( buffer.Count == 1 )
                //{
                //    myStr.Append( ( char ) buffer[0] );
                //}
                //else
				{
                    string txt = buffer.GetString(myOwnerDocument.RuntimeEncoding);
					myStr.Append( txt );
				}
				buffer.Reset();
			}
		}
		/// <summary>
		/// write content to rtf document
		/// </summary>
		/// <param name="writer">RTF text writer</param>
		public override void Write(RTFWriter writer)
		{
			writer.WriteStartGroup();
			foreach( RTFNode node in myNodes )
			{
				node.Write( writer );
			}
			writer.WriteEndGroup();
		}

		/// <summary>
		/// search child node special keyword
		/// </summary>
		/// <param name="Key">special keyword</param>
		/// <param name="Deeply">whether search deeplyl</param>
		/// <returns>node find</returns>
		public RTFNode SearchKey( string Key , bool Deeply )
		{
			foreach( RTFNode node in myNodes )
			{
				if( node.Type == RTFNodeType.Keyword 
					|| node.Type == RTFNodeType.ExtKeyword 
					|| node.Type == RTFNodeType.Control )
				{
					if( node.Keyword == Key )
						return node ;
				}
				if( Deeply )
				{
					if( node is RTFNodeGroup )
					{
						RTFNodeGroup g = ( RTFNodeGroup ) node ;
						RTFNode n = g.SearchKey( Key , true );
						if( n != null )
							return n ;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// append child node
		/// </summary>
		/// <param name="node">node</param>
		public void AppendChild( RTFNode node )
		{
			CheckNodes();
			if( node == null )
				throw new System.ArgumentNullException("node");
			if( node == this )
				throw new System.ArgumentException("node != this");
			node.Parent = this ;
			node.OwnerDocument = myOwnerDocument ;
			this.Nodes.Add( node );
		}
		/// <summary>
		/// delete node
		/// </summary>
		/// <param name="node">node</param>
		public void RemoveChild( RTFNode node )
		{
			CheckNodes();
			if( node == null )
				throw new System.ArgumentNullException("node");
			if( node == this )
				throw new System.ArgumentException("node != this");
			this.Nodes.Remove( node );
		}
		/// <summary>
		/// insert node
		/// </summary>
		/// <param name="index">index</param>
		/// <param name="node">node</param>
		public void InsertNode( int index , RTFNode node )
		{
			CheckNodes();
			if( node == null )
				throw new System.ArgumentNullException("node");
			if( node == this )
				throw new System.ArgumentException("node != this");
			node.Parent = this ;
			node.OwnerDocument = myOwnerDocument ;
			this.Nodes.Insert( index , node );
		}

		private void CheckNodes()
		{
			if( this.Nodes == null )
				throw new System.Exception("child node is invalidate");
		}
	}
}