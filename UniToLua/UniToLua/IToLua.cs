using System;

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

        int BeginEnum(string enumName);

        void EndEnum();

        void RegFunction(string funcName, CSharpFunctionDelegate func);

        void RegVar(string name, CSharpFunctionDelegate get, CSharpFunctionDelegate set);


        #region Check

        void PushObject<T>(T obj);

        void PushInt64(long value);

        void PushValue<T>(T value);

        T CheckObject<T>(int arg, bool notNull = true);

        T[] CheckArray<T>(int arg);

        T CheckValue<T>(int arg);

        T GetValue<T>(TValue v);

        #endregion

        #region CheckType

        bool CheckNum(int count);
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

    }
}