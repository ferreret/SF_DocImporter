using System;
using System.Collections.Generic;
using System.IO;

namespace PdfProcessingService.Util
{
    public class IniFile
    {
        private readonly Dictionary<string, Dictionary<string, string>> iniData = new();
        private readonly string filePath;

        public IniFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("La ruta no puede estar vacía", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"No se encontró el archivo INI en la ruta: {path}");
            }

            filePath = path;
            LoadIniFile();
        }

        private void LoadIniFile()
        {
            using var reader = new StreamReader(filePath);
            string? currentSection = null;

            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (line == null) continue;
                line = line.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line[1..^1]; // Extraemos el nombre de la sección entre corchetes
                    if (!iniData.ContainsKey(currentSection))
                    {
                        iniData[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                }
                else if (currentSection != null)
                {
                    var keyValue = line.Split('=', 2);
                    if (keyValue.Length == 2)
                    {
                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();
                        iniData[currentSection][key] = value;
                    }
                }
            }
        }

        public string ReadValue(string section, string key)
        {
            if (iniData.TryGetValue(section, out var sectionData) && sectionData.TryGetValue(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"La clave '{key}' no existe en la sección '{section}'.");
        }
    }
}
