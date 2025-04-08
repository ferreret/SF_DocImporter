using LibUtil;
using LibUtil.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vintasoft.Imaging;
using Vintasoft.Imaging.Pdf.Tree;
using Vintasoft.Imaging.Pdf;
using Vintasoft.Imaging.Text;

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
            // Factura? factura = TemplateManagement.ApplyFacturaTemplate(pathPdf, _serviceConfig.PathTemplateFactura! , _fileLogger);

            Factura factura = new Factura();

            string fullText = VSUtil.GetFullText(pathPdf);

            // NoFactura
            var match = Regex.Match(fullText, @"MSF\d{8,10}");
            if (match.Success)
            {
                factura.NoFactura = match.Value;

                // Fecha Factura
                var dateMatches = Regex.Matches(fullText.Substring(0, match.Index), @"\d{1,2}/\d{1,2}/\d{4}");
                if (dateMatches.Count > 0)
                {
                    factura.FechaFactura = dateMatches[dateMatches.Count - 1].Value;
                }
            }

            // NoAutorizacion y NIF Mutua
            string lineaAutorizacion = VSUtil.GetTextFromAnchorText(pathPdf, 0, "Autorización:", false);

            // Si hago un split por espacios y hay solo un elemento, es el cif de mutua
            string[] splitLineaAutorizacion = lineaAutorizacion.Split(' ');
            if (splitLineaAutorizacion.Length == 1)
            {
                factura.CIFMutua = splitLineaAutorizacion[0];
                factura.NoAutorizacion = string.Empty;
            }
            else
            {
                factura.NoAutorizacion = splitLineaAutorizacion[0];
                factura.CIFMutua = splitLineaAutorizacion[1];
            }

            // DNI Paciente
            factura.DNIPaciente = VSUtil.GetTextFromAnchorText(pathPdf, 0, "DNI:", true);

            // Nombre Paciente
            factura.NombrePaciente = VSUtil.GetNombrePaciente(pathPdf, 0);

            if (!string.IsNullOrEmpty(factura.NombrePaciente) && !string.IsNullOrEmpty(factura.DNIPaciente))
            {
                factura.NombrePaciente = factura.NombrePaciente.Replace(factura.DNIPaciente, string.Empty)
                                                               .Replace("\r", " ")
                                                               .Replace("\n", " ")
                                                               .Trim();
            }

            // Get Mutua
            factura.Mutua = VSUtil.GetMutua(pathPdf, 0);

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
                if (fileNameWithoutExtension.Contains("-A"))
                {
                    windreamIndexes.NoAutorizacion = fileNameWithoutExtension.Split('-')[0];
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
