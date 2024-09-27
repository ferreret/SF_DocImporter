using PdfProcessingService.Models;
using PdfProcessingService.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Vintasoft.Imaging;
using Vintasoft.Imaging.Drawing.Gdi;

namespace PdfProcessingService.Processors
{
    public class MetaDataExtractor
    {

        FileLogger _fileLogger;

        public MetaDataExtractor()
        {
            ImagingGlobalSettings.RegisterImaging(
                "Nicolas Barcelo Lozano",
                "barcelo@oficinasinpapel.com",
                "QG0v2N4jWvCrmNlwRDqIol0GhT9g89ZdLw5rz/pfapytcsnPhwUrxiHoj5IOgsGzCaZiKEEgZ0InkrDpw0LSGPKVBBzzFR5PgbfyetXMO7Jx+Kse1jwSVZ9IW8gZAfcmQYoAKTkMAdfFKFYyIaR3S4wHT7xgCcF4q56ilfN8nZyA"
            );

            ImagingGlobalSettings.RegisterPdfReader(
              "K81FF2uRBF13BM2Mw9b72FZGQim2hCYHFoxFHUKNG87Q+lLS9jg5GTR9fnWv7yzmCwaE0yBFMBowy9oqFXah2tqjVvnlnvSALfhUDhRTLs3HBGtlRpxE/8ftehITmgsl6zVyuqsMMpsjVIqPEZGDZUgzO6OJZ76QoKmt+jLD4mJA"
            );

            GdiGraphicsFactory.SetAsDefault();
        }

        public WindreamIndexes Extract(string pathPdf, FileLogger fileLogger)
        {
            _fileLogger = fileLogger;

            WindreamIndexes windreamIndexes = new WindreamIndexes();

            // Extraer los metadatos del PDF

            // Miro el último caracter del nombre del archivo menos la extensión
            string lastChar = ObtenerUltimoCaracterSinExtension(pathPdf).ToString();

            // Si es A es una autorización
            // Si es F es una factura
            // Si es I es un informe
            if (lastChar == "A")
            {
                ProcessAutorizacion(pathPdf, windreamIndexes);
            }
            else if (lastChar == "F")
            {
                ProcessFactura(pathPdf, windreamIndexes);
            }
            else if (lastChar == "I")
            {
                ProcessInforme(pathPdf, windreamIndexes);
            }
            else
            {
                throw new ArgumentException("El archivo no es una autorización, factura o informe.");
            }

            return windreamIndexes;
        }

        private void ProcessAutorizacion(string pathPdf, WindreamIndexes windreamIndexes)
        {
            // Extraer los metadatos de una autorización
        }

        private void ProcessFactura(string pathPdf, WindreamIndexes windreamIndexes)
        {
            // Extraer los metadatos de una factura
        }

        private void ProcessInforme(string pathPdf, WindreamIndexes windreamIndexes)
        {
            // Extraer los metadatos de un informe
        }

        public static char ObtenerUltimoCaracterSinExtension(string filePath)
        {
            // Obtiene el nombre del archivo sin la extensión
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

            // Verifica si el nombre es válido y tiene al menos un carácter
            if (!string.IsNullOrEmpty(fileNameWithoutExtension))
            {
                // Retorna el último carácter del nombre
                return fileNameWithoutExtension[fileNameWithoutExtension.Length - 1];
            }
            else
            {
                throw new ArgumentException("El nombre del archivo no es válido o está vacío.");
            }
        }
    }
}
