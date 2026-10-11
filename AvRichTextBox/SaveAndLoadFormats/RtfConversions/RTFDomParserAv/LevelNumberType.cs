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
using System.Collections.Generic;
using System.Text;

namespace RtfDomParserAv
{
    public enum LevelNumberType
    {
        None = -10,
        ///<summary>	Arabic (1, 2, 3)	</summary>
        Arabic	=	0	,
        ///<summary>	Uppercase Roman numeral (I, II, III)	</summary>
        Uppercase_Roman_numeral	=	1	,
        ///<summary>	Lowercase Roman numeral (i, ii, iii)	</summary>
        Lowercase_Roman_numeral	=	2	,
        ///<summary>	Uppercase letter (A, B, C)	</summary>
        Uppercase_letter	=	3	,
        ///<summary>	Lowercase letter (a, b, c)	</summary>
        Lowercase_letter	=	4	,
        ///<summary>	Ordinal number (1st, 2nd, 3rd)	</summary>
        Ordinal_number	=	5	,
        ///<summary>	Cardinal text number (One, Two Three)	</summary>
        Cardinal_text_number	=	6	,
        ///<summary>	Ordinal text number (First, Second, Third)	</summary>
        Ordinal_text_number	=	7	,
        ///<summary>	Kanji numbering without the digit character (*dbnum1)	</summary>
        Kanji_numbering_without_the_digit_character	=	10	,
        ///<summary>	Kanji numbering with the digit character (*dbnum2)	</summary>
        Kanji_numbering_with_the_digit_characte	=	11	,
        ///<summary>	46 phonetic katakana characters in "aiueo" order (*aiueo)	</summary>
        _46_phonetic_katakana_characters_in_aiueo_order	=	12	,
        ///<summary>	46 phonetic katakana characters in "iroha" order (*iroha)	</summary>
        _46_phonetic_katakana_characters_in_iroha_order	=	13	,
        ///<summary>	Double_byte character	</summary>
        Double_byte_character	=	14	,
        ///<summary>	Single_byte character	</summary>
        Single_byte_character	=	15	,
        ///<summary>	Kanji numbering 3 (*dbnum3)	</summary>
        Kanji_numbering_3	=	16	,
        ///<summary>	Kanji numbering 4 (*dbnum4)	</summary>
        Kanji_numbering_4	=	17	,
        ///<summary>	Circle numbering (*circlenum)	</summary>
        Circle_numbering	=	18	,
        ///<summary>	Double_byte Arabic numbering	</summary>
        Double_byte_Arabic_numbering	=	19	,
        ///<summary>	46 phonetic double_byte katakana characters (*aiueo*dbchar)	</summary>
        _46_phonetic_double_byte_katakana_characters_aiueo_dbchar	=	20	,
        ///<summary>	46 phonetic double_byte katakana characters (*iroha*dbchar)	</summary>
        _46_phonetic_double_byte_katakana_characters_iroha_dbchar	=	21	,
        ///<summary>	Arabic with leading zero (01, 02, 03, ..., 10, 11)	</summary>
        Arabic_with_leading_zero	=	22	,
        ///<summary>	Bullet (no number at all)	</summary>
        Bullet	=	23	,
        ///<summary>	Korean numbering 2 (*ganada)	</summary>
        Korean_numbering_2	=	24	,
        ///<summary>	Korean numbering 1 (*chosung)	</summary>
        Korean_numbering_1	=	25	,
        ///<summary>	Chinese numbering 1 (*gb1)	</summary>
        Chinese_numbering_1	=	26	,
        ///<summary>	Chinese numbering 2 (*gb2)	</summary>
        Chinese_numbering_2	=	27	,
        ///<summary>	Chinese numbering 3 (*gb3)	</summary>
        Chinese_numbering_3	=	28	,
        ///<summary>	Chinese numbering 4 (*gb4)	</summary>
        Chinese_numbering_4	=	29	,
        ///<summary>	Chinese Zodiac numbering 1 (* zodiac1)	</summary>
        Chinese_Zodiac_numbering_1	=	30	,
        ///<summary>	Chinese Zodiac numbering 2 (* zodiac2) 	</summary>
        Chinese_Zodiac_numbering_2	=	31	,
        ///<summary>	Chinese Zodiac numbering 3 (* zodiac3)	</summary>
        Chinese_Zodiac_numbering_3	=	32	,
        ///<summary>	Taiwanese double_byte numbering 1	</summary>
        Taiwanese_double_byte_numbering_1	=	33	,
        ///<summary>	Taiwanese double_byte numbering 2	</summary>
        Taiwanese_double_byte_numbering_2	=	34	,
        ///<summary>	Taiwanese double_byte numbering 3	</summary>
        Taiwanese_double_byte_numbering_3	=	35	,
        ///<summary>	Taiwanese double_byte numbering 4	</summary>
        Taiwanese_double_byte_numbering_4	=	36	,
        ///<summary>	Chinese double_byte numbering 1	</summary>
        Chinese_double_byte_numbering_1	=	37	,
        ///<summary>	Chinese double_byte numbering 2	</summary>
        Chinese_double_byte_numbering_2	=	38	,
        ///<summary>	Chinese double_byte numbering 3	</summary>
        Chinese_double_byte_numbering_3	=	39	,
        ///<summary>	Chinese double_byte numbering 4	</summary>
        Chinese_double_byte_numbering_4	=	40	,
        ///<summary>	Korean double_byte numbering 1	</summary>
        Korean_double_byte_numbering_1	=	41	,
        ///<summary>	Korean double_byte numbering 2	</summary>
        Korean_double_byte_numbering_2	=	42	,
        ///<summary>	Korean double_byte numbering 3	</summary>
        Korean_double_byte_numbering_3	=	43	,
        ///<summary>	Korean double_byte numbering 4	</summary>
        Korean_double_byte_numbering_4	=	44	,
        ///<summary>	Hebrew non_standard decimal 	</summary>
        Hebrew_non_standard_decimal_	=	45	,
        ///<summary>	Arabic Alif Ba Tah	</summary>
        Arabic_Alif_Ba_Tah	=	46	,
        ///<summary>	Hebrew Biblical standard	</summary>
        Hebrew_Biblical_standard	=	47	,
        ///<summary>	Arabic Abjad style	</summary>
        Arabic_Abjad_style	=	48	,
        ///<summary>	No number	</summary>
        No_number	=	255	
    }
}
