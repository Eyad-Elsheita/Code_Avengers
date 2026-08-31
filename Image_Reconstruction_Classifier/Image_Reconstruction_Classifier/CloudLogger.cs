namespace Image_Reconstruction_Classifier
{
    /// <summary>
    /// Writes timestamped, leveled log lines to standard error, so logs surface in Azure App
    /// Service's log stream regardless of console redirection.
    /// </summary>
    public static class CloudLogger
    {
        private static readonly string LogPrefix = "Image_Reconstruction_Classifier";

        // ============================================================
        // LOG — General neutral message
        // ============================================================
        /// <summary>
        /// Writes a neutral log line at the "LOG" level.
        /// </summary>
        /// <param name="toolName">Name of the calling tool or component, used as a log tag.</param>
        /// <param name="message">Message to log.</param>
        public static void Log(string toolName, string message)
        {
            var line = FormatLine("LOG", toolName, message);
            Console.Error.WriteLine(line);
        }

        // ============================================================
        // LOG INFO — Normal successful operations
        // ============================================================
        /// <summary>
        /// Writes a log line at the "INFO" level, for normal successful operations.
        /// </summary>
        /// <param name="toolName">Name of the calling tool or component, used as a log tag.</param>
        /// <param name="message">Message to log.</param>
        public static void LogInfo(string toolName, string message)
        {
            var line = FormatLine("INFO", toolName, message);
            Console.Error.WriteLine(line);
        }

        // ============================================================
        // LOG ERROR — Exceptions and failures
        // ============================================================
        /// <summary>
        /// Writes a log line at the "ERROR" level, optionally including an exception's message.
        /// </summary>
        /// <param name="toolName">Name of the calling tool or component, used as a log tag.</param>
        /// <param name="message">Message to log.</param>
        /// <param name="ex">Exception associated with the failure, if any.</param>
        public static void LogError(string toolName, string message, Exception? ex = null)
        {
            var line = FormatLine("ERROR", toolName, message);
            Console.Error.WriteLine(line);
            if (ex != null)
                Console.Error.WriteLine($"  Exception: {ex.Message}");
        }

        // ============================================================
        // LOG DEBUG — Detailed step-by-step info
        // ============================================================
        /// <summary>
        /// Writes a log line at the "DEBUG" level, for detailed step-by-step info.
        /// </summary>
        /// <param name="toolName">Name of the calling tool or component, used as a log tag.</param>
        /// <param name="message">Message to log.</param>
        public static void LogDebug(string toolName, string message)
        {
            var line = FormatLine("DEBUG", toolName, message);
            Console.Error.WriteLine(line);
        }

        // ============================================================
        // HELPER: Format log line
        // ============================================================
        private static string FormatLine(string level, string toolName, string message)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            return $"[{timestamp}] [{level,-5}] [{LogPrefix}.{toolName}] {message}";
        }
    }
}