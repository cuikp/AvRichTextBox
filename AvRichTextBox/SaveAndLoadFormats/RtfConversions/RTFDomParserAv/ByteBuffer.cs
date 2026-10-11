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
	/// Binary data buffer
	/// </summary>
	public class ByteBuffer
	{
		/// <summary>
		/// Initialize instance
		/// </summary>
		public ByteBuffer()
		{
		}

		/// <summary>
		/// Current contains validate bytes
		/// </summary>
		protected int intCount = 0 ;
		/// <summary>
		/// byte array 
		/// </summary>
		protected byte[] bsBuffer = new byte[ 16 ];

		/// <summary>
		/// Clear object's data
		/// </summary>
		public virtual void Clear()
		{
			bsBuffer = new byte[ 16 ];
			intCount = 0 ;
		}

		/// <summary>
		/// Reset current position without clear buffer
		/// </summary>
		public void Reset()
		{
			intCount = 0 ;
		}

		/// <summary>
		/// Set of get byte at special index which starts with 0
		/// </summary>
		public byte this[ int index ]
		{
			get
			{
				if( index >= 0 && index < intCount )
					return bsBuffer[ index ] ;
				else
					throw new IndexOutOfRangeException("index");
			}
			set
			{
				if( index >= 0 && index < intCount )
					bsBuffer[ index ] = value ;
				else
					throw new IndexOutOfRangeException("index");
			}
		}
		/// <summary>
		/// Validate bytes count
		/// </summary>
		public virtual int Count
		{
			get
            {
                return intCount;
            }
		}

		/// <summary>
		/// Add a byte
		/// </summary>
		/// <param name="b">byte data</param>
		public void Add( byte b )
		{
			FixBuffer( intCount + 1 );
			bsBuffer[intCount] = b ;
			intCount ++;
		}

		/// <summary>
		/// Add bytes
		/// </summary>
		/// <param name="bs">bytes</param>
		public void Add( byte[] bs )
		{
			if( bs != null)
				Add( bs , 0 , bs.Length );
		}
		/// <summary>
		/// Add bytes
		/// </summary>
		/// <param name="bs">Bytes</param>
		/// <param name="StartIndex">Start index</param>
		/// <param name="Length">Length</param>
		public void Add(byte[] bs , int StartIndex , int Length )
		{
			if( bs != null && StartIndex >= 0 && (StartIndex + Length ) <= bs.Length && Length > 0 )
			{
				FixBuffer(intCount + Length );
				Array.Copy(bs , StartIndex , bsBuffer , intCount , Length );
				intCount += Length ;
			}
		}

		/// <summary>
		/// Get validate bytes array
		/// </summary>
		/// <returns>bytes array</returns>
		public byte[] ToArray()
		{
			if( intCount > 0 )
			{
				byte[] bs = new byte[ intCount ];
				Array.Copy( bsBuffer , 0 , bs , 0 , intCount );
				return bs ;
			}
			else
				return null!;
		}

		/// <summary>
		/// Convert bytes data to a string
		/// </summary>
		/// <param name="encoding">string encoding</param>
		/// <returns>string data</returns>
		public string GetString( System.Text.Encoding encoding )
		{
			if( encoding == null )
				throw new System.ArgumentNullException("encoding");
			if( intCount > 0 )
				return encoding.GetString( bsBuffer , 0 , intCount );
			else
				return "";
		}
		/// <summary>
		/// Fix inner buffer so it can fit to new size of buffer
		/// </summary>
		/// <param name="NewSize">new size</param>
		protected void FixBuffer( int NewSize )
		{
			if( NewSize <= bsBuffer.Length )
				return ;
			if( NewSize < (int)( bsBuffer.Length * 1.5 ))
				NewSize = (int)( bsBuffer.Length * 1.5 );
			
			byte[] bs = new byte[ NewSize ];
			Buffer.BlockCopy( bsBuffer , 0 , bs , 0 , bsBuffer.Length );
			//Array.Copy( bsBuffer , 0 , bs , 0 , bsBuffer.Length );
			bsBuffer = bs ;
		}
	}
}