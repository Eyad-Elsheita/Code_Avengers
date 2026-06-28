using System;
using System.IO;

namespace Image_Reconstruction_Classifier
{
    /// <summary>
    /// Simple logger that writes messages to both the console and a log file.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath = Environment.GetEnvironmentVariable("Log_Output_Path")
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");

        private static readonly object LockObj = new object();

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public static void LogInfo(string message) => WriteLog("INFO", message);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(string message) => WriteLog("ERROR", message);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        public static void LogDebug(string message) => WriteLog("DEBUG", message);

        /// <summary>
        /// Logs a general message with a custom level label.
        /// </summary>
        public static void Log(string level, string message) => WriteLog(level.ToUpper(), message);

        private static void WriteLog(string level, string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            // Write to console
            Console.WriteLine(logEntry);

            // Write to log file in a thread-safe manner
            lock (LockObj)
            {
                try
                {
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Logger] Failed to write to log file: {ex.Message}");
                }
            }
        }
    }
}
