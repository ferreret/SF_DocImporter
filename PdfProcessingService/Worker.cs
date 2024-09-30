using PdfProcessingService.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfProcessingService.Processors;
using PdfProcessingService.Models;

namespace PdfProcessingService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WindreamImporter _importer;
        private readonly MetaDataExtractor _extractor;
        private FileLogger? _fileLogger;

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

        // -------------------------------------------------------------------------------------------------------
        // FUNCTION: CheckTimeFile
        // Función que comprueba si el archivo tiene más de 5 minutos de antigüedad
        // NBL 2024/04/17
        // -------------------------------------------------------------------------------------------------------
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
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                        // Cargar la configuración del servicio
                        var configPath = Path.Combine(GetExecutablePath(), "PdfProcessingService.ini");
                        var config = new ServiceConfig(configPath);

                        // Inicializar el logger de archivo
                        _fileLogger = new FileLogger(config.LogFolder!);

                        if (_fileLogger == null)
                        {
                            _logger.LogError("FileLogger no está inicializado.");
                            return;
                        }

                        // Por cada una de las carpetas de entrada, hacemos un bucle por los archivos pdf
                        foreach (var (inputFolder, outputFolder) in config.Folders!)
                        {
                            _fileLogger.LogInformation($"Procesando archivos en la carpeta: {inputFolder}");

                            var files = Directory.GetFiles(inputFolder, "*.pdf");

                            foreach (var file in files)
                            {
                                if (!CheckTimeFile(file, config.DelaySeconds))
                                {
                                    _fileLogger.LogInformation($"El archivo {file} no tiene más de {config.DelaySeconds} segundos de antigüedad. No se procesará");
                                    continue;
                                }

                                _fileLogger.LogInformation($"Procesando archivo PDF: {file}");

                                WindreamIndexes metadata;

                                try
                                {
                                    metadata = _extractor.Extract(file, _fileLogger);
                                    _fileLogger.LogInformation(metadata.ToString());
                                }
                                catch (Exception ex)
                                {
                                    _fileLogger.LogError($"Error al extraer metadata del archivo {file}: {ex.Message}");
                                    continue;
                                }

                                bool fileImported = false;

                                // Importar el archivo a Windream
                                if (metadata != null)
                                {
                                    try
                                    {
                                        fileImported = _importer.Import(file, metadata, _fileLogger, config);
                                    }
                                    catch (Exception ex)
                                    {
                                        _fileLogger.LogError($"Error al importar el archivo {file} a Windream: {ex.Message}");
                                        continue;
                                    }

                                }

                                // Mover el archivo a la carpeta de archivos procesados
                                // Creo una carpeta por año y añado al nombre del archivo la fecha hora de procesado
                                if (fileImported)
                                {
                                    string processedFolder = Path.Combine(outputFolder, DateTime.Now.Year.ToString());
                                    Directory.CreateDirectory(processedFolder);

                                    string processedFile = Path.Combine(processedFolder, $"{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                                    File.Move(file, processedFile);

                                    _fileLogger.LogInformation($"Archivo procesado: {file}");
                                }
                                else
                                {
                                    _fileLogger.LogError($"Error al importar el archivo {file} a Windream. No se ha movido a la carpeta de archivos procesados.");
                                }
                            }
                        }

                        _fileLogger.LogInformation("Fin del bucle. Todos los archivos procesados");
                        _fileLogger.LogInformation("###############################################################################################");
                    }

                    // Simular alguna tarea con retardo
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    // Registrar error en el logger de consola
                    _logger.LogError(ex, "An error occurred while running the worker");

                    // Registrar error en el archivo de log si está inicializado
                    if (_fileLogger != null)
                    {
                        _fileLogger.LogError($"Error: {ex.Message} | StackTrace: {ex.StackTrace}");
                    }

                    // Retrasar el ciclo en caso de un error, evitando que el bucle se ejecute inmediatamente de nuevo
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
    }
}
