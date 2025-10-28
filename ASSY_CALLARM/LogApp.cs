using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASSY_CALLARM
{
    public static class LogApp
    {
        private static readonly object _lock = new object();
        private static readonly ConcurrentQueue<(string level, string message, string type)> _logQueue = new ConcurrentQueue<(string, string, string)>();
        private static readonly Task _logProcessingTask;
        private static bool _isProcessing = false;
        private static string _logFilePath = "app.log";
        private static string _logFolder = "Logs";
        private static int _logDayDelete = 60; // Số ngày lưu log
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

        private const int MaxLogLines = 100;
        private static int _logIndex = 0;
        private static readonly string[] _logBuffer = new string[MaxLogLines];

        public static event Action LogAdded;
        static LogApp()
        {
            // Khởi tạo task xử lý log bất đồng bộ
            _logProcessingTask = Task.Run(ProcessLogQueue);
        }

        public static void SetDayDelete(int days)
        {
            _logDayDelete = days;
        }

        /// <summary>
        /// Ghi log thông tin
        /// </summary>
        public static void Info(string message, string type = "Log")
        {
            EnqueueLog("INFO", message, type);
        }
        public static void ClearLog()
        {
            lock (_lock)
            {
                Array.Clear(_logBuffer, 0, _logBuffer.Length);
                _logIndex = 0;

            }
        }
        /// <summary>
        /// Ghi log cảnh báo
        /// </summary>
        public static void Warn(string message)
        {
            EnqueueLog("WARN", message);
        }

        /// <summary>
        /// Ghi log lỗi
        /// </summary>
        public static void Error(string message)
        {
            EnqueueLog("ERROR", message);
        }

        private static void EnqueueLog(string level, string message, string type = "Log")
        {
            _logQueue.Enqueue((level, message, type));
        }

        private static async Task ProcessLogQueue()
        {
            while (true)
            {
                if (_logQueue.IsEmpty && !_isProcessing)
                {
                    await Task.Delay(100); // Chờ 100ms nếu hàng đợi trống
                    continue;
                }

                if (_logQueue.TryDequeue(out var logEntry))
                {
                    _isProcessing = true;
                    await WriteLogAsync(logEntry.level, logEntry.message, logEntry.type);
                    _isProcessing = false;
                    await Task.Delay(2);
                }
            }
        }

        private static async Task WriteLogAsync(string level, string message, string type = "LogApp")
        {
            try
            {
                lock (_lock)
                {
                    _logFolder = Path.Combine(BaseDir, "Logs");
                    Directory.CreateDirectory(_logFolder);
                    _logFilePath = $"{DateTime.Now:yyyy-MM-dd HH}_{type}.txt";
                    string logPath = Path.Combine(_logFolder, _logFilePath);
                    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

                    // Lưu vào buffer
                    _logBuffer[_logIndex] = line;
                    _logIndex = (_logIndex + 1) % MaxLogLines; // Vòng lại khi đầy
                    LogAdded?.Invoke();

                    File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
                    CleanOldLogs(_logDayDelete);
                }
            }
            catch
            {
                // Không throw lỗi để tránh crash app nếu ghi log bị lỗi
            }
        }

        private static void CleanOldLogs(int days)
        {
            try
            {
                var files = Directory.GetFiles(_logFolder, "*.txt", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    if (info.LastWriteTime < DateTime.Now.AddDays(-days))
                    {
                        info.Delete();
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi xóa file
            }
        }
        /// <summary>
        /// Lấy 100 dòng log gần nhất
        /// </summary>
        public static string[] GetLatestLogs()
        {
            lock (_lock)
            {
                int count = Math.Min(_logIndex, MaxLogLines);
                var logs = new string[count];
                Array.Copy(_logBuffer, logs, count);
                return logs;
            }
        }
    }
}
