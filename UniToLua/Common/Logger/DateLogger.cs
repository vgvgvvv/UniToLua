using System;

namespace Common
{
    public class DateLogger : ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Info;
        
        public bool Enable { get; set; } = true;

        private readonly ILogger RawLogger;

        public DateLogger(ILogger rawLogger)
        {
            RawLogger = rawLogger;
        }
        
        public void Info(params object[] info)
        {
            RawLogger.Info($"[{DateTime.Now :u}]", "[Info]", info.ArrConvertToString(':'));
        }

        public void Debug(params object[] info)
        {
            RawLogger.Debug($"[{DateTime.Now :u}]", "[Debug]", info.ArrConvertToString(':'));
        }

        public void Warning(params object[] info)
        {
            RawLogger.Warning($"[{DateTime.Now :u}]", "[Warning]", info.ArrConvertToString(':'));
        }

        public void Error(params object[] info)
        {
            RawLogger.Error($"[{DateTime.Now :u}]", "[Error]", info.ArrConvertToString(':'));
        }

        public void Exception(Exception ex)
        {
            RawLogger.Exception(ex);
        }
    }
}