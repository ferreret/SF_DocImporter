using System;
using System.Collections.Generic;
using System.IO;

namespace PdfProcessingService.Util
{
    public class ServiceConfig
    {
        public int FoldersCount { get; set; }
        public List<string>? Folders { get; set; }
        public string? LogFolder { get; set; }
        public int DelaySeconds { get; set; }


        public ServiceConfig(string path)
        {
            IniFile iniFile = new IniFile(path);

            // Validar que el conteo de carpetas sea un entero válido
            if (!int.TryParse(iniFile.ReadValue("ImportFolders", "Count"), out int folderCount))
            {
                throw new FormatException("El valor 'Count' en la sección 'ImportFolders' no es un entero válido.");
            }
            FoldersCount = folderCount;

            // Inicializar la lista de carpetas
            Folders = new List<string>();

            for (int i = 1; i <= FoldersCount; i++)
            {
                string folderPath = iniFile.ReadValue("ImportFolders", $"Folder{i}");

                // Validar que cada carpeta exista
                if (!Directory.Exists(folderPath))
                {
                    throw new DirectoryNotFoundException($"La carpeta especificada no existe: {folderPath}");
                }

                Folders.Add(folderPath);
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
        }
    }
}
