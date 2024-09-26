using PdfProcessingService.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PdfProcessingService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private FileLogger? _fileLogger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public string GetExecutablePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
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

                        _logger.LogInformation("Configuration and logging initialized.");
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

