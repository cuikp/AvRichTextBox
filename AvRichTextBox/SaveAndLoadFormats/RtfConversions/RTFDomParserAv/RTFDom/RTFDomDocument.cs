/*
 * 
 *  RTFDomParserAv, modified from original:
 *   DCSoft RTF DOM v1.0
 *   Author : Yuan yong fu.
 *   Email  : yyf9989@hotmail.com
 *   blog site:http://www.cnblogs.com/xdesigner.
 * 
 */

using System.Collections;
using System.Text;
using System.ComponentModel;
using Avalonia.Media;

namespace RtfDomParserAv
{
    /// <summary>
    /// RTF Document
    /// </summary>
    /// <remarks>
    /// This type is the root of RTF Dom tree structure
    /// </remarks>
    public partial class RTFDomDocument : RTFDomElement
    {
        static RTFDomDocument() => Defaults.LoadEncodings();

        public RTFDomDocument()
        {
            this.OwnerDocument = this;
        }

        private string strFollowingChars = null!;
        /// <summary>
        /// following characters
        /// </summary>
        [System.ComponentModel.DefaultValue(null)]
        public string FollowingChars
        {
            get => strFollowingChars;
            set => strFollowingChars = value;
        }

        private string strLeadingChars = null!;
        /// <summary>
        /// leading characters
        /// </summary>
        [System.ComponentModel.DefaultValue(null)]
        public string LeadingChars
        {
            get => strLeadingChars;
            set => strLeadingChars = value;
        }

        private Encoding myDefaultEncoding = System.Text.Encoding.Default;
        /// <summary>
        /// text encoding of current font
        /// </summary>
        private Encoding myFontChartset = null!;
        /// <summary>
        /// text encoding of associate font 
        /// </summary>
        private Encoding myAssociateFontChartset = null!;
        /// <summary>
        /// text encoding
        /// </summary>
        internal Encoding RuntimeEncoding
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
                return myDefaultEncoding;
            }
        }

        /// <summary>
        /// default font name
        /// </summary>
        private static string DefaultFontName = Defaults.FontName;


        private RTFFontTable myFontTable = [];
        public RTFFontTable FontTable
        {
            get => myFontTable;
            set => myFontTable = value;
        }

        private RTFColorTable myColorTable = new();
        public RTFColorTable ColorTable
        {
            get => myColorTable;
            set => myColorTable = value;
        }

        public RTFListTable ListTable { get; set; } = [];

        public RTFListOverrideTable ListOverrideTable { get; set; } = [];

        private RTFDocumentInfo myInfo = new();
        public RTFDocumentInfo Info
        {
            get => myInfo;
            set => myInfo = value;
        }

        private string strGenerator = null!;
        /// <summary>
        /// document generator
        /// </summary>
        [DefaultValue(null)]
        public string Generator
        {
            get => strGenerator;
            set => strGenerator = value;
        }

        private int intPaperWidth = 12240;
        /// <summary>
        /// paper width,unit twips
        /// </summary>
        [DefaultValue(12240)]
        public int PaperWidth
        {
            get => intPaperWidth;
            set => intPaperWidth = value;
        }

        private int intPaperHeight = 15840;
        /// <summary>
        /// paper height,unit twips
        /// </summary>
        [DefaultValue(15840)]
        public int PaperHeight
        {
            get => intPaperHeight;
            set => intPaperHeight = value;
        }

        private int intLeftMargin = 1800;
        /// <summary>
        /// left margin,unit twips
        /// </summary>
        [DefaultValue(1800)]
        public int LeftMargin
        {
            get => intLeftMargin;
            set => intLeftMargin = value;
        }

        private int intTopMargin = 1440;
        /// <summary>
        /// top margin,unit twips
        /// </summary>
        [DefaultValue(1440)]
        public int TopMargin
        {
            get => intTopMargin;
            set => intTopMargin = value;
        }

        private int intRightMargin = 1800;
        /// <summary>
        /// right margin,unit twips
        /// </summary>
        [System.ComponentModel.DefaultValue(1800)]
        public int RightMargin
        {
            get => intRightMargin;
            set => intRightMargin = value;
        }

        private int intBottomMargin = 1440;
        /// <summary>
        /// bottom margin,unit twips
        /// </summary>
        [System.ComponentModel.DefaultValue(1440)]
        public int BottomMargin
        {
            get => intBottomMargin;
            set => intBottomMargin = value;
        }

        private bool bolLandscape = false;
        /// <summary>
        /// landscape
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool Landscape
        {
            get => bolLandscape;
            set => bolLandscape = value;
        }

        private int _HeaderDistance = 720;
        /// <summary>
        /// Header's distance from the top of the page( Twips)
        /// </summary>
        [System.ComponentModel.DefaultValue(720)]
        public int HeaderDistance
        {
            get => _HeaderDistance;
            set => _HeaderDistance = value;
        }

        /// <summary>
        /// Footer's distance from the bottom of the page( twips)
        /// </summary>
        [System.ComponentModel.DefaultValue(720)]
        public int FooterDistance { get; set; } = 720;

        /// <summary>
        /// client area width,unit twips
        /// </summary>
        [Browsable(false)]
        public int ClientWidth
        {
            get
            {
                if (bolLandscape)
                {
                    return intPaperHeight - intLeftMargin - intRightMargin;
                }
                else
                {
                    return intPaperWidth - intLeftMargin - intRightMargin;
                }
            }
        }

        private bool bolChangeTimesNewRoman = false;
        /// <summary>
        /// convert "Times new roman" to default font when parse rtf content
        /// </summary>
        [DefaultValue(true)]
        public bool ChangeTimesNewRoman
        {
            get=> bolChangeTimesNewRoman;
            set => bolChangeTimesNewRoman = value;
        }
        
        //private Stack myElements = new Stack();

        public event ProgressEventHandler Progress = null!;

        /// <summary>
        /// raise progress event
        /// </summary>
        /// <param name="max">progress max value</param>
        /// <param name="Value">progress value</param>
        /// <param name="message">progress message</param>
        /// <returns>user cancel</returns>
        protected bool OnProgress(int max, int Value, string message)
        {
            if (Progress != null)
            {
                ProgressEventArgs args = new(max, Value, message);
                Progress(this, args);
                return args.Cancel;
            }
            return false;
        }


        public void Load(string fileName)
        {
            using FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);
            Load(stream);
        }

        public void Load(Stream stream)
        {
            //_HtmlContentBuilder = new StringBuilder();
            //_RTFHtmlState = true ;
            _HtmlContent = null!;
            this.Elements.Clear();
            bolStartContent = false;
            RTFReader reader = new(stream);
            DocumentFormatInfo format = new();
            _ParagraphFormat = null!;
            Load(reader, format);
            // combination table rows to table
            CombinTable(this);
            FixElements(this);
            FixRTFHtml();
        }


        /// <summary>
        /// load a rtf document from a text reader and parse content
        /// </summary>
        /// <param name="reader">text reader</param>
        public void Load(TextReader reader)
        {
            //_HtmlContentBuilder = new StringBuilder();
            //_RTFHtmlState = true;
            _HtmlContent = null!;
            this.Elements.Clear();
            bolStartContent = false;
            RTFReader r = new(reader);
            DocumentFormatInfo format = new();
            _ParagraphFormat = null!;
            Load(r, format);
            // combination table rows to table
            CombinTable(this);
            FixElements(this);
            FixRTFHtml();
        }

        /// <summary>
        /// load a rtf document from a string in rtf format and parse content
        /// </summary>
        /// <param name="rtfText">text</param>
        public void LoadRTFText(string rtfText)
        {
            System.IO.StringReader reader = new(rtfText);
            //_HtmlContentBuilder = new StringBuilder();
            //_RTFHtmlState = true;
            _HtmlContent = null!;
            this.Elements.Clear();
            bolStartContent = false;
            RTFReader rtfReader = new(reader);
            DocumentFormatInfo format = new();
            _ParagraphFormat = null!;
            Load(rtfReader, format);
            CombinTable(this);
            FixElements(this);
            FixRTFHtml();
        }

        private void FixRTFHtml()
        {
            //if (this._HtmlContentBuilder != null)
            //{
            //    this._HtmlContent = this._HtmlContentBuilder.ToString();
            //    this._HtmlContentBuilder = null;
            //}
            //this._RTFHtmlState = false;
        }

        /// <summary>
        /// 将文档中所有的内容都放置在段落中。
        /// </summary>
        public void FixForParagraphs(RTFDomElement parentElement)
        {
            RTFDomParagraph lastParagraph = null!;
            RTFDomElementList list = [];
            foreach (RTFDomElement element in parentElement.Elements)
            {
                if (element is RTFDomHeader or RTFDomFooter)
                {
                    FixForParagraphs(element);
                    lastParagraph = null!;
                    list.Add(element);
                    continue;
                }
                if (element is RTFDomParagraph or RTFDomTableRow or RTFDomTable or RTFDomTableCell)
                {
                    lastParagraph = null!;
                    list.Add(element);
                    continue;
                }
                if (lastParagraph == null)
                {
                    lastParagraph = new RTFDomParagraph();
                    list.Add(lastParagraph);
                    if (element is RTFDomText)
                    {
                        lastParagraph.Format = ((RTFDomText)element).Format.Clone();
                    }
                }
                lastParagraph.Elements.Add(element);
            }//foreach
            parentElement.Elements.Clear();
            foreach (RTFDomElement element in list)
            {
                //if (element is RTFDomHeader
                //    || element is RTFDomFooter)
                //{
                //    FixForParagraphs(element);
                //}
                parentElement.Elements.Add(element);
            }
        }

        private void FixElements(RTFDomElement parentElement)
        {
            // combin text element , decrease number of RTFDomText instance
            ArrayList result = [];
            foreach (RTFDomElement element in parentElement.Elements)
            {
                if (element is RTFDomParagraph)
                {
                    RTFDomParagraph p = (RTFDomParagraph)element;
                    if (p.Format.PageBreak)
                    {
                        p.Format.PageBreak = false;
                        result.Add(new RTFDomPageBreak());
                    }
                }
                if (element is RTFDomText)
                {
                    if (result.Count > 0 && result[result.Count - 1] is RTFDomText)
                    {
                        RTFDomText lastText = (RTFDomText)result[result.Count - 1]!;
                        RTFDomText txt = (RTFDomText)element;
                        if (lastText.Text.Length == 0 || txt.Text.Length == 0)
                        {
                            if (lastText.Text.Length == 0)
                            {   // clone text format
                                lastText.Format = txt.Format.Clone();
                            }
                            lastText.Text += txt.Text;
                        }
                        else
                        {
                            if (lastText.Format.EqualsSettings(txt.Format))
                                lastText.Text += txt.Text;
                            else
                                result.Add(txt);
                        }
                    }
                    else
                        result.Add(element);
                }
                else
                    result.Add(element);

            }//foreach
            parentElement.Elements.Clear();
            parentElement.Locked = false;

            foreach (RTFDomElement element in result)
                parentElement.AppendChild(element);

            foreach (RTFDomElement element in parentElement.Elements.ToArray())
            {
                if (element is RTFDomTable table)
                    UpdateTableCells(table, true);
            }


            //// 删除临时生成的锻炼对象，将段落整体往上代爷移动一位
            //RTFDomParagraph tempP = null;
            //RTFDomParagraph lastP = null;
            //foreach (RTFDomElement element in parentElement.Elements)
            //{
            //    if (element is RTFDomParagraph)
            //    {
            //        RTFDomParagraph p = (RTFDomParagraph)element;
            //        if (p.TemplateGenerated)
            //        {
            //            tempP = p;
            //        }
            //        else
            //        {
            //            if (tempP != null)
            //            {
            //                tempP.TemplateGenerated = false;
            //                tempP.Format = p.Format;
            //                tempP = p;
            //            }
            //        }
            //        lastP = p;
            //    }
            //}//foreach
            //if (tempP != null && lastP != null)
            //{
            //    // 若发生锻炼向上代昊移动，而且煮一个段落为空，则删除煮一个段落。
            //    if (lastP.Elements.Count == 0)
            //    {
            //        parentElement.Elements.Remove(lastP);
            //    }
            //}

            foreach (RTFDomElement element in parentElement.Elements)
                FixElements(element);
        }

        private RTFDomElement[] GetLastElements(bool checkLockState)
        {
            List<RTFDomElement> result = [];
            RTFDomElement element = this;
            while (element != null)
            {
                if (checkLockState)
                {
                    if (element.Locked)
                        break;
                }
                result.Add(element);
                element = element.Elements.LastElement;
            }
            if (checkLockState)
            {
                for (int iCount = result.Count - 1; iCount >= 0; iCount--)
                {
                    if (result[iCount].Locked)
                        result.RemoveAt(iCount);
                }
            }
            return [.. result];
        }

        public RTFDomElement GetLastElement(Type elementType)
        {
            RTFDomElement[] elements = GetLastElements(true);
            for (int iCount = elements.Length - 1; iCount >= 0; iCount--)
            {
                if (elementType.IsInstanceOfType(elements[iCount]))
                    return elements[iCount];
            }
            return null!;
        }

        public RTFDomElement GetLastElement(Type elementType, bool lockStatus)
        {
            RTFDomElement[] elements = GetLastElements(true);
            for (int iCount = elements.Length - 1; iCount >= 0; iCount--)
            {
                if (elementType.IsInstanceOfType(elements[iCount]))
                {
                    if (elements[iCount].Locked == lockStatus)
                        return elements[iCount];
                }
            }
            return null!;
        }

        public RTFDomElement GetLastElement()
        {
            RTFDomElement[] elements = GetLastElements(true);
            return elements[^1];
        }

        private void CompleteParagraph()
        {
            RTFDomElement lastElement = GetLastElement();
            while (lastElement != null)
            {
                if (lastElement is RTFDomParagraph)
                {
                    RTFDomParagraph p = (RTFDomParagraph)lastElement;
                    p.Locked = true;
                    if (_ParagraphFormat != null)
                    {
                        p.Format = _ParagraphFormat;
                        _ParagraphFormat = _ParagraphFormat.Clone();
                    }
                    else
                        _ParagraphFormat = new DocumentFormatInfo();

                    break;
                }
                lastElement = lastElement.Parent;
            }
        }

        private void AddContentElement(RTFDomElement newElement)
        {
            RTFDomElement[] elements = GetLastElements(true);
            RTFDomElement lastElement = null!;
            if (elements.Length > 0)
            {
                lastElement = elements[elements.Length - 1];
            }
            if (lastElement is RTFDomDocument or RTFDomHeader or RTFDomFooter)
            {
                if (newElement is RTFDomText
                    or RTFDomImage
                    or RTFDomObject
                    or RTFDomShape
                    or RTFDomShapeGroup
                    or RTFDomField // added to catch fields, etc. at start
                    or RTFDomBookmark
                    or RTFDomLineBreak)
                {
                    RTFDomParagraph p = new();
                    if (lastElement.Elements.Count > 0)
                        p.TemplateGenerated = true;

                    if (_ParagraphFormat != null)
                        p.Format = _ParagraphFormat;

                    lastElement.AppendChild(p);
                    p.Elements.Add(newElement);
                    return;
                }
            }
            RTFDomElement element = elements[^1];
            //if ( newElement is RTFDomTableRow)
            //{
            //    System.Diagnostics.Debugger.Break();
            //}
            if (newElement != null && newElement.NativeLevel > 0)
            {
                for (int iCount = elements.Length - 1; iCount >= 0; iCount--)
                {
                    if (elements[iCount].NativeLevel == newElement.NativeLevel)
                    {
                        for (int iCount2 = iCount; iCount2 < elements.Length; iCount2++)
                        {
                            RTFDomElement e2 = elements[iCount2];
                            //if (newElement.GetType().Equals(e2.GetType()))
                            //{
                            //}
                            if (newElement is RTFDomText
                                or RTFDomImage
                                or RTFDomObject
                                or RTFDomShape
                                or RTFDomShapeGroup
                                or RTFDomField
                                or RTFDomBookmark
                                or RTFDomLineBreak)
                            {
                                if (newElement.NativeLevel == e2.NativeLevel)
                                {
                                    if (e2 is RTFDomTableRow or RTFDomTableCell or RTFDomField or RTFDomParagraph)
                                        continue;
                                }
                            }

                            elements[iCount2].Locked = true;
                        }
                        break;
                    }
                }
            }

            for (int iCount = elements.Length - 1; iCount >= 0; iCount--)
            {
                if (elements[iCount].Locked == false)
                {
                    element = elements[iCount];
                    if (element is RTFDomImage)
                        element.Locked = true;
                    else
                        break;
                }
            }
            if (element is RTFDomTableRow)
            {     // If the last element is table row can not contains any element, so need create a cell element.
                RTFDomTableCell cell = new() { NativeLevel = element.NativeLevel };
                element.AppendChild(cell);
                if (newElement is RTFDomTableRow)
                    cell.Elements.Add(newElement);
                else
                {
                    RTFDomParagraph cellP = new()
                    {
                        Format = _ParagraphFormat.Clone(),
                        NativeLevel = cell.NativeLevel
                    };

                    cell.AppendChild(cellP);

                    if (newElement != null)
                        cellP.AppendChild(newElement);
                }
            }
            else
            {
                if (newElement != null)
                {
                    if (element is RTFDomParagraph &&
                        (newElement is RTFDomParagraph
                        || newElement is RTFDomTableRow))
                    {
                        // If both is paragraph , append new paragraph to the parent of old paragraph
                        element.Locked = true;
                        element.Parent.AppendChild(newElement);
                    }
                    else
                        element.AppendChild(newElement);
                }
            }
        }


        private int ListTextFlag = 0;
        private bool bolStartContent = false;

        /// <summary>
        /// convert a hex string to a byte array
        /// </summary>
        /// <param name="hex">hex string</param>
        /// <returns>byte array</returns>
        private byte[] HexToBytes(string hex)
        {
            string chars = "0123456789abcdef";

            int index = 0;
            int Value = 0;
            int CharCount = 0;
            ByteBuffer buffer = new();
            for (int iCount = 0; iCount < hex.Length; iCount++)
            {
                char c = hex[iCount];
                c = char.ToLower(c);
                index = chars.IndexOf(c);
                if (index >= 0)
                {
                    CharCount++;
                    Value = Value * 16 + index;
                    if (CharCount > 0 && (CharCount % 2) == 0)
                    {
                        buffer.Add((byte)Value);
                        Value = 0;
                    }
                }
            }
            return buffer.ToArray();
        }

        /// <summary>
        /// 将柄择行元素合并成柄择元素
        /// </summary>
        /// <param name="parentElement">父元素对蟻E/param>
        private void CombinTable(RTFDomElement parentElement)
        {
            ArrayList result = [];
            ArrayList rows = [];
            int lastRowWidth = -1;
            RTFDomTableRow lastRow = null!;
            foreach (RTFDomElement element in parentElement.Elements)
            {
                if (element is RTFDomTableRow)
                {
                    RTFDomTableRow row = (RTFDomTableRow)element;
                    row.Locked = false;
                    ArrayList cellSettings = row.CellSettings;
                    if (cellSettings.Count == 0)
                    {
                        if (lastRow != null && lastRow.CellSettings.Count == row.Elements.Count)
                        {
                            cellSettings = lastRow.CellSettings;
                        }
                    }
                    if (cellSettings.Count == row.Elements.Count)
                    {
                        for (int iCount = 0; iCount < row.Elements.Count; iCount++)
                        {
                            row.Elements[iCount].Attributes = (RTFAttributeList)cellSettings[iCount]!;
                        }
                    }
                    bool isLastRow = row.HasAttribute(RTFConsts._lastrow);
                    if (isLastRow == false)
                    {
                        int index = parentElement.Elements.IndexOf(element);
                        if (index == parentElement.Elements.Count - 1)
                        {
                            // this element is the last element
                            // then this row is the last row
                            isLastRow = true;
                        }
                        else
                        {
                            RTFDomElement e2 = parentElement.Elements[index + 1];
                            if (!(e2 is RTFDomTableRow))
                            {
                                // next element is not row 
                                isLastRow = true;
                            }
                        }
                    }
                    // split to table
                    if (isLastRow)
                    {
                        // if current row mark the last row , then 
                        // generate a new table
                        rows.Add(row);
                        result.Add(CreateTable(rows));
                        lastRowWidth = -1;
                    }
                    else
                    {
                        int width = 0;
                        if (row.HasAttribute(RTFConsts._trwWidth))
                        {
                            width = row.Attributes[RTFConsts._trwWidth];
                            if (row.HasAttribute(RTFConsts._trwWidthA))
                            {
                                width = width - row.Attributes[RTFConsts._trwWidthA];
                            }
                        }
                        else
                        {
                            foreach (RTFDomTableCell cell in row.Elements)
                            {
                                if (cell.HasAttribute(RTFConsts._cellx))
                                    width = Math.Max(width, cell.Attributes[RTFConsts._cellx]);
                            }
                        }
                        if (lastRowWidth > 0 && lastRowWidth != width)
                        {
                            // If row's width is change , then can consider multi-table combin
                            // then split and generate new table
                            if (rows.Count > 0)
                                result.Add(CreateTable(rows));
                        }
                        lastRowWidth = width;
                        rows.Add(row);
                    }
                    lastRow = row;
                }
                else if (element is RTFDomTableCell)
                {
                    lastRow = null!;
                    CombinTable(element);

                    if (rows.Count > 0)
                        result.Add(CreateTable(rows));

                    result.Add(element);
                    lastRowWidth = -1;
                }
                else
                {
                    lastRow = null!;
                    CombinTable(element);

                    if (rows.Count > 0)
                        result.Add(CreateTable(rows));

                    result.Add(element);
                    lastRowWidth = -1;
                }
            }//foreach

            if (rows.Count > 0)
                result.Add(CreateTable(rows));

            parentElement.Locked = false;
            parentElement.Elements.Clear();
            foreach (RTFDomElement element in result)
            {
                parentElement.AppendChild(element);
            }

        }
        /// <summary>
        /// create table
        /// </summary>
        /// <param name="rows">table rows</param>
        /// <returns>new table</returns>
        private RTFDomTable CreateTable(ArrayList rows)
        {
            if (rows.Count > 0)
            {
                RTFDomTable table = new();
                int index = 0;
                foreach (RTFDomTableRow row in rows)
                {
                    row.RowIndex = index;
                    index++;
                    table.AppendChild(row);
                }
                rows.Clear();
                foreach (RTFDomTableRow row in table.Elements)
                {
                    foreach (RTFDomTableCell cell in row.Elements)
                        CombinTable(cell);
                }
                return table;
            }
            else
                throw new ArgumentException(null, nameof(rows));
        }

        private int intDefaultRowHeight = 400;
        /// <summary>
        /// default row's height, in twips.
        /// </summary>
        public int DefaultRowHeight 
        {
            get => intDefaultRowHeight;
            set => intDefaultRowHeight = value;
        }

        private void UpdateTableCells(RTFDomTable table, bool fixTableCellSize)
        {
            // number of table column
            int columns = 0;
            // flag of cell merge
            bool merge = false;
            // right position of all cells
            ArrayList rights = [];

            // right position of table
            int tableLeft = 0;
            for (int iCount = table.Elements.Count - 1; iCount >= 0; iCount--)
            {
                RTFDomTableRow row = (RTFDomTableRow)table.Elements[iCount];
                if (row.Elements.Count == 0)
                    table.Elements.RemoveAt(iCount);
            }

            if (table.Elements.Count == 0)
                Debug.WriteLine("");

            foreach (RTFDomTableRow row in table.Elements)
            {
                int lastCellX = 0;

                columns = Math.Max(columns, row.Elements.Count);
                if (row.HasAttribute(RTFConsts._irow))
                {
                    row.RowIndex = row.Attributes[RTFConsts._irow];
                }
                row.IsLastRow = row.HasAttribute(RTFConsts._lastrow);
                row.Header = row.HasAttribute(RTFConsts._trhdr);
                // read row height
                if (row.HasAttribute(RTFConsts._trrh))
                {
                    row.Height = row.Attributes[RTFConsts._trrh];
                    if (row.Height == 0)
                        row.Height = this.DefaultRowHeight;
                    else if (row.Height < 0)
                        row.Height = -row.Height;
                }
                else
                {
                    row.Height = this.DefaultRowHeight;
                }
                // read default padding of cell
                if (row.HasAttribute(RTFConsts._trpaddl))
                    row.PaddingLeft = row.Attributes[RTFConsts._trpaddl];
                else
                    row.PaddingLeft = int.MinValue;

                if (row.HasAttribute(RTFConsts._trpaddt))
                    row.PaddingTop = row.Attributes[RTFConsts._trpaddt];
                else
                    row.PaddingTop = int.MinValue;

                if (row.HasAttribute(RTFConsts._trpaddr))
                    row.PaddingRight = row.Attributes[RTFConsts._trpaddr];
                else
                    row.PaddingRight = int.MinValue;

                if (row.HasAttribute(RTFConsts._trpaddb))
                    row.PaddingBottom = row.Attributes[RTFConsts._trpaddb];
                else
                    row.PaddingBottom = int.MinValue;

                if (row.HasAttribute(RTFConsts._trleft))
                {
                    tableLeft = row.Attributes[RTFConsts._trleft];
                }
                if (row.HasAttribute(RTFConsts._trcbpat))
                {
                    row.Format.BackColor = this.ColorTable.GetColor(
                        row.Attributes[RTFConsts._trcbpat],
                        Colors.Transparent);
                }

                int widthCount = 0;
                foreach (RTFDomTableCell cell in row.Elements)
                {
                    // set merge if present 
                    if (cell.HasAttribute(RTFConsts._clvmgf) || cell.HasAttribute(RTFConsts._clvmrg) || cell.HasAttribute(RTFConsts._clmgf) || cell.HasAttribute(RTFConsts._clmrg))
                        merge = true;

                    if (cell.HasAttribute(RTFConsts._clpadl))
                        cell.PaddingLeft = cell.Attributes[RTFConsts._clpadl];
                    else
                        cell.PaddingLeft = int.MinValue;

                    if (cell.HasAttribute(RTFConsts._clpadr))
                        cell.PaddingRight = cell.Attributes[RTFConsts._clpadr];
                    else
                        cell.PaddingRight = int.MinValue;

                    if (cell.HasAttribute(RTFConsts._clpadt))
                        cell.PaddingTop = cell.Attributes[RTFConsts._clpadt];
                    else
                        cell.PaddingTop = int.MinValue;

                    if (cell.HasAttribute(RTFConsts._clpadb))
                        cell.PaddingBottom = cell.Attributes[RTFConsts._clpadb];
                    else
                        cell.PaddingBottom = int.MinValue;

                    // whether display border line
                    cell.Format.LeftBorder = cell.HasAttribute(RTFConsts._clbrdrl);
                    cell.Format.TopBorder = cell.HasAttribute(RTFConsts._clbrdrt);
                    cell.Format.RightBorder = cell.HasAttribute(RTFConsts._clbrdrr);
                    cell.Format.BottomBorder = cell.HasAttribute(RTFConsts._clbrdrb);
                    if (cell.HasAttribute(RTFConsts._brdrcf))
                        cell.Format.BorderColor = this.ColorTable.GetColor(cell.GetAttributeValue(RTFConsts._brdrcf, 1), Colors.Black);

                    for (int iCount = cell.Attributes.Count - 1; iCount >= 0; iCount--)
                    {
                        string name3 = cell.Attributes.GetItem(iCount).Name;
                        if (name3 is RTFConsts._brdrtbl or RTFConsts._brdrnone or RTFConsts._brdrnil)
                        {
                            for (int iCount2 = iCount - 1; iCount2 >= 0; iCount2--)
                            {
                                string name2 = cell.Attributes.GetItem(iCount2).Name;
                                if (name2 == RTFConsts._clbrdrl) { cell.Format.LeftBorder = false; break; }
                                else if (name2 == RTFConsts._clbrdrt) { cell.Format.TopBorder = false; break; }
                                else if (name2 == RTFConsts._clbrdrr) { cell.Format.RightBorder = false; break; }
                                else if (name2 == RTFConsts._clbrdrb) { cell.Format.BottomBorder = false; break; }
                            }
                        }
                    }
                    // vertical alignment
                    if (cell.HasAttribute(RTFConsts._clvertalt))
                        cell.VerticalAlignment = RTFVerticalAlignment.Top;
                    else if (cell.HasAttribute(RTFConsts._clvertalc))
                        cell.VerticalAlignment = RTFVerticalAlignment.Middle;
                    else if (cell.HasAttribute(RTFConsts._clvertalb))
                        cell.VerticalAlignment = RTFVerticalAlignment.Bottom;

                    // background color
                    if (cell.HasAttribute(RTFConsts._clcbpat))
                        cell.Format.BackColor = this.ColorTable.GetColor(cell.Attributes[RTFConsts._clcbpat], Colors.Transparent);
                    else
                        cell.Format.BackColor = Colors.Transparent;

                    if (cell.HasAttribute(RTFConsts._clcfpat))
                        cell.Format.BorderColor = this.ColorTable.GetColor(cell.Attributes[RTFConsts._clcfpat], Colors.Black);

                    // cell's width
                    int cellWidth = 2763;// cell's default with is 2763 Twips(570 Document)
                    if (cell.HasAttribute(RTFConsts._cellx))
                    {
                        cellWidth = cell.Attributes[RTFConsts._cellx] - lastCellX;
                        if (cellWidth < 100)
                            cellWidth = 100;
                    }
                    int right = lastCellX + cellWidth;
                    // fix cell's right position , if this position is very near with another cell's right position( less then 45 twips or 3 pixel), then consider these two position
                    // is the same , this can decrease number of table columns
                    for (int iCount = 0; iCount < rights.Count; iCount++)
                    {
                        if (Math.Abs(right - (int)rights[iCount]!) < 45)
                        {
                            right = (int)rights[iCount]!;
                            cellWidth = right - lastCellX;
                            break;
                        }
                    }

                    cell.Left = lastCellX;
                    cell.Width = cellWidth;
                    if (cell.HasAttribute(RTFConsts._cellx) == false)
                    {

                    }
                    widthCount += cellWidth;
                    //int right = cell.Left + cell.Width;
                    if (rights.Contains(right) == false)
                    {  // because of convert twips to unit of document may cause truncation error.  This may cause rights.Contains mistake . so scale cell's with with 
                       // native twips unit , after all computing , convert to unit of document.
                        rights.Add(right);
                    }
                    lastCellX += cellWidth;
                }
                row.Width = widthCount;
            }

            if (rights.Count == 0)
            { // can not detect cell's width , so consider set cell's width automatic, then set cell's default width
                int cols = 1;
                foreach (RTFDomTableRow row in table.Elements)
                    cols = Math.Max(cols, row.Elements.Count);

                int w = (int)(this.ClientWidth / cols);

                for (int iCount = 0; iCount < cols; iCount++)
                    rights.Add(iCount * w + w);
            }
            // computing cell's rowspan and colspan , number of rights array is the number of table columns.

            rights.Add(0);
            rights.Sort();
            // add table column instance
            for (int iCount = 1; iCount < rights.Count; iCount++)
            {
                RTFDomTableColumn col = new();
                col.Width = (int)rights[iCount]! - (int)rights[iCount - 1]!;
                table.Columns.Add(col);
            }

            for (int rowIndex = 1; rowIndex < table.Elements.Count; rowIndex++)
            {
                RTFDomTableRow row = (RTFDomTableRow)table.Elements[rowIndex];
                for (int colIndex = 0; colIndex < row.Elements.Count; colIndex++)
                {
                    RTFDomTableCell cell = (RTFDomTableCell)row.Elements[colIndex];
                    if (cell.Width == 0)
                    {
                        // If current cell not special width, then use the width of cell in the same column and in the last row
                        RTFDomTableRow preRow = (RTFDomTableRow)table.Elements[rowIndex - 1];
                        if (preRow.Elements.Count > colIndex)
                        {
                            RTFDomTableCell preCell = (RTFDomTableCell)preRow.Elements[colIndex];
                            cell.Left = preCell.Left;
                            cell.Width = preCell.Width;
                            CopyStyleAttribute(cell, preCell.Attributes);
                        }
                    }
                }
            }
            if (merge == false)
            {  // If not detect cell merge , maybe exist cell merge in the same row
                foreach (RTFDomTableRow row in table.Elements)
                {
                    if (row.Elements.Count < table.Columns.Count)
                    {  // if number of row's cells not equals the number of table's columns then cell merge exists
                        merge = true;
                        break;
                    }
                }
            }
            if (merge)
            { // detect cell merge, begin merge operation: In rtf format, cell merged by another cell in the same row has no rtf text, so delay create those cell instance
                foreach (RTFDomTableRow row in table.Elements)
                {
                    if (row.Elements.Count != table.Columns.Count)
                    {  // If number of row's cells not equals number of table's columns, then consider possibility of horizontal merge
                        RTFDomElement[] cells = row.Elements.ToArray();
                        foreach (RTFDomTableCell cell in cells)
                        {
                            int index = rights.IndexOf(cell.Left);
                            int index2 = rights.IndexOf(cell.Left + cell.Width);
                            int intColSpan = index2 - index;

                            // detect horizontal merge
                            if (cell.HasAttribute(RTFConsts._clmgf))
                            {
                                int cellIndex = Array.IndexOf(cells, cell);
                                intColSpan = 1;

                                for (int i = cellIndex + 1; i < cells.Length; i++)
                                {
                                    if ((RTFDomTableCell)cells[i] is RTFDomTableCell nextCell)
                                    {
                                        if (nextCell.HasAttribute(RTFConsts._clmrg))
                                            intColSpan++;
                                        else
                                            break;
                                    }
                                }
                            }

                            // detect vertical merge
                            bool verticalMerge = cell.HasAttribute(RTFConsts._clvmrg);
                            if (verticalMerge == false)
                            {
                                // If this cell does not merged by another cell above, then set colspan
                                cell.ColSpan = intColSpan;
                            }
                            if (row.Elements.LastElement == cell)
                            {
                                cell.ColSpan = table.Columns.Count - row.Elements.Count + 1;
                                intColSpan = cell.ColSpan;
                            }
                            for (int iCount = 0; iCount < intColSpan - 1; iCount++)
                            {
                                RTFDomTableCell newCell = new() { Attributes = cell.Attributes.Clone() };
                                row.Elements.Insert(row.Elements.IndexOf(cell) + 1, newCell);
                                if (verticalMerge)
                                { // This cell has been merged.
                                    newCell.Attributes[RTFConsts._clvmrg] = 1;
                                    newCell.OverrideCell = cell;
                                }
                            }
                        }
                        if (row.Elements.Count != table.Columns.Count)
                        {
                            // If the last cell has been merged. then supply new cells.
                            RTFDomTableCell lastCell = (RTFDomTableCell)row.Elements.LastElement;
                            if (lastCell == null)
                                Debug.WriteLine("");

                            //if (lastCell.OverrideCell == null && lastCell.ColSpan > 1)
                            //{
                            //   lastCell.ColSpan = table.Columns.Count - row.Elements.IndexOf(lastCell);
                            //}
                            for (int iCount = row.Elements.Count; iCount < rights.Count; iCount++)
                            {
                                RTFDomTableCell newCell = new();
                                CopyStyleAttribute(newCell, lastCell!.Attributes);
                                row.Elements.Add(newCell);
                            }
                        }
                    }
                }

                // set cell's merge
                foreach (RTFDomTableRow row in table.Elements)
                {
                    foreach (RTFDomTableCell cell in row.Elements)
                    {
                        if (cell.HasAttribute(RTFConsts._clmgf))
                        {  //Debug.WriteLine("found H merge at:" + cell.InnerText.Replace("\\n", ""));

                            int cellIndex = row.Elements.IndexOf(cell);

                            for (int i = cellIndex + 1; i < row.Elements.Count; i++)
                            {   //Debug.WriteLine("checking cellindex = " + i);

                                if ((RTFDomTableCell)row.Elements[i] is RTFDomTableCell nextCell)
                                {  //Debug.WriteLine("found nextCell");

                                    if (nextCell.HasAttribute(RTFConsts._clmrg))
                                    {
                                        cell.ColSpan++;
                                        //Debug.WriteLine("found clmrg, incremented ColSpan = " + cell.ColSpan);
                                    }
                                    else
                                        break;
                                }
                            }
                            //Debug.WriteLine("final colspan: " + cell.ColSpan);
                        }

                        if (cell.HasAttribute(RTFConsts._clvmgf) == false)
                        {  //if this cell does not mark vertical merge, then next cell
                            continue;
                        }
                        // if this cell mark vertical merge
                        int colIndex = row.Elements.IndexOf(cell);
                        for (int rowIndex = table.Elements.IndexOf(row) + 1; rowIndex < table.Elements.Count; rowIndex++)
                        {
                            RTFDomTableRow row2 = (RTFDomTableRow)table.Elements[rowIndex];
                            RTFDomTableCell cell2 = (RTFDomTableCell)row2.Elements[colIndex];
                            if (cell2.HasAttribute(RTFConsts._clvmrg))
                            {
                                if (cell2.OverrideCell != null)
                                {  // If this cell has been merge by another cell( must in the same row ), then break the circle
                                    break;
                                }
                                // increase vertical merge.
                                cell.RowSpan++;
                                cell2.OverrideCell = cell;
                            }
                            else
                            {  // cell not mark merged by another cell
                                break;
                            }
                        }
                    }
                }

                // set cell's OverridedCell information
                foreach (RTFDomTableRow row in table.Elements)
                {
                    foreach (RTFDomTableCell cell in row.Elements)
                    {
                        if (cell.RowSpan > 1 || cell.ColSpan > 1)
                        {
                            for (int rowIndex = 1; rowIndex <= cell.RowSpan; rowIndex++)
                            {
                                for (int colIndex = 1; colIndex <= cell.ColSpan; colIndex++)
                                {
                                    int r = table.Elements.IndexOf(row) + rowIndex - 1;
                                    int c = row.Elements.IndexOf(cell) + colIndex - 1;
                                    RTFDomTableCell cell2 = (RTFDomTableCell)table.Elements[r].Elements[c];
                                    if (cell != cell2)
                                        cell2.OverrideCell = cell;
                                }
                            }
                        }
                    }
                }

            }

            if (fixTableCellSize)
            {  // Fix table's left position use the first table column
                if (table.Columns.Count > 0)
                    ((RTFDomTableColumn)table.Columns[0]).Width -= tableLeft;
            }

        }

        private void CopyStyleAttribute(RTFDomTableCell cell, RTFAttributeList table)
        {
            RTFAttributeList attrs = table.Clone();
            attrs.Remove(RTFConsts._clvmgf);
            attrs.Remove(RTFConsts._clvmrg);
            attrs.Remove(RTFConsts._clmgf);
            attrs.Remove(RTFConsts._clmrg);
            cell.Attributes = attrs;
        }

        public override string ToString() => $"RTFDocument: {this.myInfo.Title}";

        private bool ApplyText(RTFTextContainer myText, RTFReader reader, DocumentFormatInfo format)
        {
            if (myText.HasContent)
            {
                string strText = myText.Text;
                myText.Clear();
                //if (this._RTFHtmlState == false)
                //{
                //    _HtmlContentBuilder.Append(strText);
                //    return false ;
                //}
                // if current element is image element , then finish handle image element
                RTFDomImage img = (RTFDomImage)GetLastElement(typeof(RTFDomImage));
                if (img != null && img.Locked == false)
                {
                    img.Data = HexToBytes(strText);
                    img.Format = format.Clone();
                    img.Width = (int)(img.DesiredWidth * img.ScaleX / 100);
                    img.Height = (int)(img.DesiredHeight * img.ScaleY / 100);
                    img.Locked = true;
                    if (reader.TokenType != RTFTokenType.GroupEnd)
                    {
                        ReadToEndGround(reader);
                    }
                    return true;
                }
                else if (format.ReadText && bolStartContent)
                {
                    RTFDomText txt = new()
                    {
                        NativeLevel = myText.Level,
                        Format = format.Clone()
                    };
                    if (txt.Format.Align == RTFAlignment.Justify)
                        txt.Format.Align = RTFAlignment.Left;
                    txt.Text = strText;



                    //  Debug.WriteLine(
                    //$"ApplyText: [{strText}] " +
                    //$"myText.Level={myText.Level}, " +
                    //$"txt.NativeLevel={txt.NativeLevel}, " +
                    //$"bolStartContent={bolStartContent}, " +
                    //$"ReadText={format.ReadText}");

                    //  var es = GetLastElements(true);

                    //  foreach (var e in es)
                    //  {
                    //     Debug.WriteLine(
                    //         $"    {e.GetType().Name}" +
                    //         $" Level={e.NativeLevel}" +
                    //         $" Locked={e.Locked}" +
                    //         $" Text=[{e.InnerText}]");
                    //  }



                    AddContentElement(txt);
                }
            }
            return false;
        }



        /// <summary>
        /// read data , until at the front of the end token belong the current level.
        /// </summary>
        /// <param name="reader"></param>
        private void ReadToEndGround(RTFReader reader)
        {
            reader.ReadToEndGround();
        }


        private void ReadListOverrideTable(RTFReader reader)
        {
            ListOverrideTable = [];
            while (reader.ReadToken() != null)
            {
                if (reader.TokenType == RTFTokenType.GroupEnd)
                {
                    break;
                }
                else if (reader.TokenType == RTFTokenType.GroupStart)
                {
                    // 读取一条记录
                    int level = reader.Level;
                    RTFListOverride record = null!;
                    while (reader.ReadToken() != null)
                    {
                        if (reader.TokenType == RTFTokenType.GroupEnd)
                        {
                            break;
                        }
                        if (reader.CurrentToken.Key == "listoverride")
                        {
                            record = new RTFListOverride();
                            ListOverrideTable.Add(record);
                            continue;
                        }
                        if (record == null)
                        {
                            continue;
                        }
                        switch (reader.CurrentToken.Key)
                        {
                            case "listid":
                                record.ListID = reader.CurrentToken.Param;
                                break;
                            case "listoverridecount":
                                record.ListOverriedCount = reader.CurrentToken.Param;
                                break;
                            case "ls":
                                record.ID = reader.CurrentToken.Param;
                                break;
                        }
                    }
                }
            }//while
        }

        #region HTML RTF 


        private string _HtmlContent = null;

        /// <summary>
        /// HTML content in RTF
        /// </summary>
        public string HtmlContent
        {
            get { return _HtmlContent; }
            set { _HtmlContent = value; }
        }

        private void ReadHtmlContent(RTFReader reader)
        {
            StringBuilder htmlStr = new();
            bool htmlState = true;
            while (reader.ReadToken() != null)
            {
                if (reader.Keyword == "htmlrtf")
                {
                    if (reader.HasParam && reader.Parameter == 0)
                    {
                        htmlState = false;
                    }
                    else
                    {
                        htmlState = true;
                    }
                }
                else if (reader.Keyword == "htmltag")
                {
                    if (reader.InnerReader.Peek() == (int)' ')
                    {
                        reader.InnerReader.Read();
                    }
                    string text = ReadInnerText(reader, null, true, false, true);
                    if (string.IsNullOrEmpty(text) == false)
                    {
                        htmlStr.Append(text);
                    }
                }
                else if (reader.TokenType == RTFTokenType.Keyword
                    || reader.TokenType == RTFTokenType.ExtKeyword)
                {
                    if (htmlState == false)
                    {
                        switch (reader.Keyword)
                        {
                            case "par":
                                htmlStr.Append(Environment.NewLine);
                                break;
                            case "line":
                                htmlStr.Append(Environment.NewLine);
                                break;
                            case "tab":
                                htmlStr.Append("\t");
                                break;
                            case "lquote":
                                htmlStr.Append("&lsquo;");
                                break;
                            case "rquote":
                                htmlStr.Append("&rsquo;");
                                break;
                            case "ldblquote":
                                htmlStr.Append("&ldquo;");
                                break;
                            case "rdblquote":
                                htmlStr.Append("&rdquo;");
                                break;
                            case "bullet":
                                htmlStr.Append("&bull;");
                                break;
                            case "endash":
                                htmlStr.Append("&ndash;");
                                break;
                            case "emdash":
                                htmlStr.Append("&mdash;");
                                break;
                            case "~":
                                htmlStr.Append("&nbsp;");
                                break;
                            case "_":
                                htmlStr.Append("&shy;");
                                break;
                        }
                    }
                }
                else if (reader.TokenType == RTFTokenType.Text)
                {
                    if (htmlState == false)
                    {
                        htmlStr.Append(reader.Keyword);
                    }
                }
            }//while
            this.HtmlContent = htmlStr.ToString();
        }

        #endregion

        private void ReadListTable(RTFReader reader)
        {
            ListTable = [];
            while (reader.ReadToken() != null)
            {
                if (reader.TokenType == RTFTokenType.GroupEnd)
                {
                    break;
                }
                else if (reader.TokenType == RTFTokenType.GroupStart)
                {
                    bool firstRead = true;
                    RTFList currentList = null!;
                    int level = reader.Level;
                    while (reader.ReadToken() != null)
                    {
                        if (reader.TokenType == RTFTokenType.GroupEnd)
                        {
                            if (reader.Level < level)
                                break;
                        }
                        else if (reader.TokenType == RTFTokenType.GroupStart)
                        {
                            // if meet nested level , then ignore
                            //reader.ReadToken();
                            //ReadToEndGround(reader);
                            //reader.ReadToken();
                        }
                        if (firstRead)
                        {
                            if (reader.CurrentToken.Key != "list")
                            {
                                // 不是以list开头，忽略祦E                                ReadToEndGround(reader);
                                reader.ReadToken();
                                break;
                            }
                            currentList = new RTFList();
                            ListTable.Add(currentList);
                            firstRead = false;
                        }
                        switch (reader.CurrentToken.Key)
                        {
                            case "listtemplateid":
                                currentList.ListTemplateID = reader.CurrentToken.Param;
                                break;
                            case "listid":
                                currentList.ListID = reader.CurrentToken.Param;
                                break;
                            case "listhybrid":
                                currentList.ListHybrid = true;
                                break;
                            case "levelfollow":
                                currentList.LevelFollow = reader.CurrentToken.Param;
                                break;
                            case "levelstartat":
                                currentList.LevelStartAt = reader.CurrentToken.Param;
                                break;
                            case "levelnfc":
                                if (currentList.LevelNfc == LevelNumberType.None)
                                {
                                    currentList.LevelNfc = (LevelNumberType)reader.CurrentToken.Param;
                                }
                                break;
                            case "levelnfcn":
                                if (currentList.LevelNfc == LevelNumberType.None)
                                {
                                    currentList.LevelNfc = (LevelNumberType)reader.CurrentToken.Param;
                                }
                                break;
                            case "leveljc":
                                currentList.LevelJc = reader.CurrentToken.Param;
                                break;
                            case "leveltext":
                                //if (currentList.LevelNfc == LevelNumberType.Bullet)
                                {
                                    if (string.IsNullOrEmpty(currentList.LevelText))
                                    {
                                        string text = ReadInnerText(reader, true);
                                        if (text != null && text.Length > 2)
                                        {
                                            int len = (int)text[0];
                                            len = Math.Min(len, text.Length - 1);
                                            text = text.Substring(1, len);
                                        }
                                        currentList.LevelText = text!;
                                    }
                                }
                                break;
                            case "f":
                                currentList.FontName = this.FontTable.GetFontName(reader.CurrentToken.Param);
                                break;
                        }
                    }//while
                }
            }//while
        }


        /// <summary>
        /// read font table
        /// </summary>
        /// <param name="group"></param>
        private void ReadFontTable(RTFReader reader)
        {
            myFontTable.Clear();
            while (reader.ReadToken() != null)
            {
                if (reader.TokenType == RTFTokenType.GroupEnd)
                {
                    break;
                }
                else if (reader.TokenType == RTFTokenType.GroupStart)
                {
                    int index = -1;
                    string name = null!;
                    int charset = 1;
                    bool nilFlag = false;
                    while (reader.ReadToken() != null)
                    {
                        if (reader.TokenType == RTFTokenType.GroupEnd)
                            break;

                        else if (reader.TokenType == RTFTokenType.GroupStart)
                        {
                            // if meet nested level , then ignore
                            reader.ReadToken();
                            ReadToEndGround(reader);
                            reader.ReadToken();
                        }
                        else if (reader.Keyword == "f" && reader.HasParam)
                        {
                            index = reader.Parameter;
                        }
                        else if (reader.Keyword == "fnil")
                        {
                            name = Defaults.FontName;
                            nilFlag = true;
                        }
                        else if (reader.Keyword == RTFConsts._fcharset)
                        {
                            charset = reader.Parameter;
                        }
                        else if (reader.CurrentToken.IsTextToken)
                        {
                            //if (defaultFont == false)
                            {
                                name = ReadInnerText(reader, reader.CurrentToken, false, false, false);
                                if (name != null)
                                {
                                    name = name.Trim();

                                    if (name.EndsWith(';'))
                                        name = name[..^1];
                                }
                            }
                        }
                    }
                    if (index >= 0 && name != null)
                    {
                        if (name.EndsWith(';'))
                            name = name[..^1];

                        name = name.Trim();

                        if (string.IsNullOrEmpty(name))
                            name = Defaults.FontName;
                        //System.Console.WriteLine( "Index:" + index + "  Name:" + name );
                        RTFFont font = new(index, name)
                        {
                            Charset = charset,
                            NilFlag = nilFlag
                        };
                        myFontTable.Add(font);
                    }
                }//else
            }//while
        }

        /// <summary>
        /// read color table
        /// </summary>
        /// <param name="group"></param>
        private void ReadColorTable(RTFReader reader)
        {
            myColorTable.Clear();
            myColorTable.CheckValueExistWhenAdd = false;
            int r = -1;
            int g = -1;
            int b = -1;
            while (reader.ReadToken() != null)
            {
                if (reader.TokenType == RTFTokenType.GroupEnd)
                    break;

                switch (reader.Keyword)
                {
                    case "red":
                        r = reader.Parameter;
                        break;
                    case "green":
                        g = reader.Parameter;
                        break;
                    case "blue":
                        b = reader.Parameter;
                        break;
                    case ";":
                        if (r >= 0 && g >= 0 && b >= 0)
                        {
                            Color c = Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
                            myColorTable.Add(c);
                            r = -1;
                            g = -1;
                            b = -1;
                        }
                        break;
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
        private void ReadDocumentInfo(RTFReader reader)
        {
            myInfo.Clear();
            int level = 0;
            while (reader.ReadToken() != null)
            {
                if (reader.TokenType == RTFTokenType.GroupStart)
                    level++;

                else if (reader.TokenType == RTFTokenType.GroupEnd)
                {
                    level--;
                    if (level < 0)
                        break;
                }
                else
                {
                    switch (reader.Keyword)
                    {
                        case "creatim":
                            myInfo.Creatim = ReadDateTime(reader);
                            level--;
                            break;
                        case "revtim":
                            myInfo.Revtim = ReadDateTime(reader);
                            level--;
                            break;
                        case "printim":
                            myInfo.Printim = ReadDateTime(reader);
                            level--;
                            break;
                        case "buptim":
                            myInfo.Buptim = ReadDateTime(reader);
                            level--;
                            break;
                        default:
                            if (reader.Keyword != null)
                            {
                                if (reader.HasParam)
                                    myInfo.SetInfo(reader.Keyword, reader.Parameter.ToString());
                                else
                                    myInfo.SetInfo(reader.Keyword, ReadInnerText(reader, true));
                            }
                            break;
                    }
                }
            }//while
        }

        /// <summary>
        /// read datetime
        /// </summary>
        /// <param name="reader">reader</param>
        /// <returns>datetime value</returns>
        private DateTime ReadDateTime(RTFReader reader)
        {
            int yr = 1900;
            int mo = 1;
            int dy = 1;
            int hr = 0;
            int min = 0;
            int sec = 0;
            while (reader.ReadToken() != null)
            {
                if (reader.TokenType == RTFTokenType.GroupEnd)
                    break;

                switch (reader.Keyword)
                {
                    case "yr":
                        yr = reader.Parameter;
                        break;
                    case "mo":
                        mo = reader.Parameter;
                        break;
                    case "dy":
                        dy = reader.Parameter;
                        break;
                    case "hr":
                        hr = reader.Parameter;
                        break;
                    case "min":
                        min = reader.Parameter;
                        break;
                    case "sec":
                        sec = reader.Parameter;
                        break;
                }//switch
            }//while
            return new DateTime(yr, mo, dy, hr, min, sec);
        }

        //private RTFDomImage ReadDomImage(RTFReader reader, DocumentFormatInfo format)
        //{
        //    bolStartContent = true;
        //    RTFDomImage img = new RTFDomImage();
        //    img.NativeLevel = reader.Level;
        //    this.AddContentElement(img);
        //    RTFTextContainer txt = new RTFTextContainer( this );
        //    while (reader.ReadToken() != null)
        //    {
        //        if (reader.Level < img.NativeLevel)
        //        {
        //            break;
        //        }
        //        if (reader.TokenType == RTFTokenType.GroupStart)
        //        {
        //            continue;
        //        }
        //        if (reader.TokenType == RTFTokenType.GroupEnd)
        //        {
        //            continue;
        //        }
        //        if (reader.TokenType == RTFTokenType.Text)
        //        {
        //            txt.Accept(reader.CurrentToken, reader);
        //            continue;
        //        }
        //        switch (reader.Keyword)
        //        {
        //            case RTFConsts._nonshppict :
        //                // ignore group
        //                this.ReadToEndGround(reader);
        //                break;
        //            case RTFConsts._picscalex:
        //                img.ScaleX = reader.Parameter;
        //                break;
        //            case RTFConsts._picscaley:
        //                img.ScaleY = reader.Parameter;
        //                break;
        //            case RTFConsts._picwgoal:
        //                img.DesiredWidth = reader.Parameter;
        //                break;
        //            case RTFConsts._pichgoal:
        //                img.DesiredHeight = reader.Parameter;
        //                break;
        //            case RTFConsts._blipuid:
        //                img.ID = ReadInnerText(reader, true);
        //                break;
        //            case RTFConsts._emfblip:
        //                img.PicType = RTFPicType.Emfblip;
        //                break;
        //            case RTFConsts._pngblip:
        //                img.PicType = RTFPicType.Pngblip;
        //                break;
        //            case RTFConsts._jpegblip:
        //                img.PicType = RTFPicType.Jpegblip;
        //                break;
        //            case RTFConsts._macpict:
        //                img.PicType = RTFPicType.Macpict;
        //                break;
        //            case RTFConsts._pmmetafile:
        //                img.PicType = RTFPicType.Pmmetafile;
        //                break;
        //            case RTFConsts._wmetafile:
        //                img.PicType = RTFPicType.Wmetafile;
        //                break;
        //            case RTFConsts._dibitmap:
        //                img.PicType = RTFPicType.Dibitmap;
        //                break;
        //            case RTFConsts._wbitmap:
        //                img.PicType = RTFPicType.Wbitmap;
        //                break;
        //        }//switch
        //    }//while
        //    if (txt.HasContent)
        //    {
        //        img.Data = HexToBytes(txt.Text);
        //    }
        //    img.Format = format.Clone();
        //    img.Width = (int)(img.DesiredWidth * img.ScaleX / 100);
        //    img.Height = (int)(img.DesiredHeight * img.ScaleY / 100);
        //    img.Locked = true;
        //    return img;
        //}

        /// <summary>
        /// Read a rtf emb object
        /// </summary>
        /// <param name="reader">reader</param>
        /// <param name="format">format</param>
        /// <returns>rtf emb object instance</returns>
        private RTFDomObject ReadDomObject(RTFReader reader, DocumentFormatInfo format)
        {
            RTFDomObject obj = new() { NativeLevel = reader.Level };

            AddContentElement(obj);
            int levelBack = reader.Level;
            while (reader.ReadToken() != null)
            {
                if (reader.Level < levelBack)
                    break;

                if (reader.TokenType == RTFTokenType.GroupStart)
                    continue;

                if (reader.TokenType == RTFTokenType.GroupEnd)
                    continue;

                if (reader.Level == obj.NativeLevel + 1 && reader.Keyword.StartsWith("attribute_"))
                    obj.CustomAttributes[reader.Keyword] = ReadInnerText(reader, true);

                switch (reader.Keyword)
                {
                    case RTFConsts._objautlink:
                        obj.Type = RTFObjectType.AutLink;
                        break;
                    case RTFConsts._objclass:
                        obj.ClassName = ReadInnerText(reader, true);
                        break;
                    case RTFConsts._objdata:
                        string data = ReadInnerText(reader, true);
                        obj.Content = HexToBytes(data);
                        break;
                    case RTFConsts._objemb:
                        obj.Type = RTFObjectType.EMB;
                        break;
                    case RTFConsts._objh:
                        obj.Height = reader.Parameter;
                        break;
                    case RTFConsts._objhtml:
                        obj.Type = RTFObjectType.Html;
                        break;
                    case RTFConsts._objicemb:
                        obj.Type = RTFObjectType.Icemb;
                        break;
                    case RTFConsts._objlink:
                        obj.Type = RTFObjectType.Link;
                        break;
                    case RTFConsts._objname:
                        obj.Name = ReadInnerText(reader, true);
                        break;
                    case RTFConsts._objocx:
                        obj.Type = RTFObjectType.Ocx;
                        break;
                    case RTFConsts._objpub:
                        obj.Type = RTFObjectType.Pub;
                        break;
                    case RTFConsts._objsub:
                        obj.Type = RTFObjectType.Sub;
                        break;
                    case RTFConsts._objtime:
                        break;
                    case RTFConsts._objw:
                        obj.Width = reader.Parameter;
                        break;
                    case RTFConsts._objscalex:
                        obj.ScaleX = reader.Parameter;
                        break;
                    case RTFConsts._objscaley:
                        obj.ScaleY = reader.Parameter;
                        break;
                    case RTFConsts._result:
                        // 读取对象运算结果数据
                        RTFDomElementContainer result = new() { Name = RTFConsts._result };
                        obj.AppendChild(result);
                        Load(reader, format);
                        result.Locked = true;
                        break;
                }
            }//while
            obj.Locked = true;
            return obj;
        }

        /// <summary>
        /// read field
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private RTFDomField ReadDomField(RTFReader reader, DocumentFormatInfo format)
        {
            RTFDomField field = new() { NativeLevel = reader.Level };
            AddContentElement(field);
            int levelBack = reader.Level;
            while (reader.ReadToken() != null)
            {
                if (reader.Level < levelBack)
                    break;

                if (reader.TokenType == RTFTokenType.GroupStart)
                {
                }
                else if (reader.TokenType == RTFTokenType.GroupEnd)
                {
                }

                else
                {
                    switch (reader.Keyword)
                    {
                        case RTFConsts._flddirty:
                            field.Method = RTFDomFieldMethod.Dirty;
                            break;
                        case RTFConsts._fldedit:
                            field.Method = RTFDomFieldMethod.Edit;
                            break;
                        case RTFConsts._fldlock:
                            field.Method = RTFDomFieldMethod.Lock;
                            break;
                        case RTFConsts._fldpriv:
                            field.Method = RTFDomFieldMethod.Priv;
                            break;
                        case RTFConsts._fldrslt:
                            //Debug.WriteLine("found field result");

                            RTFDomElementContainer result = new() { Name = RTFConsts._fldrslt };
                            field.AppendChild(result);
                            Load(reader, format);
                            result.Locked = true;
                            break;

                        case RTFConsts._fldinst:

                            //Debug.WriteLine("found field instruction");

                            RTFDomElementContainer inst = new() { Name = RTFConsts._fldinst };
                            field.AppendChild(inst);

                            // Important: Load this group only once
                            Load(reader, format);

                            inst.Locked = true;

                            string txt = inst.InnerText;

                            if (!string.IsNullOrEmpty(txt))
                            {
                                int index = txt.IndexOf(RTFConsts._HYPERLINK, StringComparison.OrdinalIgnoreCase);

                                if (index >= 0)
                                {
                                    int index1 = txt.IndexOf('"', index);
                                    if (index1 >= 0)
                                    {
                                        int index2 = txt.IndexOf('"', index1 + 1);
                                        if (index2 > index1)
                                        {
                                            string link = txt.Substring(index1 + 1, index2 - index1 - 1);

                                            if (link.StartsWith("_Toc"))
                                                link = "#" + link;

                                            if (format.Parent != null)
                                                format.Parent.Link = link;
                                        }
                                    }
                                }
                            }

                            break;


                    }//switch
                }
            }//while
            field.Locked = true;
            return field;

        }//private RTFDomField ReadDomField(RTFReader reader)

        private string ReadInnerText(RTFReader reader, bool deeply) => ReadInnerText( reader, null!, deeply, false, false);

        /// <summary>
        /// read the following plain text in the current level
        /// </summary>
        /// <param name="reader">RTF reader</param>
        /// <param name="deeply">whether read the text in the sub level</param>
        /// <returns>text</returns>
        private string ReadInnerText( RTFReader reader, RTFToken firstToken, bool deeply, bool breakMeetControlWord, bool htmlMode)
        {
            int level = 0;
            RTFTextContainer container = new(this);
            container.Accept(firstToken, reader);
            while (true)
            {
                RTFTokenType type = reader.PeekTokenType();
                if (type == RTFTokenType.Eof)
                    break;
                if (type == RTFTokenType.GroupStart)
                    level++;

                else if (type == RTFTokenType.GroupEnd)
                {
                    level--;
                    if (level < 0)
                        break;
                }
                reader.ReadToken();

                if (deeply || level == 0)
                {
                    if (htmlMode)
                    {
                        if (reader.Keyword == "par")
                        {
                            container.Append(Environment.NewLine);
                            continue;
                        }
                    }
                    if (container.Accept(reader.CurrentToken, reader))
                        continue;
                    else
                    {
                        if (breakMeetControlWord)
                            break;
                    }
                }

            }//while
            return container.Text;
        }

        public override string ToDomString()
        {
            System.Text.StringBuilder builder = new();
            builder.Append(this.ToString());
            builder.Append($"{Environment.NewLine}   Info");

            foreach (string item in myInfo.StringItems)
                builder.Append($"{Environment.NewLine}      {item}");

            builder.Append(Environment.NewLine + "   ColorTable(" + myColorTable.Count + ")");
            for (int iCount = 0; iCount < myColorTable.Count; iCount++)
            {
                Color c = myColorTable[iCount];
                builder.Append(Environment.NewLine + "      " + iCount + ":" + c.R + " " + c.G + " " + c.B);
            }
            builder.Append(Environment.NewLine + "   FontTable(" + myFontTable.Count + ")");
            foreach (RTFFont font in myFontTable)
            {
                builder.Append(Environment.NewLine + "      " + font.ToString());
            }
            if (ListTable.Count > 0)
            {
                builder.Append(Environment.NewLine + "   ListTable(" + ListTable.Count + ")");
                foreach (RTFList list in this.ListTable)
                    builder.Append(Environment.NewLine + "      " + list.ToString());
            }
            if (this.ListOverrideTable.Count > 0)
            {
                builder.Append(Environment.NewLine + "   ListOverrideTable(" + this.ListOverrideTable.Count + ")");
                foreach (RTFListOverride list in this.ListOverrideTable)
                {
                    builder.Append(Environment.NewLine + "      " + list.ToString());
                }
            }
            builder.Append(Environment.NewLine + "   -----------------------");
            if (string.IsNullOrEmpty(this.HtmlContent) == false)
            {
                builder.Append(Environment.NewLine + "   HTMLContent:" + this.HtmlContent);
                builder.Append(Environment.NewLine + "   -----------------------");
            }
            ToDomString(this.Elements, builder, 1);
            return builder.ToString();
        }
    }
}
