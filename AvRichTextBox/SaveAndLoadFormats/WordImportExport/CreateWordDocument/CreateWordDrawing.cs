using A = DocumentFormat.OpenXml.Drawing;
using DOW = DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

internal static partial class WordConversions
{
   internal static DOW.Drawing CreateWordDocDrawing(string relationshipID, double pixelWidth, double pixelHeight, string extension)
   {
      double emuWidth = PixToEMU(pixelWidth);
      double emuHeight = PixToEMU(pixelHeight);

      var drawingElement = new DOW.Drawing(new DW.Inline(new DW.Extent()
      {
         Cx = (int)emuWidth,
         Cy = (int)emuHeight
      }, new DW.EffectExtent()
      {
         LeftEdge = 0L,
         TopEdge = 0L,
         RightEdge = 0L,
         BottomEdge = 0L,
      },
          new DW.DocProperties() { Id = 1U, Name = "Picture1" },
          new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks() { NoChangeAspect = true }),
          new A.Graphic(
              new A.GraphicData(
                  new PIC.Picture(
                      new PIC.NonVisualPictureProperties(new PIC.NonVisualDrawingProperties()
                      {
                         Id = 0U,
                         Name = "wordDrawing" + extension
                      },
          new PIC.NonVisualPictureDrawingProperties()),
          new PIC.BlipFill(new A.Blip(new A.BlipExtensionList(new A.BlipExtension() { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" }))
          {
             Embed = relationshipID,
             CompressionState = A.BlipCompressionValues.Print
          }, new A.Stretch(new A.FillRectangle())
          ),
          new PIC.ShapeProperties(
  new A.Transform2D(
      new A.Offset() { X = 0L, Y = 0L },
      new A.Extents() { Cx = (int)emuWidth, Cy = (int)emuHeight }
      ),
  new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
)))
              { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
))
      {
         DistanceFromTop = 0U,
         DistanceFromBottom = 0U,
         DistanceFromLeft = 0U,
         DistanceFromRight = 0U
      }
);

      return drawingElement;
   }

}
