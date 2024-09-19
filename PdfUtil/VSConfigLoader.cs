using Microsoft.Extensions.Configuration;
using System.IO;
using Vintasoft.Imaging;

namespace PdfUtil
{
    public static class VintasoftConfigLoader
    {
        private static IConfiguration _configuration;
        private static bool _isInitialized = false;

        static VintasoftConfigLoader()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) // Usar la base del dominio actual
                .AddJsonFile("vsconfig.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        // Método para inicializar el registro de licencias
        public static void InitializeLicenses()
        {
            if (!_isInitialized)
            {
                ImagingGlobalSettings.RegisterImaging(
                    _configuration["VintasoftSettings:ImagingLicenseName"],
                    _configuration["VintasoftSettings:ImagingLicenseEmail"],
                    _configuration["VintasoftSettings:ImagingLicenseKey"]
                );

                ImagingGlobalSettings.RegisterPdfReader(
                    _configuration["VintasoftSettings:PdfReaderLicenseKey"]
                );

                _isInitialized = true; // Evita que se registre más de una vez
            }
        }
    }
}
