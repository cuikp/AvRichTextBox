/*
 * 
 *  RTFDomParserAv, modified from original:
 *   DCSoft RTF DOM v1.0
 *   Author : Yuan yong fu.
 *   Email  : yyf9989@hotmail.com
 *   blog site:http://www.cnblogs.com/xdesigner.
 * 
 */

using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace RtfDomParserAv
{
    public class RTFBorderStyle
    {
        private bool _Left = false;
        [DefaultValue( false )]
        public bool Left
        {
            get { return _Left; }
            set { _Left = value; }
        }

        private bool _Top = false;
        [DefaultValue(false)]
        public bool Top
        {
            get { return _Top; }
            set { _Top = value; }
        }

        private bool _Right = false;
        [DefaultValue(false)]
        public bool Right
        {
            get { return _Right; }
            set { _Right = value; }
        }

        private bool _Bottom = false;
        [DefaultValue(false)]
        public bool Bottom
        {
            get { return _Bottom; }
            set { _Bottom = value; }
        }

        private IDashStyle _Style = DashStyle.Dash;
        public IDashStyle Style
        {
            get { return _Style; }
            set { _Style = value; }
        }

        private Color _Color = Colors.Black;
        [DefaultValue(typeof( Color ) , "Black")]
        public Color Color
        {
            get { return _Color; }
            set { _Color = value; }
        }

        private bool _Thickness = false;
        /// <summary>
        /// 粗边框样式
        /// </summary>
        [DefaultValue( false)]
        public bool Thickness
        {
            get { return _Thickness; }
            set { _Thickness = value; }
        }
        /// <summary>
        /// 复制对象
        /// </summary>
        /// <returns>复制品</returns>
        public RTFBorderStyle Clone()
        {
            RTFBorderStyle b = new RTFBorderStyle();
            b._Bottom = this._Bottom;
            b._Color = this._Color;
            b._Left = this._Left;
            b._Right = this._Right;
            b._Style = this._Style;
            b._Top = this._Top;
            b._Thickness = this._Thickness;
            return b;
        }

        public bool EqualsValue(RTFBorderStyle b)
        {
            if (b == this)
            {
                return true;
            }
            if (b == null)
            {
                return false;
            }
            if (b._Bottom != this._Bottom
                || b._Color != this._Color
                || b._Left != this._Left
                || b._Right != this._Right
                || b._Style != this._Style
                || b._Top != this._Top
                || b._Thickness != this._Thickness )
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
