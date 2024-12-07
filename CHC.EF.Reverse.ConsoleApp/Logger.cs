using System;
using System.IO;

namespace CHC.EF.Reverse.ConsoleApp
{
    public class Logger
    {
        private readonly string _logPath;

        public Logger()
        {
            _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CodeGen.log");
        }

        public void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public void Error(string message, Exception ex = null)
        {
            WriteLog("ERROR", message + (ex == null ? "" : " EXCEPTION: " + ex.Message));
        }

        private void WriteLog(string level, string message)
        {
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            Console.WriteLine(logMessage);
            File.AppendAllText(_logPath, logMessage + Environment.NewLine);
        }
    }
}