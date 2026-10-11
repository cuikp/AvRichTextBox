/*
 * 
 *  RTFDomParserAv, modified from original:
 *   DCSoft RTF DOM v1.0
 *   Author : Yuan yong fu.
 *   Email  : yyf9989@hotmail.com
 *   blog site:http://www.cnblogs.com/xdesigner.
 * 
 */

namespace RtfDomParserAv;

public class RTFRawLayerInfo
{
    public int UCValue { get; set { field = value; UCValueCount = 0; } } = 0;
    public int UCValueCount { get; set ; } = 0;

    /// <summary>
    /// 检查UC累计值
    /// </summary>
    /// <returns>检查通过，能添加单字节字符</returns>
    public bool CheckUCValueCount()
    {
        UCValueCount--;
        return UCValueCount < 0;
    }
    
    public RTFRawLayerInfo Clone() => (RTFRawLayerInfo)this.MemberwiseClone();
    
}
