using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniToLua.Common
{
    public enum LogLevel
    {
        Info,
        Debug,
        Warning,
        Error,
        Exception
    }

    public class Log
    {
        public static ILogger Logger = new ConsoleLogger().WithDate();
        
        public static bool Enable
        {
            get => Logger.Enable;
            set => Logger.Enable = value;
        }

        public static void SetLogger(ILogger logger)
        {
            Logger = logger;
        }

        public static void SetLevel(LogLevel level)
        {
            Logger.Level = level;
        }

        public static void Info(params object[] info)
        {
            if (Logger.Level > LogLevel.Info)
            {
                return;
            }
            Logger.Info(info);
        }

        public static void Debug(params object[] info)
        {
            if (Logger.Level > LogLevel.Debug)
            {
                return;
            }
            Logger.Debug(info);
        }

        public static void Warning(params object[] info)
        {
            if (Logger.Level > LogLevel.Warning)
            {
                return;
            }
            Logger.Warning(info);
        }

        public static void Error(params object[] info)
        {
            if (Logger.Level > LogLevel.Error)
            {
                return;
            }
            Logger.Error(info);
        }

        public static void Exception(Exception ex)
        {
            if (Logger.Level > LogLevel.Exception)
            {
                return;
            }
            Logger.Exception(ex);
        }

        public static void CombineLoggers(params ILogger[] loggers)
        {
            Logger = new CombineLogger(loggers);
        }

        public static void AppendLogger(ILogger logger)
        {
            if (Logger is CombineLogger combined)
            {
                combined.Loggers.Add(logger);
            }
            else
            {
                Logger = new CombineLogger(Logger, logger);
            }
        }
    }

    public static class LogExtension
    {
        public static ILogger WithDate(this ILogger logger)
        {
            return new DateLogger(logger);
        }
    }

    public interface ILogger
    {
        LogLevel Level { get; set; } 
        bool Enable { get; set; }
        void Info(params object[] info);
        void Debug(params object[] info);
        void Warning(params object[] info);
        void Error(params object[] info);
        void Exception(Exception ex);
    }

}
