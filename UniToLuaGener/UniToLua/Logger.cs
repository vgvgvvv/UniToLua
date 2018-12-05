using System;

namespace UniToLuaGener
{
    public class Logger
    {
        public static void Log(string log)
        {
            Console.Write(log);   
        }

        public static void Error(string error)
        {
            Console.Error.Write(error);
        }
    }
}