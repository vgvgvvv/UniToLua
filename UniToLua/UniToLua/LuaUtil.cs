using System;

namespace UniLua
{
    public partial class LuaState
    {
        public void NewTable(string tableName, int index = -3)
        {
            API.PushString(tableName);
            API.NewTable();
            API.RawSet(index);
        }

        #region Push

        public void PushObject<T>(T obj)
        {
            if (obj == null)
            {
                API.PushNil();
            }
            else
            {
                var type = typeof(T);
                var reference = ClassMetaRefDict[type.Name];
                API.PushLightUserData(obj);
                GetRef(reference);
                API.SetMetaTable(-2);
            }
        }

        public void PushInt64(long value)
        {
            unchecked
            {
                API.PushUInt64((ulong)value);
            }
        }

        public void PushValue<T>(T value)
        {
            var t = value != null ? value.GetType() : typeof(T);
            if (t == typeof(sbyte) || t == typeof(byte) ||
                t == typeof(short) || t == typeof(ushort) ||
                t == typeof(char))
                API.PushInteger((int)Convert.ChangeType(value, typeof(int)));
            else if (t == typeof(int))
                API.PushInteger((int)(object)value);
            else if (t == typeof(uint))
                API.PushUnsigned((uint)(object)value);
            else if (t == typeof(long))
                PushInt64((long)(object)value);
            else if (t == typeof(ulong))
                API.PushUInt64((ulong)(object)value);
            else if (t == typeof(bool))
                API.PushBoolean((bool)(object)value);
            else if (t == typeof(float))
                API.PushNumber((double)Convert.ChangeType(value, typeof(double)));
            else if (t == typeof(double))
                API.PushNumber((double)(object)value);
            else if (t == typeof(string))
                API.PushString(value.ToString());
            else if (t.IsEnum)
                PushValue(Convert.ChangeType(value, Enum.GetUnderlyingType(t)));
            else
                PushObject(value);
        }

        #endregion

        #region Check

        public T CheckObject<T>(int arg, bool notNull = true)
        {
            object obj = API.ToObject(arg);
            if ((obj == null || !(obj is T)) && !notNull)
                return default(T);

            T tobj = (T)obj;
            L_ArgCheck(tobj != null, arg, typeof(T).ToString() + " expected, got " + (obj == null ? "null" : obj.GetType().ToString()));
            return tobj;
        }

        public T[] CheckArray<T>(int arg)
        {
            LuaTable table = CheckObject<LuaTable>(arg, false);
            if (table == null)
                return null;

            T[] arr = new T[table.Length];
            for (int i = 0; i < table.Length; ++i)
            {
                arr[i] = GetValue<T>(table.GetInt(i + 1).V);
            }
            return arr;
        }

        public T CheckValue<T>(int arg)
        {
            StkId addr;
            if (!Index2Addr(arg, out addr))
                return default(T);

            return GetValue<T>(addr.V);
        }

        public T GetValue<T>(TValue v)
        {
            var t = typeof(T);
            if (t.IsEnum)
                t = Enum.GetUnderlyingType(t);

            switch ((LuaType)v.Tt)
            {
                case LuaType.LUA_TNIL:
                    return default(T);
                case LuaType.LUA_TBOOLEAN:
                    bool b = !v.TtIsNil() && (!v.TtIsBoolean() || v.BValue());
                    return (T)(object)b;
                case LuaType.LUA_TLIGHTUSERDATA:
                    return (T)v.OValue;
                case LuaType.LUA_TNUMBER:
                    if (t == typeof(object) &&
                        v.NValue >= int.MinValue && v.NValue <= int.MaxValue &&
                        v.NValue == (int)v.NValue)
                        t = typeof(int);
                    return (T)Convert.ChangeType(v.NValue, t);
                case LuaType.LUA_TSTRING:
                    return (T)v.OValue;
                case LuaType.LUA_TTABLE:
                case LuaType.LUA_TFUNCTION:
                case LuaType.LUA_TTHREAD:
                    return (T)v.OValue;
                case LuaType.LUA_TUINT64:
                    return (T)Convert.ChangeType(v.UInt64Value, t);
                default:
                    return default(T);
            }
        }

        #endregion

        #region StackCheck

        public bool CheckNum(int count)
        {
            return API.GetTop() == count;
        }

        #endregion

        public void GetRef(int reference)
        {
            API.RawGetI(LuaDef.LUA_REGISTRYINDEX, reference);
        }
    }
}