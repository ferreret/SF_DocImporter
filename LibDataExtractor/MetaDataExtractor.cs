using LibUtil;
using LibUtil.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Vintasoft.Imaging;

namespace LibDataExtractor
{
    public class MetaDataExtractor
    {

        FileLogger _fileLogger;
        ServiceConfig _serviceConfig;

#pragma warning disable CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de agregar el modificador "required" o declararlo como un valor que acepta valores NULL.
        public MetaDataExtractor()
#pragma warning restore CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de agregar el modificador "required" o declararlo como un valor que acepta valores NULL.
        {
            ImagingGlobalSettings.RegisterImaging(
                "Nicolas Barcelo Lozano",
                "barcelo@oficinasinpapel.com",
                "QG0v2N4jWvCrmNlwRDqIol0GhT9g89ZdLw5rz/pfapytcsnPhwUrxiHoj5IOgsGzCaZiKEEgZ0InkrDpw0LSGPKVBBzzFR5PgbfyetXMO7Jx+Kse1jwSVZ9IW8gZAfcmQYoAKTkMAdfFKFYyIaR3S4wHT7xgCcF4q56ilfN8nZyA"
            );

            ImagingGlobalSettings.RegisterPdfReader(
              "K81FF2uRBF13BM2Mw9b72FZGQim2hCYHFoxFHUKNG87Q+lLS9jg5GTR9fnWv7yzmCwaE0yBFMBowy9oqFXah2tqjVvnlnvSALfhUDhRTLs3HBGtlRpxE/8ftehITmgsl6zVyuqsMMpsjVIqPEZGDZUgzO6OJZ76QoKmt+jLD4mJA"
            );

            Vintasoft.Imaging.Drawing.SkiaSharp.SkiaSharpDrawingFactory.SetAsDefault();
        }

        public WindreamIndexes Extract(string pathPdf, FileLogger fileLogger, ServiceConfig serviceConfig, TipoDocumento tipoDoc)
        {
            _fileLogger = fileLogger;
            _serviceConfig = serviceConfig;

            WindreamIndexes windreamIndexes = new();
            
            // Si es A es una autorización
            // Si es F es una factura
            // Si es I es un informe
            if (tipoDoc == TipoDocumento.Autorización)
            {
                ProcessAutorizacion(pathPdf, windreamIndexes);
            }
            else if (tipoDoc == TipoDocumento.Factura)
            {
                ProcessFactura(pathPdf, windreamIndexes);
            }
            else if (tipoDoc == TipoDocumento.Informe)
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

            // En el caso de la autorización, el número de autorización es el nombre del archivo menos
            // guión, la letra (A) y la extensión
            if (pathPdf.ToUpper().EndsWith("-A.PDF"))
            {
                windreamIndexes.NoAutorizacion = Path.GetFileNameWithoutExtension(pathPdf).Remove(Path.GetFileNameWithoutExtension(pathPdf).Length - 2);                
            }
            else
            {
                windreamIndexes.NoAutorizacion = "";
            }
            windreamIndexes.TipoDoc = TipoDocumento.Autorización;
        }

        private void ProcessFactura(string pathPdf, WindreamIndexes windreamIndexes)
        {
            // Extraer los metadatos de una factura
            Factura? factura = TemplateManagement.ApplyFacturaTemplate(pathPdf, _serviceConfig.PathTemplateFactura! , _fileLogger);

            windreamIndexes.TipoDoc = TipoDocumento.Factura;

            // Si la factura no es nula, mapeamos los datos a los índices de Windream
            if (factura != null)
            {
                windreamIndexes.NoFactura = factura.NoFactura is null ? String.Empty : factura.NoFactura.RemoveCarriageReturns();
                windreamIndexes.NoAutorizacion = factura.NoAutorizacion is null ? String.Empty : factura.NoAutorizacion.RemoveCarriageReturns();
                windreamIndexes.DNIPaciente = factura.DNIPaciente is null ? String.Empty : factura.DNIPaciente.RemoveCarriageReturns();
                windreamIndexes.NombrePaciente = factura.NombrePaciente is null ? String.Empty : factura.NombrePaciente.RemoveCarriageReturns();
                windreamIndexes.NIFMutua = factura.CIFMutua is null ? String.Empty : factura.CIFMutua.RemoveCarriageReturns();
                windreamIndexes.Cobertura = factura.Mutua is null ? String.Empty : factura.Mutua.RemoveCarriageReturns();                

                if (DateTime.TryParse(factura.FechaFactura, out DateTime fechaFactura))
                {
                     windreamIndexes.FechaFactura = fechaFactura;
                }
                else
                {
                   _fileLogger.LogError("Error al parsear la fecha de la factura.");
                }
            }

            if (string.IsNullOrEmpty(windreamIndexes.NoAutorizacion))
            {
                // Si no se ha recuperado el No de Autorización, lo recuperamos del nombre del archivo sin la A final y la extensión
                // Comprobar previamente que el nombre del archivo acaba con A
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pathPdf);
                if (fileNameWithoutExtension.EndsWith("-A"))
                {
                    windreamIndexes.NoAutorizacion = Path.GetFileNameWithoutExtension(pathPdf).Remove(Path.GetFileNameWithoutExtension(pathPdf).Length - 2);
                }
            }
        }

        private void ProcessInforme(string pathPdf, WindreamIndexes windreamIndexes)
        {
            // Extraer los metadatos de un informe
            string nombre = VSUtil.GetTextFromAnchorText(pathPdf, 0, "NOMBRE:");
            string nif = VSUtil.GetTextFromAnchorText(pathPdf, 0, "NIF / NIE:");
            string cobertura = VSUtil.GetTextFromAnchorText(pathPdf, 0, "COBERTURA:");
            string autorizacion = VSUtil.GetTextFromAnchorText(pathPdf, 0, "AUTORIZACIÓN:");

            // Si los valores no son nulos, los mapeamos a los índices de Windream
            windreamIndexes.NombrePaciente = nombre is null ? String.Empty : nombre.RemoveCarriageReturns();
            windreamIndexes.DNIPaciente = nif is null ? String.Empty : nif.RemoveCarriageReturns();
            windreamIndexes.CoberturaInforme = cobertura is null ? String.Empty : cobertura.RemoveCarriageReturns();
            windreamIndexes.NoAutorizacion = autorizacion is null ? String.Empty : autorizacion.RemoveCarriageReturns();
            windreamIndexes.TipoDoc = TipoDocumento.Informe;

            // Si el informe no he recuperado el No de Autorización, lo recupero del nombre del archivo sin la I final y la extensión
            // Comprobar previamente que el nombre del archivo acaba con I
            if (string.IsNullOrEmpty(windreamIndexes.NoAutorizacion))
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pathPdf);
                if (fileNameWithoutExtension.EndsWith("-A"))
                {
                    windreamIndexes.NoAutorizacion = fileNameWithoutExtension.Remove(fileNameWithoutExtension.Length - 2);
                }
                else
                {
                    // Ponemos el nombre de archivo en el índice de NombrePaciente
                    windreamIndexes.NombrePaciente = fileNameWithoutExtension;
                }
            }
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
