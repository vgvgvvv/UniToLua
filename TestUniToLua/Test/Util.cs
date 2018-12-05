using System;
using System.IO;
using UniLua;
using UniLua.Tools;

namespace TestUniToLua
{
    public class Util
    {
        public static readonly string LuaPath =
            @"E:\Projects\CSProjects\UniLua\Project\UniToLua\TestUniToLua\Lua";

        public static LuaState InitTestEnv()
        {
            LuaFile.SetPathHook((fileName) => Path.Combine(LuaPath, fileName));
            ULDebug.Log = Console.WriteLine;
            ULDebug.LogError = Console.Error.WriteLine;

            var luaState = new LuaState();
            luaState.L_OpenLibs();
            luaState.OpenToLua();

            return luaState;
        }
    }
}