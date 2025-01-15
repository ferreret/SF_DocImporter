using System.Drawing;
using System.Text.RegularExpressions;
using System.Text;
using Vintasoft.Imaging.Pdf;
using LibUtil.Models;
using Vintasoft.Imaging.Text;
using Vintasoft.Imaging.Pdf.Tree;

namespace LibDataExtractor
{
    public class VSUtil
    {
        public static string GetTextFromAnchorText(string pdfPath, int pageIndex, string anchorText, float width, int startIndex = 0)
        {
            using var document = new PdfDocument(pdfPath);

            // Encuentra el texto anchor en la página
            var anchor = FindTextOnPdfPage(document, pageIndex, anchorText, startIndex);

            if (anchor == null)
            {
                Console.WriteLine("Anchor text not found.");
                return string.Empty;
            }

            Console.WriteLine($"Left: {anchor.Rectangle.Left}, Top: {anchor.Rectangle.Top}, Width: {anchor.Rectangle.Width}, Height: {anchor.Rectangle.Height}");

            // Dado que el origen de coordenadas es el extremo inferior izquierdo,
            // se calcula la región a la derecha del texto anchor:
            var searchRectangle = new RectangleF
            {
                X = anchor.Rectangle.Left + anchor.Rectangle.Width,  // Comienza justo después del anchor (a la derecha)
                Y = anchor.Rectangle.Top + anchor.Rectangle.Height,   // Mantiene la misma altura y posición vertical que el anchor
                Width = width,                                        // Utiliza el ancho proporcionado como parámetro
                Height = anchor.Rectangle.Height                      // Mantiene la misma altura que el anchor
            };

            // Busca el texto dentro de la región especificada a la derecha del anchor
            var textRegion = GetTextSubregion(document, pageIndex, searchRectangle);

            if (textRegion == null)
            {
                Console.WriteLine("No text found in the specified region to the right of the anchor.");
                return string.Empty;
            }

            Console.WriteLine($"Found text in the specified region: {textRegion.TextContent}");

            return textRegion.TextContent;
        }


        // --------------------------------------------------------------------------------------------------------
        // Obtiene el texto completo de un archivo PDF
        // --------------------------------------------------------------------------------------------------------
        public static string GetFullText(string pdfPath)
        {
            using var pdfDocument = new PdfDocument(pdfPath);
            var textBuilder = new StringBuilder();

            for (int i = 0; i < pdfDocument.Pages.Count; i++)
            {
                var page = pdfDocument.Pages[i];
                textBuilder.Append(page.TextRegion.ToString());
            }

            return textBuilder.ToString();
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca texto en una región específica de la página usando coordenadas
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion FindTextInRectangle(PdfDocument document, int pageIndex, SearchRectangle searchRectangle, int startIndex = 0)
        {
            var subregion = GetTextSubregion(document, pageIndex, searchRectangle);
            return subregion.FindText(searchRectangle.Expression, ref startIndex, false);
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca texto en una región específica de la página usando coordenadas
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion FindTextInRectangle(PdfDocument document, int pageIndex, RectangleF rectangle, string expression, int startIndex = 0)
        {
            var subregion = GetTextSubregion(document, pageIndex, rectangle);
            return subregion.FindText(expression, ref startIndex, false);
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca un patrón regex en una región específica de la página usando coordenadas
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion FindRegexInRectangle(PdfDocument document, int pageIndex, SearchRectangle searchRectangle)
        {
            var regexSearchEngine = TextSearchEngine.Create(CreateRegex(searchRectangle.Expression!, true));
            var subregion = GetTextSubregion(document, pageIndex, searchRectangle);

            int startIndex = 0;
            return subregion.FindText(regexSearchEngine, ref startIndex, false);
        }

        public static TextRegion FindTextOnPdfPage(string pdfPath, int pageIndex, string textToFind, int startIndex = 0)
        {
            using var document = new PdfDocument(pdfPath);
            return document.Pages[pageIndex].TextRegion.FindText(textToFind, ref startIndex, false);
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca texto en toda la página
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion FindTextOnPdfPage(PdfDocument document, int pageIndex, string textToFind, int startIndex = 0)
        {
            return document.Pages[pageIndex].TextRegion.FindText(textToFind, ref startIndex, false);
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca un patrón regex en toda la página
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion FindRegexOnPdfPage(PdfDocument document, int pageIndex, string regex, int startIndex = 0)
        {
            var regexSearchEngine = TextSearchEngine.Create(CreateRegex(regex, true));
            return document.Pages[pageIndex].TextRegion.FindText(regexSearchEngine, ref startIndex, false);
        }

        // --------------------------------------------------------------------------------------------------------
        // Método auxiliar para crear un objeto Regex
        // --------------------------------------------------------------------------------------------------------
        private static Regex CreateRegex(string text, bool matchCase)
        {
            var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
            return new Regex(text, options);
        }

        // --------------------------------------------------------------------------------------------------------
        // Método auxiliar para obtener una subregión de texto en una página del PDF
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion GetTextSubregion(PdfDocument document, int pageIndex, SearchRectangle searchRectangle)
        {
            return document.Pages[pageIndex].TextRegion.GetSubregion(
                new RectangleF(searchRectangle.Left, searchRectangle.Top, searchRectangle.Width, searchRectangle.Height),
                TextSelectionMode.Rectangle
            );
        }

        // --------------------------------------------------------------------------------------------------------
        // Método auxiliar para obtener una subregión de texto en una página del PDF
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion GetTextSubregion(PdfDocument document, int pageIndex, RectangleF rectangle)
        {
            return document.Pages[pageIndex].TextRegion.GetSubregion(
                rectangle,
                TextSelectionMode.Rectangle
            );
        }

        public static string GetTextFromAnchorText(string pdfPath, int pageNumber, string anchorText, bool column = false)
        {

            using PdfDocument pdfDocument = new PdfDocument(pdfPath);
            PdfPage page = pdfDocument.Pages[pageNumber];
            TextRegion sourceRegion = page.TextRegion;

            int startindex = 0;
            TextRegion textRegion = sourceRegion.FindText(anchorText, ref startindex, false);

            if (textRegion != null)
            {
                // Console.WriteLine("Text found: " + textRegion.ToString());
                // Console.WriteLine($"Left: {textRegion.Rectangle.Left}, Top: {textRegion.Rectangle.Top}, Width: {textRegion.Rectangle.Width}, Height: {textRegion.Rectangle.Height}");

                float x0 = textRegion.Rectangle.X;
                float y0 = textRegion.Rectangle.Y;
                float x1 = textRegion.Rectangle.X + textRegion.Rectangle.Width;
                float y1 = textRegion.Rectangle.Y + textRegion.Rectangle.Height;

                float width = page.MediaBox.Width;

                if (column)
                {
                    width = page.MediaBox.Width / 3;
                }

                RectangleF lineRect = new RectangleF(x1, (y0 + y1) / 2, width - x1, 0);

                TextRegion textRegionSearch = sourceRegion.GetSubregion(lineRect, TextSelectionMode.Rectangle);

                // Console.WriteLine("Text found: " + textRegionSearch.ToString());
                // Console.WriteLine($"Left: {textRegionSearch.Rectangle.Left}, Top: {textRegionSearch.Rectangle.Top}, Width: {textRegionSearch.Rectangle.Width}, Height: {textRegionSearch.Rectangle.Height}");

                return textRegionSearch.TextContent;
            }
            else
            {
                Console.WriteLine("Text not found");
                return string.Empty;
            }
        }



        public static string GetNombrePaciente(string pdfPath, int pageNumber)
        {
            using PdfDocument pdfDocument = new PdfDocument(pdfPath);
            PdfPage page = pdfDocument.Pages[pageNumber];
            TextRegion sourceRegion = page.TextRegion;

            int startindex = 0;

            TextRegion labelNombrePaciente = sourceRegion.FindText("Paciente:", ref startindex, false);
            TextRegion labelDNI = sourceRegion.FindText("DNI:", ref startindex, false);

            if (labelNombrePaciente != null && labelDNI != null)
            {
                float x0 = labelNombrePaciente.Rectangle.X;
                float y0 = labelNombrePaciente.Rectangle.Y;
                float x1 = labelNombrePaciente.Rectangle.X + labelNombrePaciente.Rectangle.Width;
                float y1 = labelDNI.Rectangle.Y;
                float width = page.MediaBox.Width / 1.8f;
                RectangleF lineRect = new RectangleF(x1, (y0 + y1) / 2, width - x1, y0 - y1);
                TextRegion textRegionSearch = sourceRegion.GetSubregion(lineRect, TextSelectionMode.Rectangle);
                // Console.WriteLine("Text found: " + textRegionSearch.ToString());
                // Console.WriteLine($"Left: {textRegionSearch.Rectangle.Left}, Top: {textRegionSearch.Rectangle.Top}, Width: {textRegionSearch.Rectangle.Width}, Height: {textRegionSearch.Rectangle.Height}");
                string resultado =  textRegionSearch.TextContent;
                return resultado;
            }

            return "";
        }

        public static string GetMutua(string pdfPath, int pageNumber)
        {
            using PdfDocument pdfDocument = new PdfDocument(pdfPath);
            PdfPage page = pdfDocument.Pages[pageNumber];
            TextRegion sourceRegion = page.TextRegion;

            int startindex = 0;

            TextRegion labelNombrePaciente = sourceRegion.FindText("Paciente:", ref startindex, false);
            TextRegion labelDNI = sourceRegion.FindText("DNI:", ref startindex, false);

            if (labelNombrePaciente != null && labelDNI != null)
            {
                float x0 = labelNombrePaciente.Rectangle.X;
                float y0 = labelNombrePaciente.Rectangle.Y;                
                float y1 = labelDNI.Rectangle.Y;
                float x1 = labelDNI.Rectangle.X;
                float width = page.MediaBox.Width * 1.1f;
                RectangleF lineRect = new RectangleF(width / 2, y0, width, y1 - y0);
                TextRegion textRegionSearch = sourceRegion.GetSubregion(lineRect, TextSelectionMode.Rectangle);
                // Console.WriteLine("Text found: " + textRegionSearch.ToString());
                // Console.WriteLine($"Left: {textRegionSearch.Rectangle.Left}, Top: {textRegionSearch.Rectangle.Top}, Width: {textRegionSearch.Rectangle.Width}, Height: {textRegionSearch.Rectangle.Height}");
                string resultado = textRegionSearch.TextContent;


                float x0_dni = labelDNI.Rectangle.X;
                float y0_dni = labelDNI.Rectangle.Y;
                float x1_dni = labelDNI.Rectangle.X + labelDNI.Rectangle.Width;
                float y1_dni = labelDNI.Rectangle.Y + labelDNI.Rectangle.Height;

                RectangleF postDNIRect = new RectangleF(width / 2, (y0_dni + y1_dni) / 2, width, 0);

                TextRegion textRegionSearchDNI = sourceRegion.GetSubregion(postDNIRect, TextSelectionMode.Rectangle);

                string lineaDNI = textRegionSearchDNI.TextContent;

                // Si encuentro lineaDNI en resultado, lo elimino
                if (resultado.Contains(lineaDNI))
                {
                    resultado = resultado.Replace(lineaDNI, "");

                    // Si en resultado hay dos retornos de carro, elimino lo que hay después de los dos retornos de carro
                    if (resultado.Contains("\r\n\r\n"))
                    {
                        int index = resultado.IndexOf("\r\n\r\n");
                        resultado = resultado.Substring(0, index);
                    }
                }

                return Regex.Replace(resultado.Replace("\r", " ")
                                 .Replace("\n", " ")
                                 .Trim(), @"\s+", " ");
            }

            return "";
        }

        //    public static string GetAutorizacionFactura(string pdfPath, int pageNumber)
        //    {
        //        using PdfDocument pdfDocument = new PdfDocument(pdfPath);
        //        PdfPage page = pdfDocument.Pages[pageNumber];
        //        TextRegion sourceRegion = page.TextRegion;

        //        int startindex = 0;
        //        TextRegion labelAutorizacion = sourceRegion.FindText("Autorización:", ref startindex, false);
        //        if (labelAutorizacion != null)
        //        {
        //            float x0 = labelAutorizacion.Rectangle.X;
        //            float y0 = labelAutorizacion.Rectangle.Y;
        //            float x1 = labelAutorizacion.Rectangle.X + labelAutorizacion.Rectangle.Width;
        //            float y1 = labelAutorizacion.Rectangle.Y + labelAutorizacion.Rectangle.Height;

        //            float width = page.MediaBox.Width / 3;

        //            RectangleF lineRect = new RectangleF(x1, (y0 + y1) / 2, width - x1, 0);

        //            TextRegion textRegionSearch = sourceRegion.GetSubregion(lineRect, TextSelectionMode.Rectangle);

        //            // Console.WriteLine("Text found: " + textRegionSearch.ToString());
        //            // Console.WriteLine($"Left: {textRegionSearch.Rectangle.Left}, Top: {textRegionSearch.Rectangle.Top}, Width: {textRegionSearch.Rectangle.Width}, Height: {textRegionSearch.Rectangle.Height}");

        //            return textRegionSearch.TextContent;

        //        }
        //        return "";
        //    }


    }
}
