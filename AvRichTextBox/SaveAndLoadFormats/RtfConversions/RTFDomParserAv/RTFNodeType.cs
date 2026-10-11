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
	/// RTF node type
	/// </summary>
	public enum RTFNodeType
	{
		/// <summary>
		/// root
		/// </summary>
		Root ,
		/// <summary>
		/// keyword, etc /marginl
		/// </summary>
		Keyword ,
		/// <summary>
		/// external keyword node , etc. /*/keyword
		/// </summary>
		ExtKeyword ,
		/// <summary>
		/// control
		/// </summary>
		Control ,
		/// <summary>
		/// plain text
		/// </summary>
		Text ,
		/// <summary>
		/// group , etc . { }
		/// </summary>
		Group ,
		/// <summary>
		/// nothing
		/// </summary>
		None
	}
}