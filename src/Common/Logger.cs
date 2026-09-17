using System;
using System.IO;

namespace IdeaMemoryManager.Common
{
    public static class Logger
    {
        private static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IdeaMemoryManager", "logs");

        static Logger()
        {
            try { Directory.CreateDirectory(LogDir); } catch { }
        }

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message, Exception ex = null)
        {
            string detail = ex != null ? $"{message}\n{ex}" : message;
            Write("ERROR", detail);
        }

        private static void Write(string level, string message)
        {
            try
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                string path = Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(path, line);
            }
            catch { }
        }
    }
}