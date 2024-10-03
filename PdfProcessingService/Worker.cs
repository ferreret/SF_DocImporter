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
    /// <summary>
    /// Clase Worker que se ejecuta en segundo plano como parte de un servicio.
    /// Realiza el procesamiento de archivos PDF, incluyendo la extracción de metadata e importación.
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WindreamImporter _importer;
        private readonly MetaDataExtractor _extractor;
        private FileLogger? _fileLogger;

        /// <summary>
        /// Constructor que inicializa el worker con el logger, el importador y el extractor de metadata.
        /// </summary>
        /// <param name="logger">Logger para registrar mensajes del worker.</param>
        /// <param name="importer">Importador de archivos hacia Windream.</param>
        /// <param name="extractor">Extractor de metadata de los archivos PDF.</param>
        public Worker(ILogger<Worker> logger, WindreamImporter importer, MetaDataExtractor extractor)
        {
            _logger = logger;
            _importer = importer;
            _extractor = extractor;
        }

        /// <summary>
        /// Obtiene el directorio donde se ejecuta la aplicación.
        /// </summary>
        /// <returns>Ruta del directorio de la aplicación.</returns>
        public string GetExecutablePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Verifica si un archivo PDF tiene más antigüedad que el tiempo de retardo configurado.
        /// </summary>
        /// <param name="file">Ruta del archivo a verificar.</param>
        /// <param name="delaySeconds">Tiempo de retardo en segundos.</param>
        /// <returns>Verdadero si el archivo tiene más antigüedad que el tiempo de retardo.</returns>
        public bool CheckTimeFile(string file, int delaySeconds)
        {
            DateTime lastWriteTime = File.GetLastWriteTime(file);
            TimeSpan ts = DateTime.Now - lastWriteTime;

            return ts.TotalSeconds > delaySeconds;
        }

        /// <summary>
        /// Método principal que ejecuta el worker en un bucle hasta que se cancela.
        /// Procesa archivos PDF en carpetas configuradas, extrae metadata y los importa.
        /// </summary>
        /// <param name="stoppingToken">Token de cancelación que indica cuándo detener el proceso.</param>
        /// <returns>Tarea asincrónica.</returns>
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

                    // Simular una tarea con retardo
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                    await Task.Delay(5000, stoppingToken); // Retrasar el ciclo en caso de un error
                }
            }
        }

        /// <summary>
        /// Carga la configuración del servicio desde un archivo INI.
        /// </summary>
        /// <returns>Objeto de configuración cargado.</returns>
        private ServiceConfig LoadServiceConfig()
        {
            var configPath = Path.Combine(GetExecutablePath(), "PdfProcessingService.ini");
            var config = new ServiceConfig(configPath);
            return config;
        }

        /// <summary>
        /// Inicializa el logger de archivos utilizando la carpeta de logs configurada.
        /// </summary>
        /// <param name="config">Configuración del servicio.</param>
        private void InitializeFileLogger(ServiceConfig config)
        {
            _fileLogger = new FileLogger(config.LogFolder!);
            if (_fileLogger == null)
            {
                _logger.LogError("FileLogger no está inicializado.");
                throw new Exception("FileLogger initialization failed");
            }
        }

        /// <summary>
        /// Procesa todas las carpetas configuradas, recorriendo los archivos PDF de entrada.
        /// </summary>
        /// <param name="config">Configuración del servicio.</param>
        private void ProcessFolders(ServiceConfig config)
        {
            foreach (var (inputFolder, outputFolder, errorFolder) in config.Folders!)
            {
                _fileLogger!.LogInformation($"Procesando archivos en la carpeta: {inputFolder}");

                var files = Directory.GetFiles(inputFolder, "*.pdf");

                foreach (var file in files)
                {
                    ProcessFile(file, outputFolder, errorFolder, config);
                }
            }

            _fileLogger!.LogInformation("Fin del bucle. Todos los archivos procesados");
            _fileLogger.LogInformation("###############################################################################################");
        }

        /// <summary>
        /// Procesa un archivo PDF, extrayendo su metadata e intentando importarlo.
        /// Si la importación es exitosa, mueve el archivo a la carpeta de procesados.
        /// </summary>
        /// <param name="file">Ruta del archivo a procesar.</param>
        /// <param name="outputFolder">Carpeta de salida donde mover el archivo procesado.</param>
        /// <param name="config">Configuración del servicio.</param>
        private void ProcessFile(string file, string outputFolder, string errorFolder, ServiceConfig config)
        {
            if (!CheckTimeFile(file, config.DelaySeconds))
            {
                _fileLogger!.LogInformation($"El archivo {file} no tiene más de {config.DelaySeconds} segundos de antigüedad. No se procesará");
                return;
            }

            _fileLogger!.LogInformation($"Procesando archivo PDF: {file}");

            WindreamIndexes metadata;
            try
            {
                metadata = _extractor.Extract(file, _fileLogger);
                _fileLogger.LogInformation(metadata.ToString());
            }
            catch (Exception ex)
            {
                _fileLogger.LogError($"Error al extraer metadata del archivo {file}: {ex.Message}");
                MoveProcessedFile(file, errorFolder);
                return;
            }

            bool fileImported = ImportFile(file, metadata, config);

            if (fileImported)
            {
                MoveProcessedFile(file, outputFolder);
            }
            else
            {
                _fileLogger.LogError($"Error al importar el archivo {file} a Windream. Enviamos el archivo a la carpeta de incidencias");
                MoveProcessedFile(file, errorFolder);
            }
        }

        /// <summary>
        /// Importa un archivo PDF a Windream utilizando la metadata extraída.
        /// </summary>
        /// <param name="file">Ruta del archivo a importar.</param>
        /// <param name="metadata">Metadata extraída del archivo.</param>
        /// <param name="config">Configuración del servicio.</param>
        /// <returns>Verdadero si la importación fue exitosa.</returns>
        private bool ImportFile(string file, WindreamIndexes metadata, ServiceConfig config)
        {
            try
            {
                return _importer.Import(file, metadata, _fileLogger!, config);
            }
            catch (Exception ex)
            {
                _fileLogger!.LogError($"Error al importar el archivo {file} a Windream: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Mueve un archivo procesado a la carpeta de salida, creando una subcarpeta por año.
        /// Si el archivo ya existe, agrega un contador antes de la extensión.
        /// </summary>
        /// <param name="file">Ruta del archivo a mover.</param>
        /// <param name="outputFolder">Carpeta de salida donde mover el archivo procesado.</param>
        private void MoveProcessedFile(string file, string outputFolder)
        {
            string processedFolder = Path.Combine(outputFolder, DateTime.Now.Year.ToString());
            Directory.CreateDirectory(processedFolder);

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            string extension = Path.GetExtension(file);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string processedFile = Path.Combine(processedFolder, $"{fileNameWithoutExtension}_{timestamp}{extension}");

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


        /// <summary>
        /// Maneja los errores que ocurren durante la ejecución del worker, registrando los mensajes de error.
        /// </summary>
        /// <param name="ex">Excepción ocurrida.</param>
        private void HandleError(Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running the worker");

            if (_fileLogger != null)
            {
                _fileLogger.LogError($"Error: {ex.Message} | StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Registra el inicio del worker en los logs.
        /// </summary>
        private void LogWorkerStart()
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
        }
    }
}
