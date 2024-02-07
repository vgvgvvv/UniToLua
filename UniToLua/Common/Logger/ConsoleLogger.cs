using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ConsoleLogger : ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Info;
        
        public bool Enable { get; set; } = true;

        public void Info(params object[] info)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[Info]:{info.ArrConvertToString(':')}");
        }

        public void Debug(params object[] info)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[Debug]:{info.ArrConvertToString(':')}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void Warning(params object[] info)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Warning]:{info.ArrConvertToString(':')}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void Error(params object[] info)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error]:{info.ArrConvertToString(':')}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void Exception(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.ToString());
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
