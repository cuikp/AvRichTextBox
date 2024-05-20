using Avalonia;
using Avalonia.Media;
using DocumentFormat.OpenXml;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

public static partial class WordConversions
{

   public static Paragraph GetParagraph(OpenXmlElement section)
   {
      Paragraph para = new Paragraph();

      foreach (OpenXmlElement psection in section.Elements())
      {
         try
         {
            switch (psection.LocalName)
            {
               case "pPr":

                  foreach (OpenXmlElement pps in psection.Elements())
                  {
                     switch (pps.LocalName)
                     {
                        case "jc":

                           switch (pps.GetAttributes()[0].Value)
                           {
                              case "left": { para.TextAlignment = TextAlignment.Left; break; }
                              case "right": { para.TextAlignment = TextAlignment.Right; break; }
                              case "center": { para.TextAlignment = TextAlignment.Center; break; }
                              case "justify": { para.TextAlignment = TextAlignment.Justify; break; }
                           }
                           break;


                        case "spacing":

                           //OpenXmlAttribute afterAtt = pps.GetAttributes().Where(gat => gat.LocalName == "after").FirstOrDefault();
                           //if (afterAtt.Value != null)
                           //{
                           //   double afterPar = Convert.ToDouble(afterAtt.Value);
                           //   para.Margin = new Thickness(0, 0, 0, TwipToPix(afterPar));
                           //}

                           //OpenXmlAttribute parLineSpacing = pps.GetAttributes().Where(gat => gat.LocalName == "line").FirstOrDefault();
                           //if (parLineSpacing.Value != null)
                           //{
                           //   double twipSpacing = 12;
                           //   try
                           //   {
                           //      twipSpacing = Convert.ToDouble(parLineSpacing.Value);
                           //      double thisLH = TwipToPix(twipSpacing);
                           //      para.LineHeight = thisLH == 0 ? 14 : thisLH;
                           //   }
                           //   catch (Exception excp) { Debug.WriteLine("Error setting lineheight\ntwipspacing=" + twipSpacing + ":::parlinespacingval= " + parLineSpacing.Value + "\n" + excp.Message); }
                           //}

                           break;

                        case "ind":

                           //para.TextIndent = TwipToPix(Convert.ToDouble(pps.GetAttributes().Where(gat => gat.LocalName == "firstLine").FirstOrDefault().Value));
                           break;

                        case "rPr":

                           OpenXmlElement rprSize = pps.ChildElements.Where(gat => gat.LocalName == "sz").FirstOrDefault();
                           if (rprSize != null)
                              para.FontSize = PointsToPixels(Convert.ToDouble(rprSize.GetAttributes()[0].Value) / 2);

                           OpenXmlElement rprCsSize = pps.ChildElements.Where(gat => gat.LocalName == "szCs").FirstOrDefault();
                           if (rprCsSize != null)
                              para.FontSize = PointsToPixels(Convert.ToDouble(rprCsSize.GetAttributes()[0].Value) / 2);

                           break;

                        default:
                           break;
                     }
                  }
                  break;


               case "br": para.Inlines.Add(new EditableLineBreak()); break;
               case "smartTag": AddDeepRuns(psection, ref para); break;
               case "hyperlink": AddDeepRuns(psection, ref para); break;
               case "r":
                  try { para.Inlines.Add(GetIEditable(psection, ref para)); }
                  catch { para.Inlines.Add(new EditableRun("")); }
                  break;
               case "del": para.Inlines.Add(GetDeletedRun(psection, ref para)); break;
               case "ins": para.Inlines.Add(GetInsertedRun(psection, ref para)); break;
               default: { break; }

            }

         }

         catch (Exception parContentEx) { Debug.WriteLine("Error in getting paragraph:\nLocalName=" + psection.LocalName + "\n" + parContentEx.Message); }
      }

      //if (double.IsNaN(para.LineHeight)) para.LineHeight = para.FontSize;
      return para;

   }
}
