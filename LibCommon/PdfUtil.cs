using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintasoft.Imaging;

namespace LibUtil
{
    public class PdfUtil
    {
        public PdfUtil()
        {
            ImagingGlobalSettings.RegisterImaging(
                "Nicolas Barcelo Lozano",
                "barcelo@oficinasinpapel.com",
                "YyZM2dTLIZ2gb/pLsjyiBINVbn5VRJ6rZzD7ldgdS3rC6fbvkPu1n4Bfcbp5b6E8kHarQaWWEzknYnmRp73SOYaoQ3ek4kLpH0ziF9phJVVs4h6/eJRCcnXKJQnxO3inFClB1LoX3w27eg5yEfnNrTIlTnuYYwPwWr4y7gKIpckg"
            );

            ImagingGlobalSettings.RegisterPdfReader(
              "Lbnsk1dyRewTfBuu6P1R/ea6jv3XzaRe1KJzxXw+4OrWBlffhoz+7vGK/1QucbLeKwfMy0oucklwKm2e0BMjIWC16YCpdkeJQB5f1EbGhDlxocmUPnE0jzjuwtQxoHZ893vLIbWgOf48Y6emRaMHyVo/80wKdzsQNP0YAWNmWQY0"
            );

            ImagingGlobalSettings.RegisterPdfWriter(
              "Xekaol5hnQDM9hve28G1UCxQzpIu731h7ApOzPcQOIPWXXVSn+HEoDcee0DNZgJFCN5PWuoR1DTqEJfRRYo98pe150CKJXJ88a9J1UzER0pR9/ULMcG6iPIExWOGgTlkMZLAa+PyyjKRXWR9ODh273bTxArt9a+3wQx+OeT3XBKU"
            );

            Vintasoft.Imaging.Drawing.SkiaSharp.SkiaSharpDrawingFactory.SetAsDefault();
        }

        /// <summary>
        /// Copies all pages of source PDF document to the end of destination PDF document.
        /// </summary>
        /// <param name="srcPdfFileName">The filename of source PDF document.</param>
        /// <param name="destPdfFileName">The filename of destination PDF document.</param>
        public void CopyPagesFromOnePdfDocumentToAnother(string srcPdfFileName, string destPdfFileName)
        {
            // open source PDF document
            using (Vintasoft.Imaging.Pdf.PdfDocument srcDocument =
                new Vintasoft.Imaging.Pdf.PdfDocument(srcPdfFileName))
            {
                // open destination PDF document
                using (Vintasoft.Imaging.Pdf.PdfDocument destDocument =
                    new Vintasoft.Imaging.Pdf.PdfDocument(destPdfFileName))
                {
                    // get pages of source PDF document as array
                    Vintasoft.Imaging.Pdf.Tree.PdfPage[] srcDocumentPages = srcDocument.Pages.ToArray();

                    // append the array of PDF pages to the destination PDF document
                    destDocument.Pages.AddRange(srcDocumentPages);

                    // save changes to a file
                    destDocument.SaveChanges();
                }
            }
        }
    }
}
