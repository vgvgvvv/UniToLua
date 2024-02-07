using System;
using System.Collections.Generic;
using System.IO;
using UniToLua.Common;

namespace UniToLuaGener
{
    public static class CommonAPI
    {
        public static List<Type> CommonAPITypes = new List<Type>()
        {
            typeof(Path),
            typeof(File),
            typeof(Directory),
            typeof(StringEx),
            typeof(FileEx),
            typeof(DirectoryEx),
            typeof(List<string>),
        };
    }
}