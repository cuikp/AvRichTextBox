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
    /// RTF parser node, this source code evolution from other software.
	/// </summary>
	public class RTFNode
	{
		/// <summary>
		/// initialize instance
		/// </summary>
		public RTFNode()
		{
		}

		public RTFNode( RTFNodeType type , string Key )
		{
			intType = type ;
			this.strKeyword = Key ;
		}
		
        public RTFNode( RTFToken token )
		{
			this.strKeyword = token.Key ;
			this.bolHasParameter = token.HasParam ;
			this.intParameter = token.Param ;
			if( token.Type == RTFTokenType.Control )
				intType = RTFNodeType.Control ;
			else if( token.Type == RTFTokenType.Control )
				intType = RTFNodeType.Control ;
			else if( token.Type == RTFTokenType.Keyword  )
				intType = RTFNodeType.Keyword ;
			else if( token.Type == RTFTokenType.ExtKeyword )
				intType = RTFNodeType.ExtKeyword ;
			else if( token.Type == RTFTokenType.Text )
				intType = RTFNodeType.Text ;
			else
				intType = RTFNodeType.Text ;
		}

		protected RTFNodeGroup myParent = null;
		/// <summary>
		/// parent node
		/// </summary>
		public virtual RTFNodeGroup Parent
		{
			get{ return myParent ;}
			set{ myParent = value;}
		}

		protected RTFRawDocument myOwnerDocument = null ;
		/// <summary>
		/// raw document which owner this node
		/// </summary>
		public virtual RTFRawDocument OwnerDocument
		{
			get{ return myOwnerDocument ;}
			set
			{
				myOwnerDocument = value;
				if( this.Nodes != null )
				{
					foreach( RTFNode node in this.Nodes )
					{
						node.OwnerDocument = value ;
					}
				}
			}
		}

		/// <summary>
		/// key word
		/// </summary>
		protected string strKeyword = null;
		/// <summary>
		/// Keyword
		/// </summary>
		public virtual string Keyword
		{
			get
            {
                return strKeyword ;
            }
			set
            {
                strKeyword = value;
            }
		}

		/// <summary>
		/// Whether this node has parameter
		/// </summary>
		protected bool bolHasParameter = false;
		/// <summary>
        /// Whether this node has parameter
		/// </summary>
		public virtual bool HasParameter
		{
			get
            {
                return bolHasParameter ;
            }
			set
            {
                bolHasParameter = value;
            }
		}

		protected int intParameter = 0;
		/// <summary>
		/// paramter value
		/// </summary>
		public virtual int Parameter
		{
			get
            {
                return intParameter ;
            }
		}

		/// <summary>
		/// child nodes
		/// </summary>
		public virtual RTFNodeList Nodes
		{
			get{ return null; }
		}


		/// <summary>
		/// index
		/// </summary>
		public int Index
		{
			get
			{
				if( myParent == null )
					return 0 ;
				else
					return myParent.Nodes.IndexOf( this );
			}
		}

		protected RTFNodeType intType = RTFNodeType.None ;
		/// <summary>
		/// node type
		/// </summary>
		public RTFNodeType Type
		{
			get
            {
                return intType ;
            }
		}

		/// <summary>
		/// previous node in parent nodes list
		/// </summary>
		public RTFNode PreviousNode
		{
			get
			{
				if( myParent != null )
				{
					int index = myParent.Nodes.IndexOf( this );
					if( index > 0 )
					{
						return myParent.Nodes[ index - 1 ];
					}
				}
				return null;
			}
		}
		/// <summary>
		/// next node in parent nodes list
		/// </summary>
		public RTFNode NextNode
		{
			get
			{
				if( myParent != null )
				{
					int index = myParent.Nodes.IndexOf( this );
					if( index >= 0 && index < myParent.Nodes.Count - 1 )
						return myParent.Nodes[ index + 1 ] ;
				}
				return null;
			}
		}

		/// <summary>
		/// write to rtf document
		/// </summary>
		/// <param name="writer">RTF text writer</param>
		public virtual void Write( RTFWriter writer )
		{
			if( intType == RTFNodeType.Control
                || intType == RTFNodeType.Keyword
                || intType == RTFNodeType.ExtKeyword )
			{
                if (this.bolHasParameter)
                {
                    writer.WriteKeyword(
                        this.strKeyword + this.intParameter, 
                        this.intType == RTFNodeType.ExtKeyword);
                }
                else
                {
                    writer.WriteKeyword(
                        this.strKeyword,
                        this.intType == RTFNodeType.ExtKeyword);
                }
			}
            else if (intType == RTFNodeType.Text)
            {
                writer.WriteText(this.strKeyword);
            }
		}
	}

	
}