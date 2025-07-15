using System;
using System.IO;
using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Events;

/*
--------------------------------
NuGet
Install-Package Serilog
Install-Package Serilog.Sinks.Console
Install-Package Serilog.Sinks.File
Install-Package Serilog.Enrichers.Thread
Install-Package Serilog.Sinks.Async

--------------------------------
{Exception}:
-If the log statement includes an Exception object, this renders the exception message + stack trace.
 If no exception is logged, it outputs nothing.
-example:
    try
    {
        throw new Exception("Something broke");
    }
    catch (Exception ex)
    {
        Logger.loge("Caught error", ex);
    }    

--------------------------------
//example:
// 初始化，只需要呼叫一次
Logger.Init(minLevel: LogEventLevel.Debug);

// 簡單記錄
Logger.logi("Application started");

// 記錄警告
Logger.logw("This is a warning");

// 記錄錯誤
Logger.loge("Something went wrong");

// 帶 Exception 的錯誤記錄
try
{
    throw new InvalidOperationException("Test exception");
}
catch (Exception ex)
{
    Logger.loge("Caught exception:", ex);
}

// 關閉日誌(程序結束時調用)
Logger.Close();
*/
namespace ProfitWin.Logging
{
    public static class Logger
    {
        private static bool _isInitialized = false;

        public static void Init(
            LogEventLevel minLevel = LogEventLevel.Debug,
            string generalLogFilePath = "logs/all.log",
            string errorLogFilePath = "logs/error.log",
            bool enableConsole = true,
            bool enableFile = true)
        {
            if (_isInitialized)
                return;

            var config = new LoggerConfiguration()
                .MinimumLevel.Is(minLevel)
                .Enrich.WithThreadId()
                .Enrich.FromLogContext();

            string outputTemplate = "[{Timestamp:yyyy/MM/dd HH:mm:ss.fff} {Level:u3}][{ThreadId} {CallerFile}:{CallerLine} {CallerMember}] {Message}{NewLine}{Exception}";

            if (enableConsole)
            {
                config = config.WriteTo.Console(outputTemplate: outputTemplate);
            }

            if (enableFile)
            {
                if (!string.IsNullOrWhiteSpace(generalLogFilePath))
                {
                    EnsureDirectoryExists(generalLogFilePath);
                    config = config.WriteTo.Async(a => a.File(
                        generalLogFilePath,
                        outputTemplate: outputTemplate,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 100));
                }

                if (!string.IsNullOrWhiteSpace(errorLogFilePath))
                {
                    EnsureDirectoryExists(errorLogFilePath);
                    config = config.WriteTo.Async(a => a.File(
                        errorLogFilePath,
                        restrictedToMinimumLevel: LogEventLevel.Warning,
                        outputTemplate: outputTemplate,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 100));
                }
            }

            Log.Logger = config.CreateLogger();
            _isInitialized = true;
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static ILogger WithCallerContext(string file, int line, string member)
        {
            return Log.ForContext("CallerFile", Path.GetFileName(file))
                      .ForContext("CallerLine", line)
                      .ForContext("CallerMember", member);
        }

        public static void logi(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            WithCallerContext(file, line, member).Information(message);
        }

        public static void logw(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            WithCallerContext(file, line, member).Warning(message);
        }

        public static void loge(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            WithCallerContext(file, line, member).Error(message);
        }

        // Exception overload for error logs
        public static void loge(string message, Exception ex,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            WithCallerContext(file, line, member).Error(ex, message);
        }

        public static void logv(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            WithCallerContext(file, line, member).Verbose(message);
        }

        public static void logd(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            WithCallerContext(file, line, member).Debug(message);
        }

        public static void logf(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            WithCallerContext(file, line, member).Fatal(message);
        }

        public static void Close()
        {
            Log.CloseAndFlush();
        }
    }
}
