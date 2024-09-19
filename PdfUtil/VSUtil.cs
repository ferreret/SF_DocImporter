using System.Text;

namespace PdfUtil;

public class VSUtil
{
    public VSUtil()
    {
        VintasoftConfigLoader.InitializeLicenses();
        Vintasoft.Imaging.Drawing.SkiaSharp.SkiaSharpDrawingFactory.SetAsDefault();
    }

    public string GetFullText(string pdfPath)
    {
        using (var pdfDocument = new Vintasoft.Imaging.Pdf.PdfDocument(pdfPath))
        {
            var text = new StringBuilder();
            for (int i = 0; i < pdfDocument.Pages.Count; i++)
            {
                var page = pdfDocument.Pages[i];
                text.Append(page.TextRegion.ToString());
            }
            return text.ToString();
        }
    }
}
