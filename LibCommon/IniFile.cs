using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibUtil
{
    public class IniFile
    {
        private readonly Dictionary<string, Dictionary<string, string>> iniData
            = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> rawLines = new();
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
            foreach (var raw in File.ReadAllLines(filePath))
            {
                rawLines.Add(raw);
            }

            string? currentSection = null;
            foreach (var raw in rawLines)
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line[1..^1];
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

        public string TryReadValue(string section, string key, string defaultValue = "")
        {
            if (iniData.TryGetValue(section, out var sectionData) && sectionData.TryGetValue(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        public bool TryReadBool(string section, string key, bool defaultValue)
        {
            var raw = TryReadValue(section, key, "");
            if (bool.TryParse(raw, out var b)) return b;
            return defaultValue;
        }

        public double TryReadDouble(string section, string key, double defaultValue)
        {
            var raw = TryReadValue(section, key, "");
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
            return defaultValue;
        }

        public void WriteValue(string section, string key, string value)
        {
            if (!iniData.ContainsKey(section))
            {
                iniData[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            iniData[section][key] = value;
        }

        public void WriteBool(string section, string key, bool value)
            => WriteValue(section, key, value ? "true" : "false");

        public void WriteDouble(string section, string key, double value)
            => WriteValue(section, key, value.ToString(CultureInfo.InvariantCulture));

        public void Save()
        {
            var output = new List<string>();
            var pendingPerSection = iniData.ToDictionary(
                kv => kv.Key,
                kv => new Dictionary<string, string>(kv.Value, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

            string? currentSection = null;
            int? lastValueLineForSection = null;

            void FlushSectionPending(string section)
            {
                if (!pendingPerSection.TryGetValue(section, out var pending) || pending.Count == 0) return;
                int insertAt = lastValueLineForSection.HasValue ? lastValueLineForSection.Value + 1 : output.Count;
                foreach (var kv in pending)
                {
                    output.Insert(insertAt, $"{kv.Key}={kv.Value}");
                    insertAt++;
                }
                pending.Clear();
            }

            foreach (var raw in rawLines)
            {
                var trimmed = raw.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";"))
                {
                    output.Add(raw);
                    continue;
                }

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    if (currentSection != null) FlushSectionPending(currentSection);
                    currentSection = trimmed[1..^1];
                    lastValueLineForSection = null;
                    output.Add(raw);
                    continue;
                }

                var keyValue = trimmed.Split('=', 2);
                if (keyValue.Length == 2 && currentSection != null)
                {
                    string key = keyValue[0].Trim();
                    if (pendingPerSection.TryGetValue(currentSection, out var pending) && pending.TryGetValue(key, out var newValue))
                    {
                        output.Add($"{key}={newValue}");
                        pending.Remove(key);
                    }
                    else
                    {
                        output.Add(raw);
                    }
                    lastValueLineForSection = output.Count - 1;
                }
                else
                {
                    output.Add(raw);
                }
            }

            if (currentSection != null) FlushSectionPending(currentSection);

            foreach (var kv in pendingPerSection)
            {
                if (kv.Value.Count == 0) continue;
                if (output.Count > 0 && !string.IsNullOrWhiteSpace(output[^1])) output.Add("");
                output.Add($"[{kv.Key}]");
                foreach (var entry in kv.Value)
                {
                    output.Add($"{entry.Key}={entry.Value}");
                }
            }

            File.WriteAllLines(filePath, output, new UTF8Encoding(false));
            rawLines.Clear();
            rawLines.AddRange(output);
        }
    }
}
