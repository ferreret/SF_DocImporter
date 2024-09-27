using PdfProcessingService.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfProcessingService.Processors;

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
                        foreach (var inputFolder in config.Folders!)
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

                                try
                                {
                                    var metadata = _extractor.Extract(file, _fileLogger);
                                    _fileLogger.LogInformation(metadata.ToString());
                                }
                                catch (Exception ex)
                                {
                                    _fileLogger.LogError($"Error al procesar el archivo {file}: {ex.Message}");
                                }

                                // Extraer metadatos del archivo
                                //var metadata = _extractor.Extract(file); 

                                // Importar el archivo a Windream
                                //_importer.Import(file, metadata);


                                _fileLogger.LogInformation($"Archivo procesado: {file}");
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
