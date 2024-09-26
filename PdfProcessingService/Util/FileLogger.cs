using System;
using System.IO;
using System.Threading;

namespace PdfProcessingService.Util
{
    public class FileLogger
    {
        private readonly string _logFilePath;
        private static readonly object _lock = new object();

        public FileLogger(string logFilePath)
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                throw new ArgumentException("Log file path cannot be null or empty", nameof(logFilePath));
            }

            _logFilePath = logFilePath;

            // Ensure the directory exists
            string? directory = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(directory) && directory != null)
            {
                Directory.CreateDirectory(directory);
            }
        }

        private string GetLogFilePath()
        {
            string? directory = Path.GetDirectoryName(_logFilePath);
            string fileName = $"{DateTime.Now:yyyyMMdd}.log";
            return Path.Combine(directory!, fileName);
        }

        private void Log(string message, FileLogLevel level)
        {
            string logFilePath = GetLogFilePath();
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            try
            {
                lock (_lock)  // Ensure thread safety
                {
                    using StreamWriter streamWriter = new(logFilePath, append: true);
                    streamWriter.WriteLine(logEntry);
                }
            }
            catch (IOException ioEx)
            {
                // Handle logging errors (could log to an alternative location or raise an event)
                Console.Error.WriteLine($"Failed to log message: {ioEx.Message}");
            }
        }

        public void LogInformation(string message)
        {
            Log(message, FileLogLevel.Information);
        }

        public void LogError(string message)
        {
            Log(message, FileLogLevel.Error);
        }
    }

    public enum FileLogLevel
    {
        Information,
        Error
    }
}
