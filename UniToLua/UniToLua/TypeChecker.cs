using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace UniLua
{
    public partial class LuaState
    {

        public bool CheckType<T1>(int pos)
        {
            return TypeChecker<T1>.Check(this, pos);
        }

        public bool CheckType<T1, T2>(int pos)
        {
            return TypeChecker<T1>.Check(this, pos) &&
                   TypeChecker<T2>.Check(this, pos + 1);
        }

        public bool CheckType<T1, T2, T3>(int pos)
        {
            return TypeChecker<T1>.Check(this, pos) &&
                   TypeChecker<T2>.Check(this, pos + 1) &&
                   TypeChecker<T3>.Check(this, pos + 2);
        }

        public bool CheckType<T1, T2, T3, T4>(int pos)
        {
            return TypeChecker<T1>.Check(this, pos) &&
                   TypeChecker<T2>.Check(this, pos + 1) &&
                   TypeChecker<T3>.Check(this, pos + 2) &&
                   TypeChecker<T4>.Check(this, pos + 3);
        }

        public bool CheckType<T1, T2, T3, T4, T5>(int pos)
        {
            return TypeChecker<T1>.Check(this, pos) &&
                   TypeChecker<T2>.Check(this, pos + 1) &&
                   TypeChecker<T3>.Check(this, pos + 2) &&
                   TypeChecker<T4>.Check(this, pos + 3) &&
                   TypeChecker<T5>.Check(this, pos + 4);
        }

        public bool CheckType<T1, T2, T3, T4, T5, T6>(int pos)
        {
            return TypeChecker<T1>.Check(this, pos) &&
                   TypeChecker<T2>.Check(this, pos + 1) &&
                   TypeChecker<T3>.Check(this, pos + 2) &&
                   TypeChecker<T4>.Check(this, pos + 3) &&
                   TypeChecker<T5>.Check(this, pos + 4) &&
                   TypeChecker<T6>.Check(this, pos + 5);
        }

        public bool CheckType<T1, T2, T3, T4, T5, T6, T7>(int pos)
        {
            return TypeChecker<T1>.Check(this, pos) &&
                   TypeChecker<T2>.Check(this, pos + 1) &&
                   TypeChecker<T3>.Check(this, pos + 2) &&
                   TypeChecker<T4>.Check(this, pos + 3) &&
                   TypeChecker<T5>.Check(this, pos + 4) &&
                   TypeChecker<T6>.Check(this, pos + 5) &&
                   TypeChecker<T7>.Check(this, pos + 6);
        }

        public bool CheckType<T1, T2, T3, T4, T5, T6, T7, T8>(int pos)
        {
            return TypeChecker<T1>.Check(this, pos) &&
                   TypeChecker<T2>.Check(this, pos + 1) &&
                   TypeChecker<T3>.Check(this, pos + 2) &&
                   TypeChecker<T4>.Check(this, pos + 3) &&
                   TypeChecker<T5>.Check(this, pos + 4) &&
                   TypeChecker<T6>.Check(this, pos + 5) &&
                   TypeChecker<T7>.Check(this, pos + 6) &&
                   TypeChecker<T8>.Check(this, pos + 7);
        }

        public bool CheckType<T1, T2, T3, T4, T5, T6, T7, T8, T9>(int pos)
        {
            return TypeChecker<T1>.Check(this, pos) &&
                   TypeChecker<T2>.Check(this, pos + 1) &&
                   TypeChecker<T3>.Check(this, pos + 2) &&
                   TypeChecker<T4>.Check(this, pos + 3) &&
                   TypeChecker<T5>.Check(this, pos + 4) &&
                   TypeChecker<T6>.Check(this, pos + 5) &&
                   TypeChecker<T7>.Check(this, pos + 6) &&
                   TypeChecker<T8>.Check(this, pos + 7) &&
                   TypeChecker<T9>.Check(this, pos + 8);
        }

        /// <summary>
        /// 用于类型检查
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class TypeChecker<T>
        {
            public static Func<LuaState, int, bool> Check = DefaultCheck;
            public static Type Type = typeof(T);
            public static bool IsValueType = Type.IsValueType;
            public static bool IsArray = Type.IsArray || Type == typeof(System.Array);

            public static bool IsNumberType = IsNumber();
            public static bool IsBoolType = Type == typeof(bool) || IsNumberType;
            public static bool IsStringType = Type == typeof(string);

            private static int canBeNil = -1;//0 不可为空 1 可为空

            public static void Init(Func<LuaState, int, bool> check)
            {
                Check = check;
            }

            private static bool DefaultCheck(LuaState L, int pos)
            {
                StkId addr;
                if (!L.Index2Addr(pos, out addr))
                {
                    return false;
                }

                var value = addr.V;

                if (value.TtIsNil())
                {
                    return true;
                }

                string errMsg = string.Empty;
                var luaType = (LuaType)value.Tt;
                bool result = true;
                switch (luaType)
                {
                    case LuaType.LUA_TNIL:
                        result = true;
                        break;
                    case LuaType.LUA_TNUMBER:
                    case LuaType.LUA_TUINT64:
                        result = IsNumberType;
                        break;
                    case LuaType.LUA_TBOOLEAN:
                        result = IsBoolType;
                        break;
                    case LuaType.LUA_TSTRING:
                        // 任何类型都可以tostring，所以总是为true
                        result = true;
                        break;
                    case LuaType.LUA_TTABLE:
                        result = value.OValue is T ||
                               IsArray ||
                               (Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(List<>)) ||
                               Type == typeof(ArrayList) ||
                               (Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(HashSet<>)) ||
                                Type == typeof(Hashtable) ||
                               (Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) ||
                                CheckClassFromTable<T>(L, value.OValue, pos, ref errMsg);
                            break;
                    case LuaType.LUA_TFUNCTION:
                        result = value.OValue is LuaLClosureValue;
                        break;
                    case LuaType.LUA_TLIGHTUSERDATA:
                        result = value.OValue is T;
                        break;
                    case LuaType.LUA_TUSERDATA:
                        if (value.OValue is LuaUserDataValue userdata)
                        {
                            result = userdata.Value is T;
                        }
                        else
                        {
                            result = false;
                        }
                        break;
                    default:
                        result = false;
                        break;
                }

                if (result == false)
                {
                    L.L_Error($"Check failed !!!! {luaType.ToString()} to {typeof(T).FullName} failed!!!! {errMsg}");
                }
                return result;

            }

            private static bool CanBeNil()
            {
                if (canBeNil != -1)
                {
                    return canBeNil == 0;
                }

                if (!IsValueType)
                {
                    canBeNil = 1;
                    return true;
                }

                if (Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    canBeNil = 1;
                    return true;
                }

                canBeNil = 0;
                return false;
            }

            private static bool IsNumber()
            {
                return Type == typeof(short) || Type == typeof(ushort) ||
                       Type == typeof(int) || Type == typeof(uint) ||
                       Type == typeof(long) || Type == typeof(ulong) ||
                        Type == typeof(float) || Type == typeof(double);
            }

            private static bool CheckClassFromTable<T1>(LuaState L, object obj, int pos, ref string errMsg)
            {
                if (obj is LuaTable table)
                {
                    var cons = typeof(T1).GetConstructor(new Type[0]);
                    if (cons != null)
                    {
                        return true;
                    }
                    else
                    {
                        errMsg = "when convert obj from table, obj 's new() cannot have any param!!!";
                    }

                }
                else
                {
                    errMsg = "obj must be a table!!!";
                }

                return false;
            }

        }

    }

    
}