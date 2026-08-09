namespace Image_Reconstruction_Classifier
{
    public static class CloudLogger
    {
        private static readonly string LogPrefix = "Image_Reconstruction_Classifier";

        // ============================================================
        // LOG — General neutral message
        // ============================================================
        public static void Log(string toolName, string message)
        {
            var line = FormatLine("LOG", toolName, message);
            Console.Error.WriteLine(line);
        }

        // ============================================================
        // LOG INFO — Normal successful operations
        // ============================================================
        public static void LogInfo(string toolName, string message)
        {
            var line = FormatLine("INFO", toolName, message);
            Console.Error.WriteLine(line);
        }

        // ============================================================
        // LOG ERROR — Exceptions and failures
        // ============================================================
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