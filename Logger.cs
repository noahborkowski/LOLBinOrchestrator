using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LOLBinOrchestrator
{
    /// <summary>
    /// Handles execution trace logging for LOLBin operations
    /// </summary>
    public class Logger
    {
        private readonly string _logFilePath;
        private readonly LogLevel _minimumLevel;
        private readonly bool _enableConsoleOutput;
        private readonly object _lockObject = new object();

        public Logger(string logFilePath = "lolbin_orchestrator.log", LogLevel minimumLevel = LogLevel.Info, bool enableConsoleOutput = true)
        {
            _logFilePath = logFilePath;
            _minimumLevel = minimumLevel;
            _enableConsoleOutput = enableConsoleOutput;
        }

        /// <summary>
        /// Logs a message with the specified level
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Optional exception</param>
        public async Task LogAsync(LogLevel level, string message, Exception? exception = null)
        {
            if (level < _minimumLevel)
                return;

            var logEntry = CreateLogEntry(level, message, exception);
            await WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Optional exception</param>
        public async Task DebugAsync(string message, Exception? exception = null)
        {
            await LogAsync(LogLevel.Debug, message, exception);
        }

        /// <summary>
        /// Logs an info message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Optional exception</param>
        public async Task InfoAsync(string message, Exception? exception = null)
        {
            await LogAsync(LogLevel.Info, message, exception);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Optional exception</param>
        public async Task WarningAsync(string message, Exception? exception = null)
        {
            await LogAsync(LogLevel.Warning, message, exception);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Optional exception</param>
        public async Task ErrorAsync(string message, Exception? exception = null)
        {
            await LogAsync(LogLevel.Error, message, exception);
        }

        /// <summary>
        /// Logs a critical message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Optional exception</param>
        public async Task CriticalAsync(string message, Exception? exception = null)
        {
            await LogAsync(LogLevel.Critical, message, exception);
        }

        /// <summary>
        /// Logs technique execution details
        /// </summary>
        /// <param name="techniqueName">Name of the technique</param>
        /// <param name="parameters">Execution parameters</param>
        /// <param name="result">Execution result</param>
        /// <param name="executionTime">Execution time</param>
        public async Task LogTechniqueExecutionAsync(string techniqueName, Dictionary<string, string> parameters, ExecutionResult result, TimeSpan executionTime)
        {
            var message = new StringBuilder();
            message.AppendLine($"Technique Execution: {techniqueName}");
            message.AppendLine($"  Success: {result.Success}");
            message.AppendLine($"  Execution Time: {executionTime.TotalMilliseconds:F2}ms");
            message.AppendLine($"  Exit Code: {result.ExitCode}");
            
            if (parameters.Count > 0)
            {
                message.AppendLine("  Parameters:");
                foreach (var param in parameters)
                {
                    message.AppendLine($"    {param.Key}: {param.Value}");
                }
            }

            if (!string.IsNullOrEmpty(result.Output))
            {
                message.AppendLine($"  Output: {result.Output}");
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                message.AppendLine($"  Error: {result.Error}");
            }

            var level = result.Success ? LogLevel.Info : LogLevel.Error;
            await LogAsync(level, message.ToString());
        }

        /// <summary>
        /// Logs payload loading details
        /// </summary>
        /// <param name="payloadPath">Path to the payload</param>
        /// <param name="payloadType">Type of payload</param>
        /// <param name="result">Load result</param>
        public async Task LogPayloadLoadAsync(string payloadPath, string payloadType, PayloadLoadResult result)
        {
            var message = new StringBuilder();
            message.AppendLine($"Payload Load: {payloadPath}");
            message.AppendLine($"  Type: {payloadType}");
            message.AppendLine($"  Success: {result.Success}");

            if (!string.IsNullOrEmpty(result.Output))
            {
                message.AppendLine($"  Output: {result.Output}");
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                message.AppendLine($"  Error: {result.Error}");
            }

            var level = result.Success ? LogLevel.Info : LogLevel.Error;
            await LogAsync(level, message.ToString());
        }

        /// <summary>
        /// Logs configuration loading details
        /// </summary>
        /// <param name="configPath">Path to configuration file</param>
        /// <param name="config">Loaded configuration</param>
        public async Task LogConfigLoadAsync(string configPath, ExecutionChainConfig config)
        {
            var message = new StringBuilder();
            message.AppendLine($"Configuration Loaded: {configPath}");
            message.AppendLine($"  Name: {config.Name}");
            message.AppendLine($"  Description: {config.Description}");
            message.AppendLine($"  Techniques: {config.Techniques.Count}");
            message.AppendLine($"  Payload Type: {config.PayloadConfig.Type}");
            message.AppendLine($"  Payload Path: {config.PayloadConfig.Path}");

            await LogAsync(LogLevel.Info, message.ToString());
        }

        /// <summary>
        /// Logs obfuscation operations
        /// </summary>
        /// <param name="method">Obfuscation method</param>
        /// <param name="operation">Operation type (obfuscate/deobfuscate)</param>
        /// <param name="dataSize">Size of data processed</param>
        /// <param name="success">Whether operation was successful</param>
        public async Task LogObfuscationAsync(string method, string operation, int dataSize, bool success)
        {
            var message = $"Obfuscation {operation}: {method}, Data Size: {dataSize} bytes, Success: {success}";
            var level = success ? LogLevel.Debug : LogLevel.Warning;
            await LogAsync(level, message);
        }

        /// <summary>
        /// Creates a log entry with timestamp and formatting
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Optional exception</param>
        /// <returns>Formatted log entry</returns>
        private string CreateLogEntry(LogLevel level, string message, Exception? exception)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelString = level.ToString().ToUpper();
            var entry = $"[{timestamp}] [{levelString}] {message}";

            if (exception != null)
            {
                entry += $"\nException: {exception.Message}";
                entry += $"\nStackTrace: {exception.StackTrace}";
            }

            return entry;
        }

        /// <summary>
        /// Writes a log entry to file and console
        /// </summary>
        /// <param name="logEntry">Log entry to write</param>
        private async Task WriteLogEntryAsync(string logEntry)
        {
            lock (_lockObject)
            {
                // Write to console if enabled
                if (_enableConsoleOutput)
                {
                    Console.WriteLine(logEntry);
                }
            }

            // Write to file
            try
            {
                await File.AppendAllTextAsync(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // If we can't write to the log file, at least try to write to console
                if (_enableConsoleOutput)
                {
                    lock (_lockObject)
                    {
                        Console.WriteLine($"[ERROR] Failed to write to log file: {ex.Message}");
                        Console.WriteLine(logEntry);
                    }
                }
            }
        }

        /// <summary>
        /// Clears the log file
        /// </summary>
        public async Task ClearLogAsync()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    await File.WriteAllTextAsync(_logFilePath, string.Empty);
                    await InfoAsync("Log file cleared");
                }
            }
            catch (Exception ex)
            {
                await ErrorAsync("Failed to clear log file", ex);
            }
        }

        /// <summary>
        /// Gets log file statistics
        /// </summary>
        /// <returns>Log file statistics</returns>
        public async Task<LogFileStats> GetLogStatsAsync()
        {
            var stats = new LogFileStats();

            try
            {
                if (!File.Exists(_logFilePath))
                    return stats;

                var lines = await File.ReadAllLinesAsync(_logFilePath);
                stats.TotalLines = lines.Length;
                stats.FileSize = new FileInfo(_logFilePath).Length;

                foreach (var line in lines)
                {
                    if (line.Contains("[DEBUG]"))
                        stats.DebugCount++;
                    else if (line.Contains("[INFO]"))
                        stats.InfoCount++;
                    else if (line.Contains("[WARNING]"))
                        stats.WarningCount++;
                    else if (line.Contains("[ERROR]"))
                        stats.ErrorCount++;
                    else if (line.Contains("[CRITICAL]"))
                        stats.CriticalCount++;
                }
            }
            catch (Exception ex)
            {
                await ErrorAsync("Failed to get log statistics", ex);
            }

            return stats;
        }

        /// <summary>
        /// Exports log entries to a different format
        /// </summary>
        /// <param name="outputPath">Output file path</param>
        /// <param name="format">Export format</param>
        /// <param name="filter">Optional filter for log entries</param>
        public async Task ExportLogAsync(string outputPath, LogExportFormat format, Func<string, bool>? filter = null)
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    await ErrorAsync("Log file does not exist for export");
                    return;
                }

                var lines = await File.ReadAllLinesAsync(_logFilePath);
                var filteredLines = filter != null ? lines.Where(filter) : lines;

                switch (format)
                {
                    case LogExportFormat.CSV:
                        await ExportToCsvAsync(outputPath, filteredLines);
                        break;
                    case LogExportFormat.JSON:
                        await ExportToJsonAsync(outputPath, filteredLines);
                        break;
                    case LogExportFormat.TXT:
                        await File.WriteAllLinesAsync(outputPath, filteredLines);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported export format: {format}");
                }

                await InfoAsync($"Log exported to {outputPath} in {format} format");
            }
            catch (Exception ex)
            {
                await ErrorAsync($"Failed to export log to {outputPath}", ex);
            }
        }

        /// <summary>
        /// Exports log entries to CSV format
        /// </summary>
        /// <param name="outputPath">Output file path</param>
        /// <param name="lines">Log lines to export</param>
        private async Task ExportToCsvAsync(string outputPath, IEnumerable<string> lines)
        {
            var csvLines = new List<string> { "Timestamp,Level,Message" };

            foreach (var line in lines)
            {
                // Parse log line to extract components
                var parts = ParseLogLine(line);
                if (parts.Length >= 3)
                {
                    var csvLine = $"\"{parts[0]}\",\"{parts[1]}\",\"{parts[2].Replace("\"", "\"\"")}\"";
                    csvLines.Add(csvLine);
                }
            }

            await File.WriteAllLinesAsync(outputPath, csvLines);
        }

        /// <summary>
        /// Exports log entries to JSON format
        /// </summary>
        /// <param name="outputPath">Output file path</param>
        /// <param name="lines">Log lines to export</param>
        private async Task ExportToJsonAsync(string outputPath, IEnumerable<string> lines)
        {
            var logEntries = new List<object>();

            foreach (var line in lines)
            {
                var parts = ParseLogLine(line);
                if (parts.Length >= 3)
                {
                    logEntries.Add(new
                    {
                        Timestamp = parts[0],
                        Level = parts[1],
                        Message = parts[2]
                    });
                }
            }

            var json = System.Text.Json.JsonSerializer.Serialize(logEntries, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPath, json);
        }

        /// <summary>
        /// Parses a log line to extract timestamp, level, and message
        /// </summary>
        /// <param name="line">Log line to parse</param>
        /// <returns>Array containing timestamp, level, and message</returns>
        private string[] ParseLogLine(string line)
        {
            // Expected format: [timestamp] [LEVEL] message
            var parts = line.Split(']', 3);
            if (parts.Length >= 3)
            {
                var timestamp = parts[0].TrimStart('[');
                var level = parts[1].TrimStart('[').Trim();
                var message = parts[2].Trim();
                return new[] { timestamp, level, message };
            }

            return new[] { string.Empty, string.Empty, line };
        }
    }

    /// <summary>
    /// Log levels
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    /// <summary>
    /// Log export formats
    /// </summary>
    public enum LogExportFormat
    {
        CSV,
        JSON,
        TXT
    }

    /// <summary>
    /// Log file statistics
    /// </summary>
    public class LogFileStats
    {
        public long FileSize { get; set; }
        public int TotalLines { get; set; }
        public int DebugCount { get; set; }
        public int InfoCount { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public int CriticalCount { get; set; }
    }
} 