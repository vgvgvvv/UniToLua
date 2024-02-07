using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class CombineLogger : ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Info;
        
        public bool Enable { get; set; } = true;
        
        public List<ILogger> Loggers { get; } = new List<ILogger>();

        public CombineLogger(params ILogger[] loggers)
        {
            Loggers.AddRange(loggers);
        }

        public void Info(params object[] info)
        {
            foreach (var logger in Loggers)
            {
                if (logger.Level > LogLevel.Info)
                {
                    continue;
                }
                logger.Info(info);
            }
        }

        public void Debug(params object[] info)
        {
            foreach (var logger in Loggers)
            {
                if (logger.Level > LogLevel.Debug)
                {
                    continue;
                }
                logger.Debug(info);
            }
        }

        public void Warning(params object[] info)
        {
            foreach (var logger in Loggers)
            {
                if (logger.Level > LogLevel.Warning)
                {
                    continue;
                }
                logger.Warning(info);
            }
        }

        public void Error(params object[] info)
        {
            foreach (var logger in Loggers)
            {
                if (logger.Level > LogLevel.Error)
                {
                    continue;
                }
                logger.Error(info);
            }
        }

        public void Exception(Exception ex)
        {
            foreach (var logger in Loggers)
            {
                if (logger.Level > LogLevel.Exception)
                {
                    continue;
                }
                logger.Exception(ex);
            }
        }
    }
}
