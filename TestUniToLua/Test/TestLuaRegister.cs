using System;
using NUnit.Framework;
using UniLua;

namespace TestUniToLua
{
    public enum HelloEnum
    {
        ENUM_A,
        ENUM_B
    }

    public static class HelloStaticLib
    {
        public static string value = "hoho";

        public static string Concat(string str1, string str2)
        {
            return str1 + str2;
        }
    }

    public class TestBaseClass
    {
        public int baseValue = 200;

        public int Sub(int a, int b)
        {
            return baseValue + a - b;
        }
    }

    public class TestClass : TestBaseClass
    {
        public int value = 100;

        public int Add(int a, int b)
        {
            return value + a + b;
        }

    }

    public class TestLuaRegister
    {
        [Test]
        public void TestLua()
        {
            LuaState state = Util.InitTestEnv();
            if (state.L_DoFile("TestUniLua.lua") != ThreadStatus.LUA_OK)
            {
                Console.WriteLine(state.L_CheckString(-1));
            }
        }

        [Test]
        public void TestRegisterModule()
        {
            LuaState state = Util.InitTestEnv();
           
            state.BeginModule(null);
            state.BeginModule("Test");
            state.BeginModule("HHH");
            state.EndModule();
            state.EndModule();
            state.EndModule();

            
            if (state.L_DoFile("TestLuaRegister.lua") != ThreadStatus.LUA_OK)
            {
                Console.WriteLine(state.L_CheckString(-1));
            }
        }


        [Test]
        public void TestRegisterEnum()
        {
            LuaState state = Util.InitTestEnv();
            state.BeginModule(null);
            state.BeginModule("Test");
            state.BeginEnum("Hello");
            state.RegVar("ENUM_A", GetA, null);
            state.RegVar("ENUM_B", GetB, null);
            state.EndEnum();
            state.EndModule();
            state.EndModule();

            if (state.L_DoFile("TestLuaRegisterEnum.lua") != ThreadStatus.LUA_OK)
            {
                Console.WriteLine(state.L_CheckString(-1));
            }
        }

        private int GetA(ILuaState state)
        {
            state.PushLightUserData(HelloEnum.ENUM_A);
            return 1;
        }

        private int GetB(ILuaState state)
        {
            state.PushLightUserData(HelloEnum.ENUM_B);
            return 1;
        }

        [Test]
        public void TestRegisterStaticLib()
        {
            LuaState state = Util.InitTestEnv();
            state.BeginModule(null);
            state.BeginModule("Test");
            state.BeginStaticLib("HelloStaticLib");
            state.RegVar("value", HelloStatic_get_value, HelloStatic_set_value);
            state.RegFunction("Concat", HelloStatic_Concat);
            state.EndStaticLib();
            state.EndModule();
            state.EndModule();
            if (state.L_DoFile("TestLuaRegisterStaticLib.lua") != ThreadStatus.LUA_OK)
            {
                Console.WriteLine(state.L_CheckString(-1));
            }
        }

        private int HelloStatic_get_value(ILuaState L)
        {
            L.PushString(HelloStaticLib.value);
            return 1;
        }

        private int HelloStatic_set_value(ILuaState L)
        {
            var result = L.L_CheckString(-1);
            HelloStaticLib.value = result;
            return 1;
        }

        private int HelloStatic_Concat(ILuaState L)
        {
            var str1 = L.L_CheckString(-1);
            var str2 = L.L_CheckString(-2);
            L.PushString(HelloStaticLib.Concat(str1, str2));
            return 1;
        }

        [Test]
        public void TestRegisterClass()
        {
            LuaState state = Util.InitTestEnv();
            state.BeginModule(null);
            state.BeginModule("Test");
            state.BeginClass(typeof(TestClass), typeof(TestBaseClass));
            state.RegFunction("New", Test_TestClass_New);
            state.RegFunction("Add", Test_TestClass_Add);
            state.RegVar("value", Test_TestClass_get_var, Test_TestClass_set_var);
            state.EndClass();
            state.EndModule();
            state.EndModule();

            if (state.L_DoFile("TestLuaRegisterClass.lua") != ThreadStatus.LUA_OK)
            {
                Console.WriteLine(state.L_CheckString(-1));
            }
        }

        private int Test_TestClass_New(ILuaState L)
        {
            L.PushValue(new TestClass());
            return 1;
        }

        private int Test_TestClass_Add(ILuaState L)
        {
            var obj = (TestClass)L.ToObject(1);
            var arg0 = L.L_CheckInteger(2);
            var arg1 = L.L_CheckInteger(3);
            L.PushInteger(obj.Add(arg0, arg1));
            return 1;
        }


        public int Test_TestClass_get_var(ILuaState L)
        {
            var obj = (TestClass) L.ToObject(1);
            L.PushInteger(obj.value);
            return 1;
        }

        public int Test_TestClass_set_var(ILuaState L)
        {
            var obj = (TestClass)L.ToObject(1);
            obj.value = L.L_CheckInteger(2);
            return 0;
        }

    }
}
