using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using LibUtil;
using LibUtil.Models;
using LibDataExtractor;
using LibWin;
using System.Runtime.InteropServices;

namespace PdfProcessingService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WindreamImporter _importer;
        private readonly MetaDataExtractor _extractor;
        private FileLogger? _fileLogger;

        // New counters to track processing statistics
        private int _totalFilesProcessed;
        private int _totalFilesSucceeded;
        private int _totalFilesFailed;

        public Worker(ILogger<Worker> logger, WindreamImporter importer, MetaDataExtractor extractor)
        {
            _logger = logger;
            _importer = importer;
            _extractor = extractor;
        }

        public string GetExecutablePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public bool CheckTimeFile(string file, int delaySeconds)
        {
            DateTime lastWriteTime = File.GetLastWriteTime(file);
            TimeSpan ts = DateTime.Now - lastWriteTime;

            return ts.TotalSeconds > delaySeconds;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    LogWorkerStart();

                    var config = LoadServiceConfig();
                    InitializeFileLogger(config);

                    ProcessFolders(config);

                    // Log the current counters
                    LogProcessingStatistics();

#if DEBUG
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    Console.WriteLine("Continuing...");
#endif

                    // Simular una tarea con retardo
                    await Task.Delay(2000, stoppingToken);
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                    await Task.Delay(5000, stoppingToken); // Retrasar el ciclo en caso de un error
                }
            }
        }

        private ServiceConfig LoadServiceConfig()
        {
            var configPath = Path.Combine(GetExecutablePath(), "PdfProcessingService.ini");
            var config = new ServiceConfig(configPath);
            return config;
        }

        private void InitializeFileLogger(ServiceConfig config)
        {
            _fileLogger = new FileLogger(config.LogFolder!);
            if (_fileLogger == null)
            {
                _logger.LogError("FileLogger no está inicializado.");
                throw new Exception("FileLogger initialization failed");
            }
        }

        private void ProcessFolders(ServiceConfig config)
        {
            var folders = new List<(string, string, string, TipoDocumento)>
            {
                (config.AutorizacionesFolder!, config.AutorizacionesProcessedFolder!, config.AutorizacionesIncidenciasFolder!, TipoDocumento.Autorización),
                (config.FacturasFolder!, config.FacturasProcessedFolder!, config.FacturasIncidenciasFolder!, TipoDocumento.Factura),
                (config.InformesFolder!, config.InformesProcessedFolder!, config.InformesIncidenciasFolder!, TipoDocumento.Informe)
            };

            foreach (var (inputFolder, outputFolder, errorFolder, tipoDoc) in folders!)
            {
                _fileLogger!.LogInformation($"Procesando archivos en la carpeta: {inputFolder}");

                var files = Directory.GetFiles(inputFolder, "*.pdf");

                foreach (var file in files)
                {
                    _totalFilesProcessed++; // Increment the total files processed count
                    ProcessFile(file, outputFolder, errorFolder, tipoDoc, config);
                }
            }

            _fileLogger!.LogInformation("Fin del bucle. Todos los archivos procesados");
            _fileLogger.LogInformation("###############################################################################################");
        }

        private void ProcessFile(string file, string outputFolder, string errorFolder, TipoDocumento tipoDoc, ServiceConfig config)
        {
            if (!CheckTimeFile(file, config.DelaySeconds))
            {
                _fileLogger!.LogInformation($"El archivo {file} no tiene más de {config.DelaySeconds} segundos de antigüedad. No se procesará");
                return;
            }

            _fileLogger!.LogInformation($"Procesando archivo PDF {tipoDoc.ToString()}: {file}");

            WindreamIndexes metadata;
            try
            {
                metadata = _extractor.Extract(file, _fileLogger, config, tipoDoc);
                _fileLogger.LogInformation(metadata.ToString());
            }
            catch (Exception ex)
            {
                _fileLogger.LogError($"Error al extraer metadata del archivo {file}: {ex.Message}");
                MoveProcessedFile(file, errorFolder);
                _totalFilesFailed++; // Increment the failed files count
                return;
            }

            string fileImported = ImportFile(file, metadata, config);
            
            if (!string.IsNullOrEmpty(fileImported))
            {
                MoveProcessedFile(file, outputFolder, fileImported);
                _totalFilesSucceeded++; // Increment the succeeded files count

                // We write to the console in green that the file has been processed
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Archivo procesado: {file}");
                Console.ResetColor();
            }
            else
            {
                _fileLogger.LogError($"Error al importar el archivo {file} a Windream. Enviamos el archivo a la carpeta de incidencias");
                MoveProcessedFile(file, errorFolder);
                _totalFilesFailed++; // Increment the failed files count
            }
        }

        private string ImportFile(string file, WindreamIndexes metadata, ServiceConfig config)
        {
            try
            {
                return _importer.Import(file, metadata, _fileLogger!, config);
            }
            catch (Exception ex)
            {
                _fileLogger!.LogError($"Error al importar el archivo {file} a Windream: {ex.Message}");
                return "";
            }
        }

        private void MoveProcessedFile(string file, string outputFolder, string importedFileName = "")
        {
            string processedFolder = Path.Combine(outputFolder, DateTime.Now.Year.ToString());
            Directory.CreateDirectory(processedFolder);

            string fileNameWithoutExtension = "";

            if (string.IsNullOrEmpty(importedFileName))
                fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            else
                fileNameWithoutExtension = importedFileName.Substring(0, importedFileName.Length - 4);

            string extension = Path.GetExtension(file);
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

            string processedFile = Path.Combine(processedFolder, $"{fileNameWithoutExtension}-{timestamp}{extension}");

            // Comprobar si el archivo ya existe y agregar un contador si es necesario
            int counter = 1;
            while (File.Exists(processedFile))
            {
                processedFile = Path.Combine(processedFolder, $"{fileNameWithoutExtension}_{timestamp}_{counter}{extension}");
                counter++;
            }

            File.Move(file, processedFile);

            _fileLogger!.LogInformation($"Archivo procesado: {processedFile}");
        }

        private void HandleError(Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running the worker");

            if (_fileLogger != null)
            {
                _fileLogger.LogError($"Error: {ex.Message} | StackTrace: {ex.StackTrace}");
            }
        }

        private void LogWorkerStart()
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
        }

        // Method to log processing statistics
        private void LogProcessingStatistics()
        {
            _fileLogger?.LogInformation($"Total files processed: {_totalFilesProcessed}", ConsoleColor.Blue);
            _fileLogger?.LogInformation($"Total files succeeded: {_totalFilesSucceeded}", ConsoleColor.Green);
            _fileLogger?.LogInformation($"Total files failed: {_totalFilesFailed}", ConsoleColor.Red);
        }
    }
}