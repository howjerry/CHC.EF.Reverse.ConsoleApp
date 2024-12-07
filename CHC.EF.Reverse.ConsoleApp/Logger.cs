namespace CHC.EF.Reverse.ConsoleApp
{
    /// <summary>
    /// 負責記錄程式碼生成過程中的日誌。
    /// </summary>
    public class Logger
    {
        private readonly string _logPath;

        /// <summary>
        /// 初始化 Logger 類別的新實例。
        /// </summary>
        public Logger()
        {
            // 設定日誌檔案路徑。
            _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CodeGen.log");
        }

        /// <summary>
        /// 記錄資訊級別的日誌。
        /// </summary>
        /// <param name="message">日誌訊息。</param>
        public void Info(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// 記錄錯誤級別的日誌。
        /// </summary>
        /// <param name="message">日誌訊息。</param>
        /// <param name="ex">異常資訊（可選）。</param>
        public void Error(string message, Exception ex = null)
        {
            WriteLog("ERROR", message + (ex == null ? "" : " EXCEPTION: " + ex.Message));
        }

        /// <summary>
        /// 將日誌訊息寫入到控制台和日誌檔案。
        /// </summary>
        /// <param name="level">日誌級別。</param>
        /// <param name="message">日誌訊息。</param>
        private void WriteLog(string level, string message)
        {
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            Console.WriteLine(logMessage);
            File.AppendAllText(_logPath, logMessage + Environment.NewLine);
        }
    }
}