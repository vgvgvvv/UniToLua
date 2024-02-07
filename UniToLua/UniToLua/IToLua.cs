using System;
using System.Collections.Generic;
using UniToLua;

namespace UniLua
{
    public partial interface ILuaState : IToLua
    {
    }

    public interface IToLua
    {
        void OpenToLua();

        void NewTable(string tableName, int index = -3);

        bool BeginModule(string name);

        void EndModule();

        int BeginClass(Type bindClass, Type baseClass);

        void EndClass();

        int BeginStaticLib(string staticLibName);

        void EndStaticLib();

        int BeginEnum(Type enumType);

        void EndEnum();

        void RegFunction(string funcName, CSharpFunctionDelegate func);

        void RegVar(string name, CSharpFunctionDelegate get, CSharpFunctionDelegate set);

        #region Check

        void PushInt64(long value);

        void PushAny<T>(T value);

        void PushAny(object value, Type type);

        void PushArray(Array array, int start = 0, int count = int.MaxValue);

        void PushEnumerableT<T>(IEnumerable<T> enumerable);

        void PushEnumerable(System.Collections.IEnumerable enumerable);

        void PushListT<T>(IList<T> list, int start = 0, int count = int.MaxValue);

        void PushList(System.Collections.IList list, int start = 0, int count = int.MaxValue);

        void PushDictionaryT<K, V>(IDictionary<K, V> dictionary);

        void PushDictionary(System.Collections.IDictionary dictionary);

        void PushTableItem<T>(int index, T value);

        T CheckObject<T>(int arg, bool notNull = true);

        T[] GetArray<T>(int arg);

        Array GetArray(int arg, Type t);

        T CheckAny<T>(int arg);

        object CheckAny(int arg, Type type, bool convertLuaTable = false);

        T GetAny<T>(TValue v);

        object GetAny(TValue v, Type t, bool convertLuaTable = false);

        #endregion

        #region CheckType

        bool CheckNum(int count);
        bool CheckRange(int min, int max);
        void DumpStack();

        bool CheckType<T1>(int pos);
        bool CheckType<T1, T2>(int pos);
        bool CheckType<T1, T2, T3>(int pos);
        bool CheckType<T1, T2, T3, T4>(int pos);
        bool CheckType<T1, T2, T3, T4, T5>(int pos);
        bool CheckType<T1, T2, T3, T4, T5, T6>(int pos);
        bool CheckType<T1, T2, T3, T4, T5, T6, T7>(int pos);
        bool CheckType<T1, T2, T3, T4, T5, T6, T7, T8>(int pos);
        bool CheckType<T1, T2, T3, T4, T5, T6, T7, T8, T9>(int pos);

        #endregion

        #region CallFunction

        void CallFunc(string luaFunc);

        void CallFunc(string luaFunc, IArg arg);

        void CallFunc<T1>(string luaFunc, T1 arg1);

        void CallFunc<T1, T2>(string luaFunc, T1 arg1, T2 arg2);

        void CallFunc<T1, T2, T3>(string luaFunc, T1 arg1, T2 arg2, T3 arg3);

        T CallFuncR1<T>(string luaFunc);

        T CallFuncR1<T>(string luaFunc, IArg arg);

        T CallFuncR1<T, T1>(string luaFunc, T1 arg1);

        T CallFuncR1<T, T1, T2>(string luaFunc, T1 arg1, T2 arg2);

        T CallFuncR1<T, T1, T2, T3>(string luaFunc, T1 arg1, T2 arg2, T3 arg3);

        Arg2<T1, T2> CallFuncR2<T1, T2>(string luaFunc);

        Arg2<T1, T2> CallFuncR2<T1, T2>(string luaFunc, IArg arg);

        Arg3<T1, T2, T3> CallFuncR3<T1, T2, T3>(string luaFunc);

        Arg3<T1, T2, T3> CallFuncR3<T1, T2, T3>(string luaFunc, IArg arg);

        #endregion


        #region LuaOperation

        bool Require(string moduleName, out object table);
        
        bool TryGetAnyFromLua(string name, Type type, out object result);

        bool TryGetAnyFromLua<T>(string name, out T result);

        bool SetAnyToLua(string name, object value, Type type, bool createTableIfNil = true);

        bool SetAnyToLua<T>(string name, T value, bool createTableIfNil = true);

        #endregion
    }
}