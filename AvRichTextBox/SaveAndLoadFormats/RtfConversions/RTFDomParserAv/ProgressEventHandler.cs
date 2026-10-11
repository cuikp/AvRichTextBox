/*
 * 
 *  RTFDomParserAv, modified from original:
 *   DCSoft RTF DOM v1.0
 *   Author : Yuan yong fu.
 *   Email  : yyf9989@hotmail.com
 *   blog site:http://www.cnblogs.com/xdesigner.
 * 
 */

namespace RtfDomParserAv
{
    public delegate void ProgressEventHandler( object sender , ProgressEventArgs args );

    public class ProgressEventArgs(int max, int Value, string message) : EventArgs 
    {
        private readonly int intMaxValue = max;
        public int MaxValue => intMaxValue;

        private readonly int intValue = Value;
        public int Value => intValue;

        private readonly string strMessage = message;
        public string Message => strMessage;

        private bool bolCancel = false;
        public bool Cancel
        {
            get => bolCancel;
            set => bolCancel = value; 
        }
    }
}
