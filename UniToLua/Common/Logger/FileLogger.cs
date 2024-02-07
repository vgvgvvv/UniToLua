using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniToLua.Common
{
    public class FileLogger : ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Info;
        
        public bool Enable { get; set; } = true;

        private FileInfo file;
        private  StreamWriter Writer;
        private StringBuilder cache = new StringBuilder();

        public FileLogger(string filePath)
        {
            file = new FileInfo(filePath);
            Writer = file.AppendText();
            Writer.AutoFlush = true;
        }

        ~FileLogger()
        {
            Writer.Close();
        }

        public void Info(params object[] info)
        {
            Writer.WriteLine($"[Info]:{info.ArrConvertToString(':')}");
        }

        public void Debug(params object[] info)
        {
            Writer.WriteLine($"[Debug]:{info.ArrConvertToString(':')}");
        }

        public void Warning(params object[] info)
        {
            Writer.WriteLine($"[Warning]:{info.ArrConvertToString(':')}");
        }

        public void Error(params object[] info)
        {
            Writer.WriteLine($"[Error]:{info.ArrConvertToString(':')}");
        }

        public void Exception(Exception ex)
        {
            Writer.WriteLine($"[Exception]:{ex.ToString()}");
        }
    }
}
