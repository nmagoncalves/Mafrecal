using System;
using System.IO;

namespace Mafrecal.WorkerService.Logging
{


    public static class Logger
    {
        private static readonly string LogFolder = Path.Combine(AppContext.BaseDirectory, "Logs");

        static Logger()
        {
            if (!Directory.Exists(LogFolder))
                Directory.CreateDirectory(LogFolder);
        }

        public static void Info(string message) => Write("INFO", message);
        public static void Error(string message, Exception ex = null) => Write("ERROR", message + (ex != null ? $" - {ex}" : ""));

        private static void Write(string level, string message)
        {
            try
            {
                string logFile = Path.Combine(LogFolder, $"{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}");
                //Console.WriteLine($"{DateTime.Now:HH:mm:ss} [{level}] {message}");
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Erro ao escrever log: " + ex);
            }
        }
    }

}
