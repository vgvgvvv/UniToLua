using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using UniToLua;

namespace UniLua
{
    public partial class LuaState
    {
        #region Call Func
        // 把指定名字的Lua函数放在栈顶，如果是成员函数（带:），还会接着把表压栈
        public bool GetFuncGlobal(string luaFunc, out bool classFunc)
        {
            if (string.IsNullOrEmpty(luaFunc))
            {
                classFunc = false;
                return false;
            }

            classFunc = luaFunc.Contains(":");
            string[] names = luaFunc.Split(".:".ToCharArray());
            API.GetGlobal(names[0]);
            if (API.IsNil(-1))
            {
                API.Remove(-1);
                return false;
            }

            for (int i = 1; i < names.Length; ++i)
            {
                if (!API.IsTable(-1))
                {
                    API.Remove(-1);
                    return false;
                }

                API.GetField(-1, names[i]);
                if (API.IsNil(-1))
                {
                    API.Remove(-1);
                    API.Remove(-1);
                    return false;
                }

                if (classFunc && i == names.Length - 1)
                {
                    API.PushValue(-2);
                    API.Remove(-3);
                }
                else
                    API.Remove(-2);
            }
            return true;
        }

        void DoPCall(int argNum, int retNum)
        {
            if (API.PCall(argNum, retNum, 0) != ThreadStatus.LUA_OK)
            {
                string msg = L_CheckString(API.GetTop());
                Log.Error(msg);
                API.Pop(1);
            }
        }

        // 没有返回值
        void PCall(int argNum)
        {
            DoPCall(argNum, 0);
        }

        // 返回一个值
        T PCall<T>(int argNum)
        {
            DoPCall(argNum, 1);

            int top = API.GetTop();
            var ret = CheckAny<T>(top);
            API.Pop(1);
            return ret;
        }

        Arg2<T1, T2> PCall<T1, T2>(int argNum)
        {
            DoPCall(argNum, 2);
            int top = API.GetTop();
            var ret1 = CheckAny<T1>(top - 1);
            var ret2 = CheckAny<T2>(top);
            API.Pop(2);
            return new Arg2<T1, T2>(ret1, ret2);
        }

        Arg3<T1, T2, T3> PCall<T1, T2, T3>(int argNum)
        {
            DoPCall(argNum, 3);
            int top = API.GetTop();
            var ret1 = CheckAny<T1>(top - 2);
            var ret2 = CheckAny<T2>(top - 1);
            var ret3 = CheckAny<T3>(top);
            API.Pop(3);
            return new Arg3<T1, T2, T3>(ret1, ret2, ret3);
        }

        public void CallFunc(string luaFunc)
        {
            bool classFunc;
            if (!GetFuncGlobal(luaFunc, out classFunc))
                return;

            PCall(classFunc ? 1 : 0);
        }

        public void CallFunc(string luaFunc, IArg arg)
        {
            bool classFunc;
            if (!GetFuncGlobal(luaFunc, out classFunc))
                return;

            arg.PushLua(this);
            PCall(classFunc ? arg.count + 1 : arg.count);
        }

        public void CallFunc<T1>(string luaFunc, T1 arg1)
        {
            CallFunc(luaFunc, (IArg)(new Arg1<T1>(arg1)));
        }

        public void CallFunc<T1, T2>(string luaFunc, T1 arg1, T2 arg2)
        {
            CallFunc(luaFunc, (IArg)(new Arg2<T1, T2>(arg1, arg2)));
        }

        public void CallFunc<T1, T2, T3>(string luaFunc, T1 arg1, T2 arg2, T3 arg3)
        {
            CallFunc(luaFunc, (IArg)(new Arg3<T1, T2, T3>(arg1, arg2, arg3)));
        }

        public T CallFuncR1<T>(string luaFunc)
        {
            bool classFunc;
            if (!GetFuncGlobal(luaFunc, out classFunc))
                return default(T);

            return PCall<T>(classFunc ? 1 : 0);
        }

        public T CallFuncR1<T>(string luaFunc, IArg arg)
        {
            bool classFunc;
            if (!GetFuncGlobal(luaFunc, out classFunc))
                return default(T);

            arg.PushLua(this);
            return PCall<T>(classFunc ? arg.count + 1 : arg.count);
        }

        public T CallFuncR1<T, T1>(string luaFunc, T1 arg1)
        {
            return CallFuncR1<T>(luaFunc, new Arg1<T1>(arg1));
        }

        public T CallFuncR1<T, T1, T2>(string luaFunc, T1 arg1, T2 arg2)
        {
            return CallFuncR1<T>(luaFunc, new Arg2<T1, T2>(arg1, arg2));
        }

        public T CallFuncR1<T, T1, T2, T3>(string luaFunc, T1 arg1, T2 arg2, T3 arg3)
        {
            return CallFuncR1<T>(luaFunc, new Arg3<T1, T2, T3>(arg1, arg2, arg3));
        }

        public Arg2<T1, T2> CallFuncR2<T1, T2>(string luaFunc)
        {
            bool classFunc;
            if (!GetFuncGlobal(luaFunc, out classFunc))
                return default(Arg2<T1, T2>);

            return PCall<T1, T2>(classFunc ? 1 : 0);
        }

        public Arg2<T1, T2> CallFuncR2<T1, T2>(string luaFunc, IArg arg)
        {
            bool classFunc;
            if (!GetFuncGlobal(luaFunc, out classFunc))
                return default(Arg2<T1, T2>);

            arg.PushLua(this);
            return PCall<T1, T2>(classFunc ? arg.count + 1 : arg.count);
        }

        public Arg3<T1, T2, T3> CallFuncR3<T1, T2, T3>(string luaFunc)
        {
            bool classFunc;
            if (!GetFuncGlobal(luaFunc, out classFunc))
                return default(Arg3<T1, T2, T3>);

            return PCall<T1, T2, T3>(classFunc ? 1 : 0);
        }

        public Arg3<T1, T2, T3> CallFuncR3<T1, T2, T3>(string luaFunc, IArg arg)
        {
            bool classFunc;
            if (!GetFuncGlobal(luaFunc, out classFunc))
                return default(Arg3<T1, T2, T3>);

            arg.PushLua(this);
            return PCall<T1, T2, T3>(classFunc ? arg.count + 1 : arg.count);
        }
        #endregion
    }
}
