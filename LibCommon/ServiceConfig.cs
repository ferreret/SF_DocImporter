using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibUtil
{
    public class ServiceConfig
    {
        public string? FacturasFolder { get; set; }
        public string? FacturasProcessedFolder { get; set; }
        public string? FacturasIncidenciasFolder { get; set; }
        public string? AutorizacionesFolder { get; set; }
        public string? AutorizacionesProcessedFolder { get; set; }
        public string? AutorizacionesIncidenciasFolder { get; set; }
        public string? InformesFolder { get; set; }
        public string? InformesProcessedFolder { get; set; }
        public string? InformesIncidenciasFolder { get; set; }

        public string? LogFolder { get; set; }
        public int DelaySeconds { get; set; }
        public string? WindreamPath { get; set; }
        public string? ObjectType { get; set; }
        public int MonthsArchive { get; set; }
        public int MaxLevensthein { get; set; }
        public string? PathTemplateFactura { get; set; }
        public string? PathMutuas { get; set; }

        public ServiceConfig(string path)
        {
            IniFile iniFile = new IniFile(path);

            FacturasFolder = iniFile.ReadValue("ImportFolders", "Facturas");
            FacturasProcessedFolder = iniFile.ReadValue("ImportFolders", "FacturasProcessed");
            FacturasIncidenciasFolder = iniFile.ReadValue("ImportFolders", "FacturasIncidencias");
            AutorizacionesFolder = iniFile.ReadValue("ImportFolders", "Autorizaciones");
            AutorizacionesProcessedFolder = iniFile.ReadValue("ImportFolders", "AutorizacionesProcessed");
            AutorizacionesIncidenciasFolder = iniFile.ReadValue("ImportFolders", "AutorizacionesIncidencias");
            InformesFolder = iniFile.ReadValue("ImportFolders", "Informes");
            InformesProcessedFolder = iniFile.ReadValue("ImportFolders", "InformesProcessed");
            InformesIncidenciasFolder = iniFile.ReadValue("ImportFolders", "InformesIncidencias");

            // Validar que las carpetas de importación existan
            if (FacturasFolder == null || !Directory.Exists(FacturasFolder))
            {
                throw new DirectoryNotFoundException($"La carpeta de facturas especificada no existe: {FacturasFolder}");
            }
            if (FacturasProcessedFolder == null || !Directory.Exists(FacturasProcessedFolder))
            {
                throw new DirectoryNotFoundException($"La carpeta de facturas procesadas especificada no existe: {FacturasProcessedFolder}");
            }
            if (FacturasIncidenciasFolder == null || !Directory.Exists(FacturasIncidenciasFolder))
            {
                throw new DirectoryNotFoundException($"La carpeta de incidencias de facturas especificada no existe: {FacturasIncidenciasFolder}");
            }
            if (AutorizacionesFolder == null || !Directory.Exists(AutorizacionesFolder))
            {
                throw new DirectoryNotFoundException($"La carpeta de autorizaciones especificada no existe: {AutorizacionesFolder}");
            }
            if (AutorizacionesProcessedFolder == null || !Directory.Exists(AutorizacionesProcessedFolder))
            {
                throw new DirectoryNotFoundException($"La carpeta de autorizaciones procesadas especificada no existe: {AutorizacionesProcessedFolder}");
            }
            if (AutorizacionesIncidenciasFolder == null || !Directory.Exists(AutorizacionesIncidenciasFolder))
            {
                throw new DirectoryNotFoundException($"La carpeta de incidencias de autorizaciones especificada no existe: {AutorizacionesIncidenciasFolder}");
            }
            if (InformesFolder == null || !Directory.Exists(InformesFolder))
            {
                throw new DirectoryNotFoundException($"La carpeta de informes especificada no existe: {InformesFolder}");
            }
            if (InformesProcessedFolder == null || !Directory.Exists(InformesProcessedFolder))
            {
                throw new DirectoryNotFoundException($"La carpeta de informes procesados especificada no existe: {InformesProcessedFolder}");
            }
            if (InformesIncidenciasFolder == null || !Directory.Exists(InformesIncidenciasFolder))
            {
                throw new DirectoryNotFoundException($"La carpeta de incidencias de informes especificada no existe: {InformesIncidenciasFolder}");
            }

            // Obtener la ruta de la carpeta de logs
            LogFolder = iniFile.ReadValue("Logging", "Path");

            // Validar que la ruta de la carpeta de logs exista
            if (LogFolder == null || !Directory.Exists(LogFolder))
            {
                throw new DirectoryNotFoundException($"La carpeta de logs especificada no existe: {LogFolder}");
            }

            // Validamos que los segundos de espera sean un entero válido
            if (!int.TryParse(iniFile.ReadValue("Service", "DelaySeconds"), out int delaySeconds))
            {
                throw new FormatException("El valor 'DelaySeconds' no es un entero válido.");
            }
            DelaySeconds = delaySeconds;

            WindreamPath = iniFile.ReadValue("Windream", "Path");
            ObjectType = iniFile.ReadValue("Windream", "ObjectType");

            // Validamos que los meses de antigüedad sean un entero válido
            if (!int.TryParse(iniFile.ReadValue("Windream", "MesesArchivo"), out int monthsArchive))
            {
                throw new FormatException("El valor 'MonthsArchive' no es un entero válido.");
            }
            MonthsArchive = monthsArchive;

            // Validamos que la distancia de Levenshtein máxima sea un entero válido
            if (!int.TryParse(iniFile.ReadValue("Service", "MaxLevensthein"), out int maxLevensthein))
            {
                throw new FormatException("El valor 'MaxLevensthein' no es un entero válido.");
            }
            MaxLevensthein = maxLevensthein;

            // Validamos que exista la ruta de la plantilla de la factura
            PathTemplateFactura = iniFile.ReadValue("Service", "PathTemplateFactura");

            if (PathTemplateFactura == null || !File.Exists(PathTemplateFactura))
            {
                throw new FileNotFoundException($"La plantilla de la factura no existe en la ruta especificada: {PathTemplateFactura}");
            }

            PathMutuas = iniFile.ReadValue("Service", "PathMutuas");

            if (PathMutuas == null || !File.Exists(PathMutuas))
            {
                throw new FileNotFoundException($"El archivo de mutuas no existe en la ruta especificada: {PathMutuas}");
            }
        }
    }
}
