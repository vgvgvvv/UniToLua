using System;
using NUnit.Framework;
using UniLua;
using UniToLua;
using UniToLuaGener;

namespace TestUniToLua
{
    public class TestLuaWrapGener
    {
        [Test]
        public void TestGenWrap()
        {
            var dllPath = @"E:\Projects\CSProjects\UniLua\Project\UniToLua\TestUniToLua\bin\Debug\TestUniToLua.dll";
            var outputPath = @"E:\Projects\CSProjects\UniLua\Project\UniToLua\TestUniToLua\WrapClasses"; 
            ExportToLua.GenAll(dllPath, outputPath);
        }

        [Test]
        public void TestAutoBindLua()
        {
            LuaState state = Util.InitTestEnv();
            //LuaBinder.Bind(state);

            if (state.L_DoFile("TestAutoRegister.lua") != ThreadStatus.LUA_OK)
            {
                Console.WriteLine(state.L_CheckString(-1));
            }

        }
    }
}