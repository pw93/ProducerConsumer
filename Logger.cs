using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace WinFormsApp2
{
    public static class Logger
    {
        private static readonly object _lock = new object();  // lock object for thread safety

        private static string baseDir = @"c:\temp3\log"; // base log directory
        private static bool isFirstWrite = true;
        private static bool isWriteFile = true;

        private static string logDir;   // directory with timestamp
        private static string logFile;  // full path to log.txt
        private static string errorLogFile; // full path to log_e.txt

        public static void logi(string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            var log = FormatLog("info", message, file, member, line);
            WriteLog(log, "info");
        }

        public static void logw(string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            var log = FormatLog("WARN", message, file, member, line);
            WriteLog(log, "warn");
        }

        public static void loge(string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            var log = FormatLog("ERROR", message, file, member, line);
            WriteLog(log, "error");
        }

        private static string FormatLog(string level, string message, string file, string member, int line)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            string fileName = Path.GetFileName(file);
            return $"[{time} {level}][{fileName}:{line} {member}()] {message}";
        }

        private static void CreateLogDir()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            logDir = Path.Combine(baseDir, timestamp);

            Directory.CreateDirectory(logDir);

            logFile = Path.Combine(logDir, "log.txt");
            errorLogFile = Path.Combine(logDir, "log_e.txt");
        }

        private static void WriteLog(string formattedLog, string level)
        {
            lock (_lock)
            {
                // Write to console and files atomically
                Console.WriteLine(formattedLog);

                if (!isWriteFile)
                    return;

                if (isFirstWrite)
                {
                    CreateLogDir();
                    isFirstWrite = false;
                }

                try
                {
                    File.AppendAllText(logFile, formattedLog + Environment.NewLine);

                    if (level == "warn" || level == "error")
                    {
                        File.AppendAllText(errorLogFile, formattedLog + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    // Log exception to console (also inside lock to keep order)
                    Console.WriteLine("Failed to write log file: " + ex.Message);
                }
            }
        }
    }
}
