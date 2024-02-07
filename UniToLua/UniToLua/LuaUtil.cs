using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UniToLua.Common;
using Newtonsoft.Json.Linq;
using UniToLua;

namespace UniLua
{
    /// <summary>
    /// 用于进入Table域
    /// </summary>
    public class TableScope : IDisposable
    {
        private ILuaState L;
        public string ScopeName { get; }
        public bool IsValid { get; }
        public TableScope(ILuaState L, string name, bool creatIfNil = true)
        {
            ScopeName = name;
            this.L = L;
            if (name == null)
            {
                L.PushGlobalTable(); // global
                IsValid = true;
            }
            else
            {
                L.PushString(name); // stack name
                L.RawGet(-2); // stack value
                
                if (L.IsNil(-1)) // stack nil
                {
                    if (creatIfNil)
                    {
                        L.Pop(1);       //stack
                        L.NewTable();     //stack table
                        L.PushString(name); //stack table name
                        L.PushValue(-2); // stack table name table
                        L.RawSet(-4);  //stack table
                        IsValid = true;
                    }
                    else
                    {
                        IsValid = false;
                    }
                }
                else if (L.IsTable(-1))//stack table
                {
                    IsValid = true;
                }
                else
                {
                    IsValid = false;
                }
            }
        }

        public void Dispose()
        {
            L.Pop(1);
        }
    }

    public partial class LuaState
    {
        //用于获取lclousue从Lua到C#的方法
        public Dictionary<Type, Func<LuaState, LuaLClosureValue, Delegate>> createLuaDelegateDict { get; }= new Dictionary<Type, Func<LuaState, LuaLClosureValue, Delegate>>();
        // 用于获取c#方法push到Lua的方法
        public Dictionary<Type, Func<Delegate, CSharpFunctionDelegate>> csFunctionDelegateDict { get; }= new Dictionary<Type, Func<Delegate, CSharpFunctionDelegate>>();

        private Dictionary<Type, Action<IToLua, object>> CustomPushMethods = new Dictionary<Type, Action<IToLua, object>>();
        private Dictionary<Type, Func<IToLua, TValue, object>> CustomGetMethods = new Dictionary<Type, Func<IToLua, TValue, object>>();

        public void NewTable(string tableName, int index = -3)
        {
            API.PushString(tableName);
            API.NewTable();
            API.RawSet(index);
        }

        public void GetRef(int reference)
        {
            API.RawGetI(LuaDef.LUA_REGISTRYINDEX, reference);
        }


        #region Push

        public void PushInt64(long value)
        {
            unchecked
            {
                API.PushUInt64((ulong)value);
            }
        }

        public CSharpFunctionDelegate GetLuaCsDelegate<T>(LuaState state, T value)
        {
            return GetCSharpFunctionDelegate<T>(state, value);
        }

        public void PushAny<T>(T value)
        {
            PushAny(value, typeof(T));
        }

        public void PushAny(object value, Type type)
        {
            if (value == null)
            {
                API.PushNil();
                return;
            }
            if (!type.IsAssignableFrom(value.GetType()))
            {
                throw new LuaException($"you cannot push {type.GetFriendlyName()} with {value.GetType().GetFriendlyName()}");
            }
            
            if (CustomPushMethods.TryGetValue(type, out var pushAct))
            {
                pushAct(this, value);
            }
            
            var t = value != null ? value.GetType() : type;
            if (t == typeof(sbyte) || t == typeof(byte) ||
                t == typeof(short) || t == typeof(ushort) ||
                t == typeof(char))
            {
                API.PushInteger((int)Convert.ChangeType(value, typeof(int)));
            }
            else if (t == typeof(int))
            {
                API.PushInteger((int)(object)value);
            }
            else if (t == typeof(uint))
            {
                API.PushUnsigned((uint)(object)value);
            }
            else if (t == typeof(long))
            {
                PushInt64((long)(object)value);
            }
            else if (t == typeof(ulong))
            {
                API.PushUInt64((ulong)(object)value);
            }
            else if (t == typeof(bool))
            {
                API.PushBoolean((bool)(object)value);
            }
            else if (t == typeof(float))
            {
                API.PushNumber((double)Convert.ChangeType(value, typeof(double)));
            }
            else if (t == typeof(double))
            {
                API.PushNumber((double)(object)value);
            }
            else if (t == typeof(string))
            {
                API.PushString(value.ToString());
            }
            else if (t.IsEnum)
            {
                PushAny(Convert.ChangeType(value, Enum.GetUnderlyingType(t)));
            }
            else if (t.IsSubclassOf(typeof(Delegate)))
            {
                API.PushCSharpFunction(GetLuaCsDelegate(this, value));
            }
            else if (t == typeof(LuaLClosureValue))
            {
                API.PushLuaClosure((LuaLClosureValue)(object)value);
            }
            else if (t.IsArray || t == typeof(Array))
            {
                PushArray((Array)(object)value);
            }
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                PushEnumerable((IEnumerable)(object)value);
            }
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                PushList((IList)(object)value);
            }
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                PushDictionary((IDictionary)(object)value);
            }
            else if (t == typeof(JObject))
            {
                PushJObject((JObject)value);
            }
            else if (t == typeof(JArray))
            {
                PushJArray((JArray)value);    
            }
            else if (t == typeof(JToken))
            {
                PushJToken((JToken)value);
            }
            else if (t == typeof(JValue))
            {
                PushJValue((JValue)value);
            }
            else
            {
                if (ClassMetaRefDict.TryGetValue(t.GetFriendlyFullName(), out var reference))
                {
                    API.PushUserData(value, null, 0);
                    GetRef(reference);             
                    API.SetMetaTable(-2);
                }
                else
                {
                    API.PushLightUserData(value);
                    // throw new LuaException($"Cannot find class named {t.FullName} in class meta ref dict please check your lua bind");
                }
                
            }
        }

        public void PushArray(Array array, int start = 0, int count = int.MaxValue)
        {
            if (array == null)
            {
                API.PushNil();
                return;
            }

            API.NewTable();
            int end = count != int.MaxValue ? count : array.Length;
            for (int i = start; i < end; ++i)
            {
                API.PushInteger(i + 1);
                PushAny(array.GetValue(i));
                API.SetTable(-3);
            }
        }

        public void PushEnumerableT<T>(IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                API.PushNil();
                return;
            }

            API.NewTable();
            int i = 0;
            foreach (var e in enumerable)
            {
                API.PushInteger(++i);
                PushAny(e);
                API.SetTable(-3);
            }
        }

        public void PushEnumerable(System.Collections.IEnumerable enumerable)
        {
            if (enumerable == null)
            {
                API.PushNil();
                return;
            }

            API.NewTable();
            int i = 0;
            var e = enumerable.GetEnumerator();
            while (e.MoveNext())
            {
                API.PushInteger(++i);
                PushAny(e.Current);
                API.SetTable(-3);
            }
        }

        public void PushListT<T>(IList<T> list, int start = 0, int count = int.MaxValue)
        {
            if (list == null)
            {
                API.PushNil();
                return;
            }

            API.NewTable();
            int end = count != int.MaxValue ? count : list.Count;
            for (int i = start; i < end; ++i)
            {
                API.PushInteger(i + 1);
                PushAny(list[i]);
                API.SetTable(-3);
            }
        }

        public void PushList(System.Collections.IList list, int start = 0, int count = int.MaxValue)
        {
            if (list == null)
            {
                API.PushNil();
                return;
            }

            API.NewTable();
            int end = count != int.MaxValue ? count : list.Count;
            for (int i = start; i < end; ++i)
            {
                API.PushInteger(i + 1);
                PushAny(list[i]);
                API.SetTable(-3);
            }
        }

        public void PushDictionaryT<K, V>(IDictionary<K, V> dictionary)
        {
            if (dictionary == null)
            {
                API.PushNil();
                return;
            }

            API.NewTable();
            var e = dictionary.GetEnumerator();
            while (e.MoveNext())
            {
                PushAny(e.Current.Key);
                PushAny(e.Current.Value);
                API.SetTable(-3);
            }
        }

        public void PushDictionary(System.Collections.IDictionary dictionary)
        {
            if (dictionary == null)
            {
                API.PushNil();
                return;
            }

            API.NewTable();
            PropertyInfo pKey = null;
            PropertyInfo pValue = null;
            var e = dictionary.GetEnumerator();
            while (e.MoveNext())
            {
                if (pKey == null)
                {
                    var et = e.Current.GetType();
                    pKey = et.GetProperty("Key");
                    pValue = et.GetProperty("Value");
                }
                PushAny(pKey.GetValue(e.Current, null));
                PushAny(pValue.GetValue(e.Current, null));
                API.SetTable(-3);
            }
        }

        public void PushTableItem<T>(int index, T value)
        {
            API.PushInteger(index);
            PushAny(value);
            API.SetTable(-3);
        }

        public void PushJObject(JObject jObject)
        {
            if (jObject == null)
            {
                API.PushNil();
                return;
            }

            API.NewTable();
            foreach (var kvp in jObject)
            {
                PushAny(kvp.Key);
                PushAny(kvp.Value);
                API.SetTable(-3);
            }
        }

        public void PushJArray(JArray jArray)
        {
            if (jArray == null)
            {
                API.PushNil();
                return;
            }
            
            API.NewTable();
            int end = jArray.Count;
            for (int i = 0; i < end; ++i)
            {
                API.PushInteger(i + 1);
                PushAny(jArray[i]);
                API.SetTable(-3);
            }
        }
        
        public void PushJToken(JToken jToken)
        {
            if (jToken == null)
            {
                API.PushNil();
                return;
            }
            switch (jToken.Type)
            {
                case JTokenType.None:
                    API.PushNil();
                    break;
                case JTokenType.Object:
                    var obj = jToken.Value<object>();
                    if (obj is JObject jObject)
                    {
                        PushJObject(jObject);
                    }
                    else
                    {
                        PushAny(obj);
                    }
                    break;
                case JTokenType.Array:
                    PushJArray(jToken.Value<JArray>());
                    break;
                case JTokenType.Property:
                    break;
                case JTokenType.Comment:
                    break;
                case JTokenType.Integer:
                    API.PushNumber(jToken.Value<int>());
                    break;
                case JTokenType.Float:
                    API.PushNumber(jToken.Value<float>());
                    break;
                case JTokenType.String:
                    API.PushString(jToken.Value<string>());
                    break;
                case JTokenType.Boolean:
                    API.PushBoolean(jToken.Value<bool>());
                    break;
                case JTokenType.Null:
                    API.PushNil();
                    break;
                case JTokenType.Undefined:
                    API.PushNil();
                    break;
                case JTokenType.Date:
                case JTokenType.Raw:
                case JTokenType.Bytes:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                case JTokenType.Constructor:
                    throw new NotImplementedException();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void PushJValue(JValue jValue)
        {
            if (jValue == null)
            {
                API.PushNil();
                return;
            }

            PushAny(jValue.Value);
        }
        
        public void AddCustomPushFunc(Type t, Action<IToLua, object> action)
        {
            CustomPushMethods[t] = action;
        }

        public void RemoveCustomPushFunc(Type t)
        {
            CustomPushMethods.Remove(t);
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

        public T[] GetArray<T>(int arg)
        {
            LuaTable table = CheckObject<LuaTable>(arg, false);
            if (table == null)
                return null;

            T[] arr = new T[table.Length];
            for (int i = 0; i < table.Length; ++i)
            {
                arr[i] = GetAny<T>(table.GetInt(i + 1).V);
            }
            return arr;
        }

        public Array GetArray(int arg, Type t)
        {
            LuaTable table = CheckObject<LuaTable>(arg, false);
            if (table == null)
                return null;

            var et = t.GetElementType();
            var array = Array.CreateInstance(et, table.Length);
            for (int i = 0; i < table.Length; i++)
            {
                int tableKey = i + 1;
                array.SetValue(GetAny(table.GetInt(tableKey).V, et), i);
            }

            return array;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="type"></param>
        /// <param name="convertLuaTable">是否自动将LuaTable转换为Array或者Hashtable</param>
        /// <returns></returns>
        public object CheckAny(int arg, Type type, bool convertLuaTable = false)
        {
            StkId addr;
            if (!Index2Addr(arg, out addr))
                return type.DefaultForType();

            return GetAny(addr.V, type, convertLuaTable);
        }

        public T CheckAny<T>(int arg)
        {
            var result = CheckAny(arg, typeof(T));
            try
            {
                return (T)result;
            }
            catch (Exception e)
            {
                L_Error($"{result.GetType().Name} cannot convert to {typeof(T)}!!");
                return default(T);
            }
        }

        public T GetAny<T>(TValue v)
        {
            var t = typeof(T);
            return (T)GetAny(v, t);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <param name="t"></param>
        /// <param name="convertLuaTable">是否自动将LuaTable转换为Array或者HashTable</param>
        /// <returns></returns>
        /// <exception cref="LuaException"></exception>
        public object GetAny(TValue v, Type t, bool convertLuaTable = false)
        {
            try
            {
                if (CustomGetMethods.TryGetValue(t, out var getFunc))
                {
                    return getFunc(this, v);
                }
                
                if (t.IsEnum)
                    t = Enum.GetUnderlyingType(t);

                switch ((LuaType) v.Tt)
                {
                    case LuaType.LUA_TNIL:
                        return t.DefaultForType();
                    case LuaType.LUA_TBOOLEAN:
                        bool b = !v.TtIsNil() && (!v.TtIsBoolean() || v.BValue());
                        return Convert.ChangeType(b, t);
                    case LuaType.LUA_TUSERDATA:
                        var userdata = v.OValue as LuaUserDataValue;
                        if (t.IsAssignableFrom(userdata.Value.GetType()))
                        {
                            return userdata.Value;
                        }
                        return Convert.ChangeType(userdata.Value, t);
                    case LuaType.LUA_TLIGHTUSERDATA:
                        var valueType = v.OValue.GetType();
                        if (t.IsAssignableFrom(valueType))
                        {
                            return v.OValue;
                        }
                        return Convert.ChangeType(v.OValue, t);
                    case LuaType.LUA_TNUMBER:
                        if (t == typeof(object) &&
                            v.NValue >= int.MinValue && v.NValue <= int.MaxValue &&
                            v.NValue == (int) v.NValue)
                            t = typeof(int);
                        if (t.IsEnum)
                        {
                            return Convert.ChangeType(Enum.ToObject(t, v.NValue), t);
                        }
                        return Convert.ChangeType(v.NValue, t);
                    case LuaType.LUA_TSTRING:
                        if (t == typeof(char) && v.OValue is string strObj)
                        {
                            if (strObj.Length == 0)
                            {
                                return default(char);
                            }
                            return strObj[0];
                        }
                        return v.OValue;
                    case LuaType.LUA_TTABLE:
                        return GetTableValue(v.OValue, t, convertLuaTable);
                    case LuaType.LUA_TFUNCTION:
                        return GetDelegateValue(v, t);
                    case LuaType.LUA_TTHREAD:
                        return v.OValue;
                    case LuaType.LUA_TUINT64:
                        return Convert.ChangeType(v.UInt64Value, t);
                    default:
                        return t.DefaultForType();
                }
            }
            catch (Exception e)
            {
                L_Error("convert exception {0}", e.ToString());
                throw new LuaException($"cannot convert {(LuaType)v.Tt} to {t.FullName}!!!! {e.StackTrace}", e);
            }
           
        }

        public void AddCustomGetFunc(Type type, Func<IToLua, TValue, object> getFunc)
        {
            CustomGetMethods[type] = getFunc;
        }

        public void RemoveCustomGetFunc(Type type)
        {
            CustomGetMethods.Remove(type);
        }
        
        private T GetTableValue<T>(object v)
        {
            Type t = typeof(T);
            return (T)GetTableValue(v, t);
        }

        private object GetTableValue(object v, Type t, bool convertLuaTable = false)
        {
            if (t == typeof(object))
            {
                // 注意：
                // 一般只有在操作虚拟机的时候需要使用LuaTable 此时 convertLuaTable = false
                // 而在日常使用将Table Check 到C# 侧我们默认使用的是Array或者HashTable，此时 convertLuaTable = true
                if (convertLuaTable && v is LuaTable)
                {
                    var table = v as LuaTable;
                    if (table.IsArray)
                    {
                        return table.ToArray();
                    }
                    else
                    {
                        return table.ToHashTable();
                    }
                }
                return v;
            }

            if (t == typeof(LuaTable))
            {
                return v;
            }
            if (v is LuaTable)
            {
                var table = v as LuaTable;
                if (t.IsArray || t == typeof(Array))
                {
                    return table.ToArray(t.GetElementType());
                }
                else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                {
                    System.Type itemType = t.GetGenericArguments()[0];
                    var result = Activator.CreateInstance(t);
                    var addMethod = t.GetMethod("Add");
                    for (int i = 0; i < table.Length; i++)
                    {
                        int tableKey = i + 1;
                        var item = GetAny(table.GetInt(tableKey).V, itemType, true);
                        addMethod.Invoke(result, new [] {item});
                    }

                    return result;
                }
                else if(t == typeof(ArrayList))
                {
                    return table.ToArrayList();
                }
                else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    System.Type itemType = t.GetGenericArguments()[0];
                    var result = Activator.CreateInstance(t);
                    var addMethod = t.GetMethod("Add");
                    for (int i = 0; i < table.Length; i++)
                    {
                        int tableKey = i + 1;
                        var item = GetAny(table.GetInt(tableKey).V, itemType, true);
                        addMethod.Invoke(result, new [] {item});
                    }

                    return result;
                }
                else if (t == typeof(Hashtable))
                {
                    return table.ToHashTable();
                }
                else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    System.Type[] genericArgumentTypes = t.GetGenericArguments();
                    var keyType = genericArgumentTypes[0];
                    var valueType = genericArgumentTypes[1];
                    var result = Activator.CreateInstance(t);

                    var addMethod = t.GetMethod("Add");

                    var keys = table.GetKeys();
                    var keySet = new HashSet<object>();
                    foreach (var keyStkId in keys)
                    {
                        var key = GetAny(keyStkId.V, keyType, true);
                        if (!keySet.Contains(key))
                        {
                             keySet.Add(key);
                            var valueStkId = table.Get(ref keyStkId.V);
                            var value = GetAny(valueStkId.V, valueType, true);
                            addMethod.Invoke(result, new[] {key, value});
                        }
                       
                    }

                    return result;
                }
                else
                {
                    return GetObjectFromTable(table, t);
                }
            }
            else
            {
                L_Error(v.GetType().FullName);
            }
            return Convert.ChangeType(v, t);
        }

        private object GetDelegateValue(TValue v, Type t)
        {
            var type = (LuaType) v.Tt;
            switch (type)
            {
                case LuaType.LUA_TNIL:
                    return null;
                case LuaType.LUA_TFUNCTION:
                    if (v.OValue is LuaLClosureValue closure)
                    {
                        return CreateDelegate(this, t, closure);
                    }
                    return null;
                case LuaType.LUA_TUSERDATA:
                case LuaType.LUA_TLIGHTUSERDATA:
                    return (Delegate) GetAny(v, t);
                default:
                    return null;
            }
        }

        private object GetObjectFromTable(LuaTable table, Type t)
        {
            var keys = table.GetKeys()
                .Where(key => key.V.TtIsString())
                .Select(key => key.V.OValue.ToString())
                .ToList();

            var cons = t.GetConstructor(new Type[0]);
            var result = cons.Invoke(new object[0]);
            foreach (var key in keys)
            {
                var value = table.GetStr(key);
                var field = t.GetField(key);
                if (field != null && field.IsPublic)
                {
                    field.SetValue(result, GetAny(value.V, field.FieldType));
                    continue;
                }

                var property = t.GetProperty(key);
                if (property != null && property.SetMethod != null && property.SetMethod.IsPublic)
                {
                    property.SetValue(result, GetAny(value.V, property.PropertyType));
                    continue;
                }

                L_Error("cannot find property named {0}", key);
            }

            return result;
        }

        private static Delegate CreateDelegate(LuaState state, Type t, LuaLClosureValue func = null)
        {
            Func<LuaState, LuaLClosureValue, Delegate> Create = null;
            if (!state.createLuaDelegateDict.TryGetValue(t, out Create))
            {
                throw new LuaException(string.Format("Create Delegate {0} not register", t.GetFriendlyName()));
            }

            if (func != null)
            {
                Delegate d = Create(state, func);
                return d;
            }

            return Create(state, null);
        }

        private static CSharpFunctionDelegate GetCSharpFunctionDelegate<T>(LuaState state, T func)
        {
            if (!state.csFunctionDelegateDict.TryGetValue(typeof(T), out var dele))
            {
                throw new LuaException(string.Format("CSharp Function Delegate {0} not register", typeof(T).GetFriendlyName()));
            }

            return dele(func as Delegate);
        }

        #endregion

        #region StackCheck

        public bool CheckNum(int count)
        {
            return API.GetTop() == count;
        }

        public bool CheckRange(int min, int max)
        {
            var topNum = API.GetTop();
            return topNum <= max && topNum >= min;
        }

        public void DumpStack()
        {
            StringBuilder builder = new StringBuilder();
            var top = API.GetTop();
            for (int i = 1; i < top; i++)
            {
                StkId addr;
                if (!Index2Addr(i, out addr))
                {
                }

                var type = (LuaType) addr.V.Tt;
                builder.AppendLine($"{i} - {addr}");
            }
            Log.Info(builder);
        }

        #endregion

        #region LuaOperation

        

        public bool TryGetAnyFromLua(string name, Type type, out object result)
        {
            result = type.DefaultForType();
            var names = name.Split('.');
            Stack<TableScope> stack = new Stack<TableScope>();
            stack.Push(new TableScope(this, null, false));
            bool succ = true;
            for (int i = 0; i < names.Length - 1; i++)
            {
                var scope = new TableScope(this, names[i], false);
                if (!scope.IsValid)
                {
                    succ = false;
                    break;
                }
                stack.Push(scope);
            }

            if (succ)
            {
                API.PushString(names[names.Length - 1]); // table name
                API.GetTable(-2);                    // table value
                result = CheckAny(-1, type);            // table value
                API.Pop(1);                           // table
            }

            while (stack.Count > 0)
            {
                stack.Pop().Dispose();
            }

            return succ;
        }

        public bool Require(string moduleName, out object module)
        {
            module = null;
            try
            {
                module = CallFuncR1<object, string>("require", moduleName);
                if (module is bool result)
                {
                    return result;
                }
                else
                {
                    return module != null;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return false;
            }
        }

        public void CallField(LuaTable table, string functionName, params object[] args)
        {
            try
            {
                API.PushLuaTable(table);
                API.GetField(-1, functionName);

                if (API.IsFunction(-1))
                {
                    API.PushValue(-2);
                    foreach (var arg in args)
                    {
                        PushAny(arg);
                    }

                    API.Call(args.Length + 1, 0);
                }
            }
            catch (LuaRuntimeException e)
            {
                var errMsg = L_CheckString(-1);
                Log.Error(errMsg);
            }
            finally
            {
                API.SetTop(0);
            }
            
        }
        
        public T CallField1R<T>(LuaTable table, string functionName, params object[] args)
        {
            T result = default(T);
            try
            {
                API.PushLuaTable(table);
                API.GetField(-1, functionName);

                if (API.IsFunction(-1))
                {
                    API.PushValue(-2);
                    foreach (var arg in args)
                    {
                        PushAny(arg);
                    }

                    API.Call(args.Length + 1, 1);
                    
                    int top = API.GetTop();
                    result = CheckAny<T>(top);
                    API.Pop(1);
                }
            }
            catch (LuaRuntimeException e)
            {
                var errMsg = L_CheckString(-1);
                Log.Error(errMsg);
            }
            finally
            {
                API.SetTop(0);
            }

            return result;
        }
        
        public bool TryGetAnyFromLua<T>(string name, out T result)
        {

            bool succ = TryGetAnyFromLua(name, typeof(T), out var inneResult);
            result = (T)inneResult;
            return succ;
        }

        public bool SetAnyToLua(string name, object value, Type type, bool createTableIfNil = true)
        {
            var names = name.Split('.');
            Stack<TableScope> stack = new Stack<TableScope>();
            stack.Push(new TableScope(this, null, false));

            bool succ = true;
            for (int i = 0; i < names.Length - 1; i++)
            {
                var scope = new TableScope(this, names[i], createTableIfNil);
                if (!scope.IsValid)
                {
                    succ = false;
                    break;
                }
                stack.Push(scope);
            }

            if (succ)
            {
                API.PushString(names[names.Length - 1]); // table name
                PushAny(value, type);                    // table name value
                API.SetTable(-3);                  // table 
            }

            while (stack.Count > 0)
            {
                stack.Pop().Dispose();
            }

            return succ;
        }

        public bool SetAnyToLua<T>(string name, T value, bool createTableIfNil = true)
        {
            return SetAnyToLua(name, value, typeof(T), createTableIfNil);
        }


        #endregion

    }
}