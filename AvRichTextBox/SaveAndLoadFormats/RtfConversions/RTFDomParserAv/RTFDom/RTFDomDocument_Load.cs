using Avalonia.Media;
using System.Diagnostics;
using System.Text;

namespace RtfDomParserAv
{
   public partial class RTFDomDocument
   {
      private int intTokenCount = 0;
      private DocumentFormatInfo _ParagraphFormat = null;

      private void Load(RTFReader reader, DocumentFormatInfo parentFormat)
      {

         bool ForbitPard = false;
         DocumentFormatInfo format = null;

         if (_ParagraphFormat == null)
            _ParagraphFormat = new DocumentFormatInfo();

         if (parentFormat == null)
            format = new DocumentFormatInfo();
         else
         {
            format = parentFormat.Clone();
            format.NativeLevel = parentFormat.NativeLevel + 1;
         }


         RTFTextContainer myText = new(this);
         int levelBack = reader.Level;
         while (reader.ReadToken() != null)
         {
            if (reader.TokenCount - intTokenCount > 100)
            {
               intTokenCount = reader.TokenCount;
               OnProgress(reader.ContentLength, reader.ContentPosition, null);
            }
            if (bolStartContent)
            {
               if (myText.Accept(reader.CurrentToken, reader))
               {
                  myText.Level = reader.Level;
                  continue;
               }
               else if (myText.HasContent)
               {
                  if (ApplyText(myText, reader, format))
                     break;
               }
            }

            if (reader.TokenType == RTFTokenType.GroupEnd)
            {
               RTFDomElement[] elements = GetLastElements(true);
               for (int iCount = 0; iCount < elements.Length; iCount++)
               {
                  RTFDomElement element = elements[iCount];
                  if (element.NativeLevel >= 0 && element.NativeLevel > reader.Level)
                  {
                     for (int iCount2 = iCount; iCount2 < elements.Length; iCount2++)
                        elements[iCount2].Locked = true;
                     break;
                  }
               }

               break;
            }

            if (reader.Level < levelBack)
               break;

            if (reader.TokenType == RTFTokenType.GroupStart)
            {
               //level++;
               Load(reader, format);
               if (reader.Level < levelBack)
                  break;
               //continue;
            }

            if (reader.TokenType is RTFTokenType.Control or RTFTokenType.Keyword or RTFTokenType.ExtKeyword)
            {

               switch (reader.Keyword)
               {
                  case "rtlch":
                     break;


                  case "fromhtml":
                     // 以HTML方式读取文档内容
                     ReadHtmlContent(reader);
                     return;
                  case RTFConsts._listtable:
                     ReadListTable(reader);
                     return;
                  case RTFConsts._listoverride:
                     // unknow keyword
                     ReadToEndGround(reader);
                     break;
                  case RTFConsts._ansi:
                     break;
                  case RTFConsts._ansicpg:
                     // read default encoding
                     myDefaultEncoding = Encoding.GetEncoding(reader.Parameter);
                     break;
                  case RTFConsts._fonttbl:
                     // read font table
                     ReadFontTable(reader);
                     break;
                  case "listoverridetable":
                     ReadListOverrideTable(reader);
                     break;
                  case "filetbl":
                     // unsupport file list
                     ReadToEndGround(reader);
                     break;// finish current level
                           //break;
                  case RTFConsts._colortbl:
                     // read color table
                     ReadColorTable(reader);
                     return;// finish current level
                            //break;
                  case "stylesheet":
                     // unsupport style sheet list
                     ReadToEndGround(reader);
                     break;
                  case RTFConsts._generator:
                     // read document generator
                     this.Generator = ReadInnerText(reader, true);
                     break;
                  case RTFConsts._info:
                     // read document information
                     ReadDocumentInfo(reader);
                     return;
                  case RTFConsts._headery:
                     {
                        if (reader.HasParam)
                        {
                           this.HeaderDistance = reader.Parameter;
                        }
                     }
                     break;
                  case RTFConsts._footery:
                     {
                        if (reader.HasParam)
                        {
                           this.FooterDistance = reader.Parameter;
                        }
                     }
                     break;
                  case RTFConsts._header:
                     {
                        // analyse header
                        RTFDomHeader header = new RTFDomHeader();
                        header.Style = HeaderFooterStyle.AllPages;
                        this.AppendChild(header);
                        Load(reader, parentFormat);
                        header.Locked = true;
                        _ParagraphFormat = new DocumentFormatInfo();
                        break;
                     }
                  case RTFConsts._headerl:
                     {
                        // analyse header
                        RTFDomHeader header = new RTFDomHeader();
                        header.Style = HeaderFooterStyle.LeftPages;
                        this.AppendChild(header);
                        Load(reader, parentFormat);
                        header.Locked = true;
                        _ParagraphFormat = new DocumentFormatInfo();
                        break;
                     }
                  case RTFConsts._headerr:
                     {
                                // analyse header
                                RTFDomHeader header = new() { Style = HeaderFooterStyle.RightPages };
                                this.AppendChild(header);
                        Load(reader, parentFormat);
                        header.Locked = true;
                        _ParagraphFormat = new DocumentFormatInfo();
                        break;
                     }
                  case RTFConsts._headerf:
                     {
                                // analyse header
                                RTFDomHeader header = new() { Style = HeaderFooterStyle.FirstPage };
                                this.AppendChild(header);
                        Load(reader, parentFormat);
                        header.Locked = true;
                        _ParagraphFormat = new DocumentFormatInfo();
                        break;
                     }
                  case RTFConsts._footer:
                     {
                        // analyse footer
                        RTFDomFooter footer = new RTFDomFooter();
                        footer.Style = HeaderFooterStyle.AllPages;
                        this.AppendChild(footer);
                        Load(reader, parentFormat);
                        footer.Locked = true;
                        _ParagraphFormat = new DocumentFormatInfo();
                        break;
                     }
                  case RTFConsts._footerl:
                     {
                        // analyse footer
                        RTFDomFooter footer = new RTFDomFooter();
                        footer.Style = HeaderFooterStyle.LeftPages;
                        this.AppendChild(footer);
                        Load(reader, parentFormat);
                        footer.Locked = true;
                        _ParagraphFormat = new DocumentFormatInfo();
                        break;
                     }
                  case RTFConsts._footerr:
                     {
                        // analyse footer
                        RTFDomFooter footer = new RTFDomFooter();
                        footer.Style = HeaderFooterStyle.RightPages;
                        this.AppendChild(footer);
                        Load(reader, parentFormat);
                        footer.Locked = true;
                        _ParagraphFormat = new DocumentFormatInfo();
                        break;
                     }
                  case RTFConsts._footerf:
                     {
                        // analyse footer
                        RTFDomFooter footer = new RTFDomFooter();
                        footer.Style = HeaderFooterStyle.FirstPage;
                        this.AppendChild(footer);
                        Load(reader, parentFormat);
                        footer.Locked = true;
                        _ParagraphFormat = new DocumentFormatInfo();
                        break;
                     }
                  case RTFConsts._xmlns:
                     { // unsupport xml namespace
                        ReadToEndGround(reader);
                        break;
                     }
                  case RTFConsts._nonesttables:
                     { // I support nest table , then ignore this keyword
                        ReadToEndGround(reader);
                        break;
                     }
                  case RTFConsts._xmlopen:
                     { // unsupport xmlopen keyword
                        break;
                     }
                  case RTFConsts._revtbl:
                     { //ReadToEndGround(reader);
                        break;
                     }


                  //**************** read document information ***********************
                  case RTFConsts._paperw:
                     { // read paper width
                        intPaperWidth = reader.Parameter;
                        break;
                     }
                  case RTFConsts._paperh:
                     { // read paper height
                        intPaperHeight = reader.Parameter;
                        break;
                     }
                  case RTFConsts._margl:
                     { // read left margin
                        intLeftMargin = reader.Parameter;
                        break;
                     }
                  case RTFConsts._margr:
                     { // read right margin
                        intRightMargin = reader.Parameter;
                        break;
                     }
                  case RTFConsts._margb:
                     { // read bottom margin
                        intBottomMargin = reader.Parameter;
                        break;
                     }
                  case RTFConsts._margt:
                     { // read top margin 
                        intTopMargin = reader.Parameter;
                        break;
                     }
                  case RTFConsts._landscape:
                     { // set landscape
                        bolLandscape = true;
                        break;
                     }
                  case RTFConsts._fchars:
                     this.FollowingChars = ReadInnerText(reader, true);
                     break;
                  case RTFConsts._lchars:
                     this.LeadingChars = ReadInnerText(reader, true);
                     break;
                  case "pnseclvl":
                     // ignore this keyword
                     ReadToEndGround(reader);
                     break; ;
                  ////**************** read html content ***************************
                  //case "htmlrtf":
                  //    {
                  //        if (reader.HasParam && reader.Parameter == 0)
                  //        {
                  //            _RTFHtmlState = false;
                  //        }
                  //        else
                  //        {
                  //            _RTFHtmlState = true;
                  //            //while ( reader.PeekTokenType() == RTFTokenType.Text )
                  //            //{
                  //            //    reader.ReadToken();
                  //            //}
                  //            //string text = ReadInnerText(reader, null, false, true, false);
                  //        }
                  //    }
                  //    break;
                  //case "htmltag":
                  //    {
                  //        if (reader.InnerReader.Peek() == (int)' ')
                  //        {
                  //            reader.InnerReader.Read();
                  //        }
                  //        string text = ReadInnerText(reader, null, true, false, true);
                  //        if (string.IsNullOrEmpty(text) == false)
                  //        {
                  //            _HtmlContentBuilder.Append(text);
                  //        }
                  //        //while (true)
                  //        //{
                  //        //    int c = reader.InnerReader.Peek();
                  //        //    if (c < 0)
                  //        //    {
                  //        //        break;
                  //        //    }
                  //        //    if (c == '}')
                  //        //    {
                  //        //        break;
                  //        //    }
                  //        //    _HtmlContentBuilder.Append((char)c);
                  //        //    reader.InnerReader.Read();
                  //        //}
                  //    }
                  //    break;
                  //**************** read paragraph format ***********************
                  case RTFConsts._pard:
                     {
                        bolStartContent = true;
                        if (ForbitPard)
                           continue;
                        // clear paragraph format
                        _ParagraphFormat.ResetParagraph();
                         //format.ResetParagraph();
                        break;
                     }
                  case RTFConsts._par:
                     {
                        bolStartContent = true;
                        // new paragraph
                        if (GetLastElement(typeof(RTFDomParagraph)) == null)
                        {
                           RTFDomParagraph p = new () { Format = _ParagraphFormat };
                           _ParagraphFormat = _ParagraphFormat.Clone();
                           AddContentElement(p);
                           p.Locked = true;
                        }
                        else
                        {
                           this.CompleteParagraph();
                           RTFDomParagraph p = new() { Format = _ParagraphFormat };
                           AddContentElement(p);

                           if (p.Parent is RTFDomTableCell cell)
                              p.NativeLevel = cell.NativeLevel;
                        }
                        bolStartContent = true;

                        break;
                     }
                  case RTFConsts._page:
                     {
                        bolStartContent = true;
                        this.CompleteParagraph();
                        this.AddContentElement(new RTFDomPageBreak());
                        break;
                     }
                  case RTFConsts._pagebb:
                     {
                        bolStartContent = true;
                        _ParagraphFormat.PageBreak = true;
                        break;
                     }
                  case RTFConsts._ql:
                     {
                        // left alignment
                        bolStartContent = true;
                        _ParagraphFormat.Align = RTFAlignment.Left;
                        break;
                     }
                  case RTFConsts._qc:
                     {
                        // center alignment
                        bolStartContent = true;
                        _ParagraphFormat.Align = RTFAlignment.Center;
                        break;
                     }
                  case RTFConsts._qr:
                     {
                        // right alignment
                        bolStartContent = true;
                        _ParagraphFormat.Align = RTFAlignment.Right;
                        break;
                     }
                  case RTFConsts._qj:
                     {
                        // jusitify alignment
                        bolStartContent = true;
                        _ParagraphFormat.Align = RTFAlignment.Justify;
                        break;
                     }
                  case RTFConsts._sl:
                     {
                        // line spacing
                        bolStartContent = true;
                        if (reader.Parameter >= 0)
                        {
                           _ParagraphFormat.LineSpacing = reader.Parameter;
                        }
                     }
                     break;
                  case RTFConsts._slmult:
                     {
                        bolStartContent = true;
                        _ParagraphFormat.MultipleLineSpacing = (reader.Parameter == 1);
                     }
                     break;
                  case RTFConsts._sb:
                     {
                        // spacing before paragraph
                        bolStartContent = true;
                        _ParagraphFormat.SpacingBefore = reader.Parameter;
                     }
                     break;
                  case RTFConsts._sa:
                     {
                        // spacing after paragraph
                        bolStartContent = true;
                        _ParagraphFormat.SpacingAfter = reader.Parameter;
                     }
                     break;
                  case RTFConsts._fi:
                     {
                        // indent first line
                        bolStartContent = true;
                        _ParagraphFormat.ParagraphFirstLineIndent = reader.Parameter;
                        //if (reader.Parameter >= 400)
                        //{
                        //    _ParagraphFormat.ParagraphFirstLineIndent = reader.Parameter; //doc.StandTabWidth;
                        //}
                        //else
                        //{
                        //    _ParagraphFormat.ParagraphFirstLineIndent = 0;
                        //}
                        ////UpdateParagraph( CurrentParagraphEOF , format );
                        break;
                     }
                  case RTFConsts._brdrw:
                     {
                        //bolStartContent = true;
                        //if (reader.HasParam)
                        //{
                        //   _ParagraphFormat.BorderWidth = reader.Parameter;
                        //}
                        //break;

                        bolStartContent = true;

                        var element = this.GetLastElement(typeof(RTFDomTableRow), false);
                        if (element is RTFDomTableRow row && row.CellSettings.Count > 0 && reader.HasParam)
                        {
                           var style = (RTFAttributeList)row.CellSettings[row.CellSettings.Count - 1];
                           style.Add(reader.Keyword, reader.Parameter);
                        }
                        else if (reader.HasParam)
                        {
                           _ParagraphFormat.BorderWidth = reader.Parameter;
                        }
                        break;



                     }
                  case RTFConsts._pn:
                     {
                        bolStartContent = true;
                        _ParagraphFormat.ListID = -1;
                        break;
                     }

                  case RTFConsts._pntext:
                     break;
                  case RTFConsts._pntxtb:
                     break;
                  case RTFConsts._pntxta:
                     break;

                  case RTFConsts._pnlvlbody:
                     {
                        // numbered list style
                        bolStartContent = true;
                        //_ParagraphFormat.NumberedList = true;
                        //_ParagraphFormat.BulletedList = false;
                        //if (_ParagraphFormat.Parent != null)
                        //{
                        //    _ParagraphFormat.Parent.NumberedList = format.NumberedList;
                        //    _ParagraphFormat.Parent.BulletedList = format.BulletedList;
                        //}
                        break;
                     }
                  case RTFConsts._pnlvlblt:
                     {
                        // bulleted list style
                        bolStartContent = true;
                        //_ParagraphFormat.NumberedList = false;
                        //_ParagraphFormat.BulletedList = true;
                        //if (_ParagraphFormat.Parent != null)
                        //{
                        //    _ParagraphFormat.Parent.NumberedList = format.NumberedList;
                        //    _ParagraphFormat.Parent.BulletedList = format.BulletedList;
                        //}
                        break;
                     }
                  case RTFConsts._listtext:
                     {
                        bolStartContent = true;
                        string txt = ReadInnerText(reader, true);
                        if (txt != null)
                        {
                           txt = txt.Trim();

                           if (txt.StartsWith('l'))
                              ListTextFlag = 1;
                           else
                              ListTextFlag = 2;

                           //Add innertext as RTFDomText
                           var textNode = new RTFDomText();
                           textNode.Text = txt + ' ';
                           //listNode.Format = this._ParagraphFormat;
                           AddContentElement(textNode);

                        }
                        break;
                     }
                  case RTFConsts._ls:
                     {
                        bolStartContent = true;
                        _ParagraphFormat.ListID = reader.Parameter;

                        //if (ListTextFlag == 1)
                        //{
                        //   _ParagraphFormat.NumberedList = false;
                        //   _ParagraphFormat.BulletedList = true;
                        //}
                        //else if (ListTextFlag == 2)
                        //{
                        //   _ParagraphFormat.NumberedList = true;
                        //   _ParagraphFormat.BulletedList = false;
                        //}
                        ListTextFlag = 0;
                        break;
                     }
                  case RTFConsts._li:
                     {
                        bolStartContent = true;
                        if (reader.HasParam)
                        {
                           _ParagraphFormat.LeftIndent = reader.Parameter;
                        }
                        break;
                     }
                  case RTFConsts._line:
                     {
                        bolStartContent = true;
                        // break line
                        //if (this._RTFHtmlState == false)
                        //{
                        //    this._HtmlContentBuilder.Append(Environment.NewLine);
                        //}
                        //else
                        {
                           if (format.ReadText)
                           {
                              RTFDomLineBreak line = new RTFDomLineBreak();
                              line.NativeLevel = reader.Level;
                              AddContentElement(line);
                           }
                        }
                        break;
                     }
                  // ****************** read text format ******************************
                  case RTFConsts._insrsid:
                     break;
                  case RTFConsts._plain:
                     {
                        // clear text format
                        bolStartContent = true;
                        format.ResetText();
                        break;
                     }
                  case RTFConsts._f:
                     {
                        // font name
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           string FontName = this.FontTable.GetFontName(reader.Parameter);
                           if (FontName != null)
                              FontName = FontName.Trim();
                           if (FontName == null || FontName.Length == 0)
                              FontName = DefaultFontName;

                           if (this.ChangeTimesNewRoman)
                           {
                              if (FontName == "Times New Roman")
                              {
                                 FontName = DefaultFontName;
                              }
                           }
                           format.FontName = FontName;
                        }
                        myFontChartset = this.FontTable[reader.Parameter].Encoding;
                        break;
                     }
                  case RTFConsts._af:
                     {
                        myAssociateFontChartset = this.FontTable[reader.Parameter].Encoding;
                        break;
                     }
                  case RTFConsts._fs:
                     {
                        // font size
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           if (reader.HasParam)
                           {
                              format.FontSize = reader.Parameter / 2.0f;
                           }
                        }
                        break;
                     }
                  case RTFConsts._cf:
                     {
                        // font color
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           if (reader.HasParam)
                           {
                              format.TextColor = this.ColorTable.GetColor(reader.Parameter, Colors.Black);
                           }
                        }
                        break;
                     }

                  case RTFConsts._cb:
                  case RTFConsts._chcbpat:
                     {
                        // background color
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           if (reader.HasParam)
                              format.BackColor = this.ColorTable.GetColor(reader.Parameter, Colors.Transparent);
                        }
                        break;
                     }
                  
                  case RTFConsts._cbpat:
                     {
                        // par background color
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           if (reader.HasParam)
                           {
                              _ParagraphFormat.BackColor = this.ColorTable.GetColor(reader.Parameter, Colors.Transparent);
                              //Debug.WriteLine("Parbackcolor found: " + reader.Keyword + " :: " + _ParagraphFormat.BackColor.ToString());
                           }
                        }
                        break;
                     }
                  case RTFConsts._b:
                     {
                        // bold
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           format.Bold = (reader.HasParam == false || reader.Parameter != 0);
                        }
                        break;
                     }

                  case RTFConsts._v:
                     {
                        // hidden text
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           if (reader.HasParam && reader.Parameter == 0)
                           {
                              format.Hidden = false;
                           }
                           else
                           {
                              format.Hidden = true;
                           }
                        }
                        break;
                     }
                  case RTFConsts._highlight:
                     {
                        // highlight content
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           if (reader.HasParam)
                              format.BackColor = this.ColorTable.GetColor(reader.Parameter, Colors.Transparent);
                        }
                        break;
                     }
                  case RTFConsts._i:
                     {
                        // italic
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           format.Italic = (reader.HasParam == false || reader.Parameter != 0);
                        }
                        break;
                     }
                  case RTFConsts._ul:
                     {
                        // under line
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           format.Underline = (reader.HasParam == false || reader.Parameter != 0);
                        }
                        break;
                     }
                  case RTFConsts._noul:
                     {
                        // no underline
                        bolStartContent = true;
                        format.Underline = false;

                        break;
                     }

                  case RTFConsts._strike:
                     {
                        // strikeout
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           format.Strikeout = (reader.HasParam == false || reader.Parameter != 0);
                        }
                        break;
                     }
                  case RTFConsts._sub:
                     {
                        // subscript
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           format.Subscript = (reader.HasParam == false || reader.Parameter != 0);
                        }
                        break;
                     }
                  case RTFConsts._super:
                     {
                        // superscript
                        bolStartContent = true;
                        if (format.ReadText)
                        {
                           format.Superscript = (reader.HasParam == false || reader.Parameter != 0);
                        }
                        break;
                     }
                  case RTFConsts._nosupersub:
                     {
                        // nosupersub
                        bolStartContent = true;
                        format.Subscript = false;
                        format.Superscript = false;
                        break;
                     }
                  case RTFConsts._brdrb:
                     {
                        bolStartContent = true;
                        //format.ParagraphBorder.Bottom = true;
                        _ParagraphFormat.BottomBorder = true;
                        break;
                     }
                  case RTFConsts._brdrl:
                     {
                        bolStartContent = true;
                        //format.ParagraphBorder.Left = true ;
                        _ParagraphFormat.LeftBorder = true;
                        break;
                     }
                  case RTFConsts._brdrr:
                     {
                        bolStartContent = true;
                        //format.ParagraphBorder.Right = true ;
                        _ParagraphFormat.RightBorder = true;
                        break;
                     }
                  case RTFConsts._brdrt:
                     {
                        bolStartContent = true;
                        //format.ParagraphBorder.Top = true;
                        _ParagraphFormat.BottomBorder = true;
                        break;
                     }
                  case RTFConsts._brdrcf:
                     {
                        bolStartContent = true;
                        RTFDomElement element = this.GetLastElement(typeof(RTFDomTableRow), false);
                        if (element is RTFDomTableRow)
                        {
                           // reading a table row
                           RTFDomTableRow row = (RTFDomTableRow)element;
                           RTFAttributeList style = null;
                           if (row.CellSettings.Count > 0)
                           {
                              style = (RTFAttributeList)row.CellSettings[row.CellSettings.Count - 1];
                              style.Add(reader.Keyword, reader.Parameter);
                           }
                           //else
                           //{
                           //    style = new RTFAttributeList();
                           //    row.CellSettings.Add(style);
                           //}

                        }
                        else
                        {
                           _ParagraphFormat.BorderColor = this.ColorTable.GetColor(reader.Parameter, Colors.Black);
                           format.BorderColor = format.BorderColor;
                        }
                        break;
                     }
                  case RTFConsts._brdrs:
                     {
                        bolStartContent = true;
                        _ParagraphFormat.BorderThickness = false;
                        format.BorderThickness = false;
                        break;
                     }
                  case RTFConsts._brdrth:
                     {
                        bolStartContent = true;
                        _ParagraphFormat.BorderThickness = true;
                        format.BorderThickness = true;
                        break;
                     }
                  case RTFConsts._brdrdot:
                     {
                        bolStartContent = true;
                        _ParagraphFormat.BorderStyle = DashStyle.Dot;
                        format.BorderStyle = DashStyle.Dot;
                        break;
                     }
                  case RTFConsts._brdrdash:
                     {
                        bolStartContent = true;
                        _ParagraphFormat.BorderStyle = DashStyle.Dash;
                        format.BorderStyle = DashStyle.Dash;
                        break;
                     }
                  case RTFConsts._brdrdashd:
                     {
                        bolStartContent = true;
                        _ParagraphFormat.BorderStyle = DashStyle.DashDot;
                        format.BorderStyle = DashStyle.DashDot;
                        break;
                     }
                  case RTFConsts._brdrdashdd:
                     {
                        bolStartContent = true;
                        _ParagraphFormat.BorderStyle = DashStyle.DashDotDot;
                        format.BorderStyle = DashStyle.DashDotDot;
                        break;
                     }
                  case RTFConsts._brdrnil:
                     {
                        bolStartContent = true;
                        _ParagraphFormat.LeftBorder = false;
                        _ParagraphFormat.TopBorder = false;
                        _ParagraphFormat.RightBorder = false;
                        _ParagraphFormat.BottomBorder = false;

                        format.LeftBorder = false;
                        format.TopBorder = false;
                        format.RightBorder = false;
                        format.BottomBorder = false;
                        break;
                     }
                  case RTFConsts._brsp:
                     {
                        bolStartContent = true;
                        if (reader.HasParam)
                        {
                           _ParagraphFormat.BorderSpacing = reader.Parameter;
                        }
                        break;
                     }
                  case RTFConsts._chbrdr:
                     {
                        bolStartContent = true;
                        format.LeftBorder = true;
                        format.TopBorder = true;
                        format.RightBorder = true;
                        format.BottomBorder = true;
                        break;
                     }
                  case RTFConsts._bkmkstart:
                     {
                        // book mark
                        bolStartContent = true;
                        if (format.ReadText && bolStartContent)
                        {
                           RTFDomBookmark bk = new RTFDomBookmark();
                           bk.Name = ReadInnerText(reader, true);
                           bk.Locked = true;
                           AddContentElement(bk);
                        }
                        break;
                     }
                  case RTFConsts._bkmkend:
                     {
                        ForbitPard = true;
                        format.ReadText = false;
                        break;
                     }
                  case RTFConsts._field:
                     {
                        // field
                        bolStartContent = true;
                        ReadDomField(reader, format);
                        return; // finish current level
                                //break;
                     }

                  //case RTFConsts._objdata:
                  //case RTFConsts._objclass:
                  //    {
                  //        ReadToEndGround(reader);
                  //        break;
                  //    }

                  #region read object *********************************

                  case RTFConsts._object:
                     {
                        // object
                        bolStartContent = true;
                        ReadDomObject(reader, format);
                        return;// finish current level
                     }

                  #endregion

                  #region read image **********************************

                  case RTFConsts._shppict:
                     // continue the following token
                     break;
                  case RTFConsts._nonshppict:
                     // unsupport keyword
                     ReadToEndGround(reader);
                     break;
                  case RTFConsts._pict:
                     {
                        // read image data
                        //ReadDomImage(reader, format);
                        bolStartContent = true;
                        RTFDomImage img = new RTFDomImage();
                        img.NativeLevel = reader.Level;
                        AddContentElement(img);
                     }
                     break;
                  case RTFConsts._picscalex:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.ScaleX = reader.Parameter;
                        }
                        break;
                     }
                  case RTFConsts._picscaley:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.ScaleY = reader.Parameter;
                        }
                        break;
                     }
                  case RTFConsts._picwgoal:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.DesiredWidth = reader.Parameter;
                        }
                        break;
                     }
                  case RTFConsts._pichgoal:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.DesiredHeight = reader.Parameter;
                        }
                        break;
                     }
                  case RTFConsts._blipuid:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.ID = ReadInnerText(reader, true);
                        }
                        break;
                     }
                  case RTFConsts._emfblip:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.PicType = RTFPicType.Emfblip;
                        }
                        break;
                     }
                  case RTFConsts._pngblip:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.PicType = RTFPicType.Pngblip;
                        }
                        break;
                     }
                  case RTFConsts._jpegblip:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.PicType = RTFPicType.Jpegblip;
                        }
                        break;
                     }
                  case RTFConsts._macpict:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.PicType = RTFPicType.Macpict;
                        }
                        break;
                     }
                  case RTFConsts._pmmetafile:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.PicType = RTFPicType.Pmmetafile;
                        }
                        break;
                     }
                  case RTFConsts._wmetafile:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.PicType = RTFPicType.Wmetafile;
                        }
                        break;
                     }
                  case RTFConsts._dibitmap:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.PicType = RTFPicType.Dibitmap;
                        }
                        break;
                     }
                  case RTFConsts._wbitmap:
                     {
                        RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                        if (img != null)
                        {
                           img.PicType = RTFPicType.Wbitmap;
                        }
                        break;
                     }
                  #endregion

                  #region read shape ************************************************
                  case RTFConsts._sp:
                     {
                        // begin read shape property
                        int level = 0;
                        string vName = null;
                        string vValue = null;
                        while (reader.ReadToken() != null)
                        {
                           if (reader.TokenType == RTFTokenType.GroupStart)
                           {
                              level++;
                           }
                           else if (reader.TokenType == RTFTokenType.GroupEnd)
                           {
                              level--;
                              if (level < 0)
                              {
                                 break;
                              }
                           }
                           else if (reader.Keyword == RTFConsts._sn)
                           {
                              vName = ReadInnerText(reader, true);
                           }
                           else if (reader.Keyword == RTFConsts._sv)
                           {
                              vValue = ReadInnerText(reader, true);
                           }
                        }//while
                        RTFDomShape shape = (RTFDomShape)GetLastElement(typeof(RTFDomShape));
                        if (shape != null)
                        {
                           shape.ExtAttrbutes[vName] = vValue;
                        }
                        else
                        {
                           RTFDomShapeGroup g = (RTFDomShapeGroup)GetLastElement(typeof(RTFDomShapeGroup));
                           if (g != null)
                           {
                              g.ExtAttrbutes[vName] = vValue;
                           }
                        }
                        break;
                     }
                  case RTFConsts._shptxt:
                     {
                        // handle following token
                        break;
                     }
                  case RTFConsts._shprslt:
                     {
                        // ignore this level
                        ReadToEndGround(reader);
                        break;
                     }
                  case RTFConsts._shp:
                     {
                        bolStartContent = true;
                        RTFDomShape shape = new RTFDomShape();
                        shape.NativeLevel = reader.Level;
                        AddContentElement(shape);
                        break;
                     }
                  case RTFConsts._shpleft:
                     {
                        RTFDomShape shape = (RTFDomShape)GetLastElement(typeof(RTFDomShape));
                        if (shape != null)
                        {
                           shape.Left = reader.Parameter;
                        }
                        break;
                     }
                  case RTFConsts._shptop:
                     {
                        RTFDomShape shape = (RTFDomShape)GetLastElement(typeof(RTFDomShape));
                        if (shape != null)
                        {
                           shape.Top = reader.Parameter;
                        }
                        break;
                     }
                  case RTFConsts._shpright:
                     {
                        RTFDomShape shape = (RTFDomShape)GetLastElement(typeof(RTFDomShape));
                        if (shape != null)
                        {
                           shape.Width = reader.Parameter - shape.Left;
                        }
                        break;
                     }
                  case RTFConsts._shpbottom:
                     {
                        RTFDomShape shape = (RTFDomShape)GetLastElement(typeof(RTFDomShape));
                        if (shape != null)
                        {
                           shape.Height = reader.Parameter - shape.Top;
                        }
                        break;
                     }
                  case RTFConsts._shplid:
                     {
                        RTFDomShape shape = (RTFDomShape)GetLastElement(typeof(RTFDomShape));
                        if (shape != null)
                        {
                           shape.ShapeID = reader.Parameter;
                        }
                        break;
                     }
                  case RTFConsts._shpz:
                     {
                        RTFDomShape shape = (RTFDomShape)GetLastElement(typeof(RTFDomShape));
                        if (shape != null)
                        {
                           shape.ZIndex = reader.Parameter;
                        }
                        break;
                     }
                  case RTFConsts._shpgrp:
                     {
                        RTFDomShapeGroup group = new RTFDomShapeGroup();
                        group.NativeLevel = reader.Level;
                        AddContentElement(group);
                        break;
                     }
                  case RTFConsts._shpinst:
                     {
                        break;
                     }
                  #endregion

                  #region read table ************************************************
                  case RTFConsts._intbl:
                  case RTFConsts._trowd:
                  case RTFConsts._itap:
                     {
                        // these keyword said than current paragraph is table row
                        bolStartContent = true;
                        RTFDomElement[] es = GetLastElements(true);
                        RTFDomElement lastUnlockElement = null;
                        RTFDomElement lastTableElement = null;
                        for (int iCount = es.Length - 1; iCount >= 0; iCount--)
                        {
                           RTFDomElement e = es[iCount];
                           if (e.Locked == false)
                           {
                              if (lastUnlockElement == null && !(e is RTFDomParagraph))
                                 lastUnlockElement = e;

                              if (e is RTFDomTableRow or RTFDomTableCell)
                              {
                                 lastTableElement = e;
                                 break;
                              }
                           }
                        }
                        if (reader.Keyword == RTFConsts._intbl)
                        {
                           if (lastTableElement == null)
                           {
                              // if can not find unlocked row then new row
                              RTFDomTableRow row = new () { NativeLevel = reader.Level };
                              lastUnlockElement.AppendChild(row);
                           }
                        }
                        else if (reader.Keyword == RTFConsts._trowd)
                        {
                           // clear row format
                           RTFDomTableRow row = null;
                           if (lastTableElement == null)
                           {
                              row = new RTFDomTableRow { NativeLevel = reader.Level };
                              lastUnlockElement.AppendChild(row);
                           }
                           else
                           {
                              row = lastTableElement as RTFDomTableRow;
                              if (row == null)
                                 row = (RTFDomTableRow)lastTableElement.Parent;
                           }
                           row.Attributes.Clear();
                           row.CellSettings.Clear();
                           _ParagraphFormat.ResetParagraph();
                        }
                        else if (reader.Keyword == RTFConsts._itap)
                        {
                           // set nested level
                           RTFDomTableRow row = null;

                           if (reader.Parameter == 0)
                           {
                              // is the 0 level , belong to document , not to a table
                              //foreach (RTFDomElement element in es)
                              //{
                              //    if (element is RTFDomTableRow || element is RTFDomTableCell)
                              //    {
                              //        element.Locked = true;
                              //    }
                              //}
                           }
                           else
                           {
                              // in a row
                              if (lastTableElement == null)
                              {
                                 row = new RTFDomTableRow { NativeLevel = reader.Level };
                                 lastUnlockElement.AppendChild(row);
                              }
                              else
                              {
                                 row = lastTableElement as RTFDomTableRow;
                                 if (row == null)
                                    row = (RTFDomTableRow)lastTableElement.Parent;
                                 //row.Attributes.Clear();
                                 //row.CellSettings = new ArrayList();
                              }
                              if (reader.Parameter == row.Level)
                              {}
                              else if (reader.Parameter > row.Level)
                              {
                                 // nested row
                                 RTFDomTableRow newRow = new () { Level = reader.Parameter };
                                 RTFDomTableCell parentCell = (RTFDomTableCell)GetLastElement(typeof(RTFDomTableCell), false);
                                 if (parentCell == null)
                                    this.AddContentElement(newRow);
                                 else
                                    parentCell.AppendChild(newRow);
                              }
                              else if (reader.Parameter < row.Level)
                              {
                                 // exit nested row
                              }
                           }
                        }
                        break;
                     }
                  case RTFConsts._nesttableprops:
                     {
                        // ignore
                        break;
                     }
                  case RTFConsts._row:
                     {
                        // finish read row
                        bolStartContent = true;
                        RTFDomElement[] es = GetLastElements(true);
                        for (int iCount = es.Length - 1; iCount >= 0; iCount--)
                        {
                           es[iCount].Locked = true;
                           if (es[iCount] is RTFDomTableRow)
                           {
                              break;
                           }
                        }
                        break;
                     }
                  case RTFConsts._nestrow:
                     {
                        // finish nested row
                        bolStartContent = true;
                        RTFDomElement[] es = GetLastElements(true);
                        for (int iCount = es.Length - 1; iCount >= 0; iCount--)
                        {
                           es[iCount].Locked = true;
                           if (es[iCount] is RTFDomTableRow)
                           {
                              break;
                           }
                        }
                        break;
                     }
                  case RTFConsts._trrh:
                  case RTFConsts._trautofit:
                  case RTFConsts._irowband:
                  case RTFConsts._trhdr:
                  case RTFConsts._trkeep:
                  case RTFConsts._trkeepfollow:
                  case RTFConsts._trleft:
                  case RTFConsts._trqc:
                  case RTFConsts._trql:
                  case RTFConsts._trqr:
                  case RTFConsts._trcbpat:
                  case RTFConsts._trcfpat:
                  case RTFConsts._trpat:
                  case RTFConsts._trshdng:
                  case RTFConsts._trwWidth:
                  case RTFConsts._trwWidthA:
                  case RTFConsts._irow:
                  case RTFConsts._trpaddb:
                  case RTFConsts._trpaddl:
                  case RTFConsts._trpaddr:
                  case RTFConsts._trpaddt:
                  case RTFConsts._trpaddfb:
                  case RTFConsts._trpaddfl:
                  case RTFConsts._trpaddfr:
                  case RTFConsts._trpaddft:
                  case RTFConsts._lastrow:
                     {
                        // meet row control word , not parse at first , just save it 
                        bolStartContent = true;
                        RTFDomTableRow row = (RTFDomTableRow)GetLastElement(typeof(RTFDomTableRow), false);
                        if (row != null)
                        {
                           row.Attributes.Add(reader.Keyword, reader.Parameter);
                        }
                        break;
                     }
                  case RTFConsts._clvmgf:
                  case RTFConsts._clvmrg:
                  case RTFConsts._clmgf:
                  case RTFConsts._clmrg:
                  case RTFConsts._cellx:
                  case RTFConsts._clvertalt:
                  case RTFConsts._clvertalc:
                  case RTFConsts._clvertalb:
                  case RTFConsts._clNoWrap:
                  case RTFConsts._clcbpat:
                  case RTFConsts._clcfpat:
                  case RTFConsts._clpadl:
                  case RTFConsts._clpadt:
                  case RTFConsts._clpadr:
                  case RTFConsts._clpadb:
                  case RTFConsts._clbrdrl:
                  case RTFConsts._clbrdrt:
                  case RTFConsts._clbrdrr:
                  case RTFConsts._clbrdrb:
                  case RTFConsts._brdrtbl:
                  case RTFConsts._brdrnone:
                     {
                        // meet cell control word , no parse at first , just save it
                        bolStartContent = true;
                        RTFDomTableRow row = (RTFDomTableRow)GetLastElement(typeof(RTFDomTableRow), false);
                        //if (row != null && row.Locked == false )
                        {
                           RTFAttributeList style = null;
                           if (row.CellSettings.Count > 0)
                           {
                              style = (RTFAttributeList)row.CellSettings[row.CellSettings.Count - 1];
                              if (style.Contains(RTFConsts._cellx))
                              {
                                 // if find repeat control word , then can consider this control word
                                 // belong to the next cell . userly cellx is the last control word of 
                                 // a cell , when meet cellx , the current cell defind is finished.
                                 style = new RTFAttributeList();
                                 row.CellSettings.Add(style);
                              }
                           }
                           if (style == null)
                           {
                              style = new RTFAttributeList();
                              row.CellSettings.Add(style);
                           }
                           style.Add(reader.Keyword, reader.Parameter);

                        }
                        break;
                     }
                  case RTFConsts._cell:
                     {
                        // finish cell content
                        bolStartContent = true;
                        this.AddContentElement(null);
                        this.CompleteParagraph();
                        _ParagraphFormat.Reset();
                        format.Reset();
                        RTFDomElement[] es = GetLastElements(true);
                        for (int iCount = es.Length - 1; iCount >= 0; iCount--)
                        {
                           if (es[iCount].Locked == false)
                           {
                              es[iCount].Locked = true;
                              if (es[iCount] is RTFDomTableCell)
                              {
                                 //((RTFDomTableCell)es[iCount]).Format = format.Clone();
                                 break;
                              }
                           }
                        }
                        break;
                     }
                  case RTFConsts._nestcell:
                     {
                        // finish nested cell content
                        bolStartContent = true;
                        AddContentElement(null);
                        this.CompleteParagraph();
                        RTFDomElement[] es = GetLastElements(false);
                        for (int iCount = es.Length - 1; iCount >= 0; iCount--)
                        {
                           es[iCount].Locked = true;
                           if (es[iCount] is RTFDomTableCell)
                           {
                              ((RTFDomTableCell)es[iCount]).Format = format;
                              break;
                           }
                        }
                        break;
                     }
                  #endregion
                  default:
                     // unsupport keyword
                     if (reader.TokenType == RTFTokenType.ExtKeyword
                         && reader.FirstTokenInGroup)
                     {
                        // if meet unsupport extern keyword , and this token is the first token in 
                        // current group , then ingore whole group.
                        ReadToEndGround(reader);
                        break;
                     }
                     break;
               }//switch
            }

         }//while
         if (myText.HasContent)
         {
            ApplyText(myText, reader, format);
         }
      }

   }
}
