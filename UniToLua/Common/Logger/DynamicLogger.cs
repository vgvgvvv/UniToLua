using System;

namespace UniToLua.Common
{
    public class DynamicLogger : ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Info;
        
        public bool Enable { get; set; } = true;
        
        private Action<string> LogAction;
        private Action<string> DebugAction;
        private Action<string> WarningAction;
        private Action<string> ErrorAction;
        private Action<Exception> ExceptionAction;
        
        public DynamicLogger(Action<string> logAction)
        {
            LogAction = logAction;
            DebugAction = logAction;
            WarningAction = logAction;
            ErrorAction = logAction;
            ExceptionAction = exception => logAction(exception.ToString());
        }

        public DynamicLogger(Action<string> logAction, Action<string> debugAction, Action<string> warningAction,
            Action<string> errorAction, Action<Exception> exceptionAction)
        {
            LogAction = logAction;
            DebugAction = debugAction;
            WarningAction = warningAction;
            ErrorAction = errorAction;
            ExceptionAction = exceptionAction;
        }
        
        public void Info(params object[] info)
        {
            LogAction?.Invoke($"[Info]:{info.ArrConvertToString(':')}");
        }

        public void Debug(params object[] info)
        {
            DebugAction?.Invoke($"[Debug]:{info.ArrConvertToString(':')}");
        }

        public void Warning(params object[] info)
        {
            WarningAction?.Invoke($"[Warning]:{info.ArrConvertToString(':')}");
        }

        public void Error(params object[] info)
        {
            ErrorAction?.Invoke($"[Error]:{info.ArrConvertToString(':')}");
        }

        public void Exception(Exception ex)
        {
            ExceptionAction?.Invoke(ex);
        }
    }
}