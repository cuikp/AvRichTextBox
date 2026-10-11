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
   /// Define some rtf key word
   /// </summary>
   public sealed class RTFConsts
   {
      public const string _insrsid = "insrsid";
      public const string _af = "af";
      public const string _rtf = "rtf";
      public const string _fonttbl = "fonttbl";
      public const string _docfmt = "docfmt";
      public const string _info = "info";
      public const string _author = "author";
      public const string _creatim = "creatim";
      public const string _version = "version";
      public const string _colortbl = "colortbl";
      public const string _b = "b";
      public const string _blue = "blue";
      //public const string _brdrb = "brdrb";
      public const string _ansi = "ansi";
      public const string _ansicpg = "ansicpg";
      public const string _mac = "mac";
      public const string _pc = "pc";
      public const string _pca = "pca";

      public const string _fnil = "fnil";
      public const string _froman = "froman";
      public const string _fswiss = "fswiss";
      public const string _fmodern = "fmodern";
      public const string _fscript = "fscript";
      public const string _fdecor = "fdecor";
      public const string _ftech = "ftech";
      public const string _fbidi = "fbidi";
      public const string _fcharset = "fcharset";
      public const string _generator = "generator";
      public const string _xmlns = "xmlns";
      public const string _header = "header";
      public const string _footer = "footer";
      public const string _headerl = "headerl";
      public const string _headerr = "headerr";
      public const string _headerf = "headerf";
      public const string _footerl = "footerl";
      public const string _footerr = "footerr";
      public const string _footerf = "footerf";
      public const string _headery = "headery";
      public const string _footery = "footery";
      public const string _stylesheet = "stylesheet";
      public const string _filetbl = "filetbl";
      public const string _listtable = "listtable";
      public const string _listoverride = "listoverride";
      public const string _revtbl = "revtbl";
      public const string _nonshppict = "nonshppict";
      public const string _pntext = "pntext";
      public const string _pntxtb = "pntxtb";
      public const string _pntxta = "pntxta";
      public const string _paperw = "paperw";
      public const string _paperh = "paperh";
      public const string _margl = "margl";
      public const string _margr = "margr";
      public const string _margb = "margb";
      public const string _margt = "margt";
      public const string _landscape = "landscape";
      //public const string _header = "header";
      //public const string _footer = "footer";
      public const string _pard = "pard";
      public const string _page = "page";
      public const string _pagebb = "pagebb";
      public const string _par = "par";
      public const string _ql = "ql";
      public const string _qc = "qc";
      public const string _qr = "qr";
      public const string _qj = "qj";
      public const string _fi = "fi";
      public const string _sl = "sl";
      public const string _slmult = "slmult";
      public const string _sb = "sb";
      public const string _sa = "sa";
      public const string _pn = "pn";
      public const string _pnlvlbody = "pnlvlbody";
      public const string _pnlvlblt = "pnlvlblt";
      public const string _listtext = "listtext";
      public const string _ls = "ls";
      public const string _li = "li";
      public const string _line = "line";
      public const string _plain = "plain";
      public const string _f = "f";
      public const string _fs = "fs";
      public const string _cf = "cf";
      public const string _cb = "cb";
      public const string _chcbpat = "chcbpat";
      public const string _cbpat = "cbpat";

      public const string _i = "i";
      public const string _u = "u";
      public const string _v = "v";
      public const string _highlight = "highlight";
      public const string _ul = "ul";
      public const string _noul = "ulnone";
      public const string _strike = "strike";
      public const string _sub = "sub";
      public const string _super = "super";
      public const string _nosupersub = "nosupersub";
      public const string _bkmkstart = "bkmkstart";
      public const string _bkmkend = "bkmkend";
      public const string _field = "field";
      public const string _flddirty = "flddirty";
      public const string _fldedit = "fldedit";
      public const string _fldlock = "fldlock";
      public const string _fldpriv = "fldpriv";
      public const string _fldinst = "fldinst";
      public const string _fldrslt = "fldrslt";
      public const string _HYPERLINK = "HYPERLINK";
      public const string _blipuid = "blipuid";
      public const string _emfblip = "emfblip";
      public const string _pngblip = "pngblip";
      public const string _jpegblip = "jpegblip";
      public const string _macpict = "macpict";
      public const string _pmmetafile = "pmmetafile";
      public const string _wmetafile = "wmetafile";
      public const string _dibitmap = "dibitmap";
      public const string _wbitmap = "wbitmap";
      public const string _shppict = "shppict";
      public const string _pict = "pict";
      public const string _picscalex = "picscalex";
      public const string _picscaley = "picscaley";
      public const string _picwgoal = "picwgoal";
      public const string _pichgoal = "pichgoal";
      public const string _intbl = "intbl";
      public const string _trowd = "trowd";
      public const string _itap = "itap";
      public const string _nesttableprops = "nesttableprops";
      public const string _nestrow = "nestrow";
      public const string _row = "row";

      public const string _irowband = "irowband";
      public const string _trautofit = "trautofit";
      public const string _trkeepfollow = "trkeepfollow";
      public const string _trqc = "trqc";
      public const string _trql = "trql";
      public const string _trqr = "trqr";
      public const string _trhdr = "trhdr";
      public const string _trrh = "trrh";
      public const string _trkeep = "trkeep";
      public const string _trleft = "trleft";
      public const string _trcbpat = "trcbpat";
      public const string _trcfpat = "trcfpat";
      public const string _trpat = "trpat";
      public const string _trshdng = "trshdng";
      public const string _trwWidth = "trwWidth";
      public const string _trwWidthA = "trwWidthA";
      public const string _irow = "irow";
      public const string _trpaddb = "trpaddb";
      public const string _trpaddl = "trpaddl";
      public const string _trpaddr = "trpaddr";
      public const string _trpaddt = "trpaddt";
      public const string _trpaddfb = "trpaddfb";
      public const string _trpaddfl = "trpaddfl";
      public const string _trpaddfr = "trpaddfr";
      public const string _trpaddft = "trpaddft";

      public const string _clmgf = "clmgf";
      public const string _clmrg = "clmrg";
      public const string _clvmgf = "clvmgf";
      public const string _clvmrg = "clvmrg";
      public const string _cellx = "cellx";
      public const string _clvertalt = "clvertalt";
      public const string _clvertalc = "clvertalc";
      public const string _clvertalb = "clvertalb";
      public const string _clNoWrap = "clNoWrap";
      public const string _clcbpat = "clcbpat";
      public const string _clcfpat = "clcfpat";
      public const string _clpadl = "clpadl";
      public const string _clpadt = "clpadt";
      public const string _clpadr = "clpadr";
      public const string _clpadb = "clpadb";
      public const string _clbrdrl = "clbrdrl";
      public const string _clbrdrt = "clbrdrt";
      public const string _clbrdrr = "clbrdrr";
      public const string _clbrdrb = "clbrdrb";
      public const string _cell = "cell";
      public const string _nestcell = "nestcell";
      public const string _lastrow = "lastrow";
      public const string _brdrt = "brdrt";
      public const string _brdrb = "brdrb";
      public const string _brdrl = "brdrl";
      public const string _brdrr = "brdrr";
      public const string _brdrw = "brdrw";
      public const string _brdrcf = "brdrcf";
      public const string _brdrs = "brdrs";
      public const string _brdrth = "brdrth";
      public const string _brdrdot = "brdrdot";
      public const string _brdrdash = "brdrdash";
      public const string _brdrdashsm = "brdrdashsm";
      public const string _brdrdashd = "brdrdashd";
      public const string _brdrdashdd = "brdrdashdd";
      public const string _chbrdr = "chbrdr";
      public const string _brdrnil = "brdrnil";
      public const string _brdrtbl = "brdrtbl";
      public const string _brdrnone = "brdrnone";
      public const string _brsp = "brsp";
      public const string _nonesttables = "nonesttables";

      public const string _object = "object";
      public const string _objemb = "objemb";
      public const string _objlink = "objlink";
      public const string _objautlink = "objautlink";
      public const string _objsub = "objsub";
      public const string _objpub = "objpub";
      public const string _objicemb = "objicemb";
      public const string _objhtml = "objhtml";
      public const string _objocx = "objocx";
      public const string _objclass = "objclass";
      public const string _objname = "objname";
      public const string _objtime = "objtime";
      public const string _objh = "objh";
      public const string _objw = "objw";
      public const string _objsetsize = "objsetsize";
      public const string _objdata = "objdata";
      public const string _objalias = "objalias";
      public const string _objsect = "objsect";
      public const string _objscalex = "objscalex";
      public const string _objscaley = "objscaley";
      public const string _result = "result";

      public const string _shp = "shp";
      public const string _shpleft = "shpleft";
      public const string _shptop = "shptop";
      public const string _shpbottom = "shpbottom";
      public const string _shpright = "shpright";
      public const string _shplid = "shplid";
      public const string _shpz = "shpz";
      public const string _shptxt = "shptxt";
      public const string _shpgrp = "shpgrp";
      public const string _background = "background";
      public const string _shprslt = "shprslt";
      public const string _shpinst = "shpinst";
      public const string _sp = "sp";
      public const string _sn = "sn";
      public const string _sv = "sv";
      public const string _xmlopen = "xmlopen";

      public const string _fchars = "fchars";
      public const string _lchars = "lchars";


      private RTFConsts()
      {
      }
   }
}
