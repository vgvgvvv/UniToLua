using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Common
{
    /// <summary>
    /// 通过编写方法并且添加属性可以做到转换至String 如：
    /// 
    /// <example>
    /// [ToString]
    /// public static string ConvertToString(TestObj obj)
    /// </example>
    ///
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ToString : Attribute { }

    /// <summary>
    /// 通过编写方法并且添加属性可以做到转换至String 如：
    /// 
    /// <example>
    /// [FromString]
    /// public static TestObj ConvertFromString(string str)
    /// </example>
    ///
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class FromString : Attribute { }


    public static class StringEx
    {

        public static char Spriter1 = ',';
        public static char Spriter2 = ':';

        public static char FBracket1 = '(';
        public static char BBracket1 = ')';

        public static T GetValue<T>(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return default(T);
            }
            return value.TryGetValue<T>((T)typeof(T).DefaultForType());
        }

        /// <summary>
        /// 从字符串中获取值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T TryGetValue<T>(this string value, T defultValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                return default(T);
            }
            return (T)TryGetValue(value, typeof(T), defultValue);
        }

        public static object GetValue(this string value, System.Type type)
        {
            return value.TryGetValue(type, type.DefaultForType());
        }

        /// <summary>
        /// 从字符串中获取值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object TryGetValue(this string value, System.Type type, object defultValue)
        {
            try
            {
                if (type == null) return "";
                if (string.IsNullOrEmpty(value))
                {
                    return type.IsValueType ? Activator.CreateInstance(type) : null;
                }

                if (type == typeof(string))
                {
                    return value;
                }
                if (type == typeof(int))
                {
                    return Convert.ToInt32(Convert.ToDouble(value));
                }
                if (type == typeof(float))
                {
                    return float.Parse(value);
                }
                if (type == typeof(byte))
                {
                    return Convert.ToByte(Convert.ToDouble(value));
                }
                if (type == typeof(sbyte))
                {
                    return Convert.ToSByte(Convert.ToDouble(value));
                }
                if (type == typeof(uint))
                {
                    return Convert.ToUInt32(Convert.ToDouble(value));
                }
                if (type == typeof(short))
                {
                    return Convert.ToInt16(Convert.ToDouble(value));
                }
                if (type == typeof(long))
                {
                    return Convert.ToInt64(Convert.ToDouble(value));
                }
                if (type == typeof(ushort))
                {
                    return Convert.ToUInt16(Convert.ToDouble(value));
                }
                if (type == typeof(ulong))
                {
                    return Convert.ToUInt64(Convert.ToDouble(value));
                }
                if (type == typeof(double))
                {
                    return double.Parse(value);
                }
                if (type == typeof(bool))
                {
                    return bool.Parse(value);
                }
                if (type.BaseType == typeof(Enum))
                {
                    return GetValue(value, Enum.GetUnderlyingType(type));
                }
               

                object constructor;
                object genericArgument;
                //词典
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                {
                    System.Type[] genericArguments = type.GetGenericArguments();
                    Dictionary<string, string> dictionary = ParseMap(value, Spriter2, Spriter1);
                    constructor = type.GetConstructor(System.Type.EmptyTypes).Invoke(null);
                    foreach (KeyValuePair<string, string> pair in dictionary)
                    {
                        object genericArgument1 = GetValue(pair.Key, genericArguments[0]);
                        genericArgument = GetValue(pair.Value, genericArguments[1]);
                        type.GetMethod("Add").Invoke(constructor, new object[] { genericArgument1, genericArgument });
                    }
                    return constructor;
                }
                //列表
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    System.Type type2 = type.GetGenericArguments()[0];
                    List<string> list = ParseList(value, Spriter1);

                    constructor = Activator.CreateInstance(type);
                    foreach (string str in list)
                    {
                        genericArgument = GetValue(str, type2);
                        type.GetMethod("Add").Invoke(constructor, new object[] { genericArgument });
                    }
                    return constructor;
                }
                if (type == typeof(ArrayList))
                {
                    return value.GetValue<List<string>>() ?? defultValue;
                }
                if (type == typeof(Hashtable))
                {
                    return value.GetValue<Dictionary<string, string>>() ?? defultValue;
                }
                //数组
                if (type.IsArray)
                {
                    Type elementType = Type.GetType(
                     type.FullName.Replace("[]", string.Empty));
                    string[] elStr = value.Split(Spriter1);
                    Array array = Array.CreateInstance(elementType, elStr.Length);

                    for (int i = 0; i < elStr.Length; i++)
                    {
                        array.SetValue(elStr[i].GetValue(elementType), i);
                    }
                    return array;
                }
                if (CanConvertFromString(type))
                {
                    return ParseFromStringableObject(value, type);
                }

                if (defultValue != type.DefaultForType())
                {
                    return defultValue;
                }
                return type.DefaultForType();
            }
            catch (Exception e)
            {
                Log.Exception(e);
                throw;
            }
        }

        #region FromString


        /// <summary>
        /// 解析列表
        /// </summary>
        /// <param name="strList"></param>
        /// <param name="listSpriter"></param>
        /// <returns></returns>
        public static List<string> ParseList(this string strList, char listSpriter = ',')
        {
            List<string> list = new List<string>();
            if (!string.IsNullOrEmpty(strList))
            {
                string str = strList.Trim();
                if (string.IsNullOrEmpty(strList))
                {
                    return list;
                }
                string[] strArray = str.Split(new char[] { listSpriter });
                foreach (string str2 in strArray)
                {
                    if (!string.IsNullOrEmpty(str2))
                    {
                        list.Add(str2.Trim());
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 解析词典
        /// </summary>
        /// <param name="strMap"></param>
        /// <param name="keyValueSpriter"></param>
        /// <param name="mapSpriter"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ParseMap(this string strMap, char keyValueSpriter = ':', char mapSpriter = ',')
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(strMap))
            {
                string[] strArray = strMap.Split(new char[] { mapSpriter });
                for (int i = 0; i < strArray.Length; i++)
                {
                    if (!string.IsNullOrEmpty(strArray[i]))
                    {
                        string[] strArray2 = strArray[i].Split(new char[] { keyValueSpriter });
                        if ((strArray2.Length == 2) && !dictionary.ContainsKey(strArray2[0]))
                        {
                            dictionary.Add(strArray2[0].Trim(), strArray2[1].Trim());
                        }
                    }
                }
            }
            return dictionary;
        }

    
        /// <summary>
        /// 解析可解析对象
        /// </summary>
        /// <param name="str"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object ParseFromStringableObject(string str, Type type)
        {
            var methodInfos = type.GetMethods();

            MethodInfo info = null;
            foreach (var method in methodInfos)
            {
                if (info != null) break;
                var attrs = method.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    if (attr is FromString)
                    {
                        info = method;
                        break;
                    }
                }
            }

            return info.Invoke(null, new object[1] { str });

        }

        #endregion FromString 


        /// <summary>
        /// 将值转化为字符串
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ConvertToString(this object value)
        {
            //Debug.logger.Log("ConverToString " + Spriter1 + "  "+ Spriter2);
            if (value == null) return string.Empty;
            System.Type type = value.GetType();
            if (type == null)
            {
                return string.Empty;
            }
            if (type.BaseType == typeof(Enum))
            {
                return Enum.GetName(type, value);
            }
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
            {
                int Count = (int)type.GetProperty("Count").GetValue(value, null);
                if (Count == 0) return String.Empty;
                MethodInfo getIe = type.GetMethod("GetEnumerator");
                object enumerator = getIe.Invoke(value, new object[0]);
                System.Type enumeratorType = enumerator.GetType();
                MethodInfo moveToNextMethod = enumeratorType.GetMethod("MoveNext");
                PropertyInfo current = enumeratorType.GetProperty("Current");

                StringBuilder stringBuilder = new StringBuilder();

                while (enumerator != null && (bool)moveToNextMethod.Invoke(enumerator, new object[0]))
                {
                    stringBuilder.Append(Spriter1.ToString() + ConvertToString(current.GetValue(enumerator, null)));
                }

                return stringBuilder.ToString().ReplaceFirst(Spriter1.ToString(), "");

            }
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)))
            {
                object pairKey = type.GetProperty("Key").GetValue(value, null);
                object pairValue = type.GetProperty("Value").GetValue(value, null);

                string keyStr = ConvertToString(pairKey);
                string valueStr = ConvertToString(pairValue);
                return keyStr + Spriter2.ToString() + valueStr;

            }
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
            {
                int Count = (int)type.GetProperty("Count").GetValue(value, null);
                if (Count == 0) return String.Empty;
                MethodInfo mget = type.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public);

                StringBuilder stringBuilder = new StringBuilder();

                object item;
                string itemStr;

                for (int i = 0; i < Count - 1; i++)
                {
                    item = mget.Invoke(value, new object[] { i });
                    itemStr = StringEx.ConvertToString(item);
                    stringBuilder.Append(itemStr + Spriter1.ToString());
                }
                item = mget.Invoke(value, new object[] { Count - 1 });
                itemStr = StringEx.ConvertToString(item);
                stringBuilder.Append(itemStr);

                return stringBuilder.ToString();
            }
            if (type == typeof(ArrayList))
            {
                StringBuilder builder = new StringBuilder();
                var arrayList = value as ArrayList;
                for (int i = 0; i < arrayList.Count - 1; i++)
                {
                    builder.Append(arrayList[i].ConvertToString()).Append(",");
                }
                builder.Append(arrayList[arrayList.Count - 1].ConvertToString());
                return builder.ToString();
            }
            if (type == typeof(Hashtable))
            {
                StringBuilder builder = new StringBuilder();
                var table = value as Hashtable;
                IEnumerator e = table.Keys.GetEnumerator();
                int index = 0;
                while (e.MoveNext())
                {
                    var tableKey = e.Current.ConvertToString();
                    var tableValue = table[e.Current].ConvertToString();
                    if (index != 0)
                    {
                        builder.Append(StringEx.Spriter1);
                    }
                    builder.Append(tableKey).Append(StringEx.Spriter2).Append(tableValue);
                    index++;
                }
                return builder.ToString();
            }
            if (type.IsArray)
            {
                StringBuilder stringBuilder = new StringBuilder();
                var array = value as Array;
                if (array.Length > 0)
                {
                    stringBuilder.Append(ConvertToString(array.GetValue(0)));
                    for (int i = 1; i < array.Length; i++)
                    {
                        stringBuilder.Append(Spriter1.ToString());
                        stringBuilder.Append(ConvertToString(array.GetValue(i)));
                    }
                    return stringBuilder.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
            if (CanConvertToString(type))
            {
                return ToStringableObjectConvertToString(value, type);
            }

            {
                StringBuilder builder = new StringBuilder();
                builder.Append("{ ");
                if (type.IsPrimitive || value is string)
                {
                    return value.ToString();
                }
            
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                if (fields.Length == 0)
                {
                    return value.ToString();
                }
            
                foreach (var field in fields)
                {
                    var aValue = field.GetValue(value);
                    builder.Append($" {{ {field.Name} : {aValue.ConvertToString()} }} ");
                }

                builder.Append(" }");

                return builder.ToString();
            }
            
        }


        #region ToString

        /// <summary>
        /// 将列表转换至字符串
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string ListConvertToString<T>(this List<T> list, char split1 = ',')
        {
            Type type = list.GetType();
            int Count = (int)type.GetProperty("Count").GetValue(list, null);
            if (Count == 0) return String.Empty;
            MethodInfo mget = type.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public);

            StringBuilder stringBuilder = new StringBuilder();

            object item;
            string itemStr;

            for (int i = 0; i < Count - 1; i++)
            {
                item = mget.Invoke(list, new object[] { i });
                itemStr = StringEx.ConvertToString(item);
                stringBuilder.Append(itemStr + split1.ToString());
            }
            item = mget.Invoke(list, new object[] { Count - 1 });
            itemStr = StringEx.ConvertToString(item);
            stringBuilder.Append(itemStr);

            return stringBuilder.ToString();
        }
        
        /// <summary>
        /// 将表格转为表格
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string ListConvertToForm<T>(this List<T> list)
        {
            Type type = list.GetType();
            int Count = (int)type.GetProperty("Count").GetValue(list, null);
            if (Count == 0) return String.Empty;
            MethodInfo mget = type.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public);
            
            StringBuilder stringBuilder = new StringBuilder();

            var valueType = list.First().GetType();
            
            var valueTypeFields = valueType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var valueTypeProperties = valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (valueTypeFields.Length > 0 || valueTypeProperties.Length > 0)
            {
                var fieldNames = valueTypeFields.Select(f => f.Name).ToArray();
                var propertyNames = valueTypeProperties.Select(p => p.Name).ToArray();

                stringBuilder.Append($"|");
                foreach (var fieldName in fieldNames)
                {
                    stringBuilder.Append($"{fieldName,-20}|");
                }

                foreach (var propertyName in propertyNames)
                {
                    stringBuilder.Append($"{propertyName,-20}|");
                }

                stringBuilder.AppendLine();
                stringBuilder.AppendLine(new string('=', (fieldNames.Length + propertyNames.Length) * 21 + 1));
                
                object item;
            
                for (int i = 0; i < Count; i++)
                {
                    var lineBuilder = new StringBuilder();

                    lineBuilder.Append("|");
                    item = mget.Invoke(list, new object[] { i });

                    foreach (var fieldInfo in valueTypeFields)
                    {
                        lineBuilder.Append($"{fieldInfo.GetValue(item),-20}|");
                    }
                
                    foreach (var propertyInfo in valueTypeProperties)
                    {
                        lineBuilder.Append($"{propertyInfo.GetValue(item),-20}|");
                    }
                    
                    stringBuilder.AppendLine(lineBuilder.ToString());
                }
                stringBuilder.AppendLine(new string('=', (fieldNames.Length + propertyNames.Length) * 21 + 1));
            }
            else
            {
                stringBuilder.Append($"|{"value", -20}|");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(new string('=', 1 * 21 + 1));
                
                object item;
            
                for (int i = 0; i < Count; i++)
                {
                    var lineBuilder = new StringBuilder();

                    lineBuilder.Append("|");
                    item = mget.Invoke(list, new object[] { i });

                    lineBuilder.Append($"{item,-20}|");
                    
                    stringBuilder.AppendLine(lineBuilder.ToString());
                }
                
                stringBuilder.AppendLine(new string('=', 1 * 21 + 1));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 数组转换至字符串
        /// </summary>
        /// <param name="value"></param>
        /// <param name="split1"></param>
        /// <returns></returns>
        public static string ArrConvertToString(this Array value, char split1 = ',')
        {
            StringBuilder stringBuilder = new StringBuilder();
            var array = value as Array;
            if (array.Length > 0)
            {
                stringBuilder.Append(ConvertToString(array.GetValue(0)));
                for (int i = 1; i < array.Length; i++)
                {
                    stringBuilder.Append(split1.ToString());
                    stringBuilder.Append(ConvertToString(array.GetValue(i)));
                }
                return stringBuilder.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 将键值对转换至字符串
        /// </summary>
        /// <param name="value"></param>
        /// <param name="split1"></param>
        /// <returns></returns>
        public static string KVPConvertToString<K, V>(this KeyValuePair<K, V> value, char split1 = ':')
        {
            Type type = value.GetType();
            object pairKey = type.GetProperty("Key").GetValue(value, null);
            object pairValue = type.GetProperty("Value").GetValue(value, null);

            string keyStr = ConvertToString(pairKey);
            string valueStr = ConvertToString(pairValue);
            return keyStr + Spriter2.ToString() + valueStr;
        }

        /// <summary>
        /// 将Dictionary转换至字符串
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="value"></param>
        /// <param name="split1"></param>
        /// <param name="split2"></param>
        /// <returns></returns>
        public static string DictConvertToString<K, V>(this Dictionary<K, V> value, char split1 = ',', char split2 = ':')
        {
            Type type = value.GetType();
            int Count = (int)type.GetProperty("Count").GetValue(value, null);
            if (Count == 0) return String.Empty;
            MethodInfo getIe = type.GetMethod("GetEnumerator");
            object enumerator = getIe.Invoke(value, new object[0]);
            System.Type enumeratorType = enumerator.GetType();
            MethodInfo moveToNextMethod = enumeratorType.GetMethod("MoveNext");
            PropertyInfo current = enumeratorType.GetProperty("Current");

            StringBuilder stringBuilder = new StringBuilder();

            while (enumerator != null && (bool)moveToNextMethod.Invoke(enumerator, new object[0]))
            {
                stringBuilder.Append(split1.ToString() + ConvertToString(current.GetValue(enumerator, null)));
            }

            return stringBuilder.ToString().ReplaceFirst(split1.ToString(), "");
        }

        /// <summary>
        /// 将Dictionary转为表格
        /// </summary>
        /// <param name="dict"></param>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        public static string DictConvertToForm<K, V>(this Dictionary<K, V> dict)
        {
            Type type = dict.GetType();
            int Count = (int)type.GetProperty("Count").GetValue(dict, null);
            if (Count == 0) return String.Empty;
            MethodInfo getIe = type.GetMethod("GetEnumerator");
            object enumerator = getIe.Invoke(dict, new object[0]);
            System.Type enumeratorType = enumerator.GetType();
            MethodInfo moveToNextMethod = enumeratorType.GetMethod("MoveNext");
            PropertyInfo current = enumeratorType.GetProperty("Current");
            
            StringBuilder stringBuilder = new StringBuilder();

            Type valueType = dict.First().Value.GetType();

            var valueTypeFields = valueType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var valueTypeProperties = valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (valueTypeFields.Length > 0 || valueTypeProperties.Length > 0)
            {
                var fieldNames = valueTypeFields.Select(f => f.Name).ToArray();
                var propertyNames = valueTypeProperties.Select(p => p.Name).ToArray();

                stringBuilder.Append($"|{"",-20}|");
                foreach (var fieldName in fieldNames)
                {
                    stringBuilder.Append($"{fieldName,-20}|");
                }
            
                foreach (var propertyName in propertyNames)
                {
                    stringBuilder.Append($"{propertyName,-20}|");
                }

                stringBuilder.AppendLine();
                stringBuilder.AppendLine(new string('=', (fieldNames.Length + propertyNames.Length + 1)*21+1));
            
                while (enumerator != null && (bool)moveToNextMethod.Invoke(enumerator, new object[0]))
                {
                    StringBuilder lineBuilder = new StringBuilder();
                    var kvp = current.GetValue(enumerator, null);

                    var key = kvp.GetPropertyByReflect("Key");
                    var value = kvp.GetPropertyByReflect("Value");

                    lineBuilder.Append($"|{key,-20}|");
                
                    foreach (var fieldInfo in valueTypeFields)
                    {
                        lineBuilder.Append($"{fieldInfo.GetValue(value),-20}|");
                    }
                
                    foreach (var propertyInfo in valueTypeProperties)
                    {
                        lineBuilder.Append($"{propertyInfo.GetValue(value),-20}|");
                    }

                    stringBuilder.AppendLine(lineBuilder.ToString());
                }
                
                stringBuilder.AppendLine(new string('=', (fieldNames.Length + propertyNames.Length + 1)*21+1));
            }
            else
            {
                stringBuilder.Append($"|{"",-20}|{"value",-20}|");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(new string('=', (2)*21+1));
                
                while (enumerator != null && (bool) moveToNextMethod.Invoke(enumerator, new object[0]))
                {
                    StringBuilder lineBuilder = new StringBuilder();
                    
                    var kvp = current.GetValue(enumerator, null);

                    var key = kvp.GetPropertyByReflect("Key");
                    var value = kvp.GetPropertyByReflect("Value");

                    lineBuilder.Append($"|{key,-20}|");
                    lineBuilder.Append($"{value.ConvertToString(),-20}|");
                    
                    stringBuilder.AppendLine(lineBuilder.ToString());
                }
                
                stringBuilder.AppendLine(new string('=', (2)*21+1));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 将可转换至字符串的对象转换为字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ToStringableObjectConvertToString(this object obj, Type type)
        {
            var methodInfos = type.GetMethods();

            MethodInfo info = null;
            foreach (var method in methodInfos)
            {
                if (info != null) break;
                var attrs = method.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    if (attr is ToString)
                    {
                        info = method;
                        break;
                    }
                }
            }

            return info.Invoke(null, new object[1] { obj }) as string;
        }

        #endregion ToString


        //可转换类型列表
        public static readonly List<Type> convertableTypes = new List<Type>
        {
            typeof(int),
            typeof(string),
            typeof(float),
            typeof(double),
            typeof(byte),
            typeof(long),
            typeof(bool),
            typeof(short),
            typeof(uint),
            typeof(ulong),
            typeof(ushort),
            typeof(sbyte),
            typeof(Dictionary<,>),
            typeof(KeyValuePair<,>),
            typeof(List<>),
            typeof(Enum),
            typeof(Array)
        };

        /// <summary>
        /// 通过文本获取类型：
        /// 注意！解析嵌套多泛型类型时会出现问题！
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Type GetTypeByString(this string str)
        {
            str = str.Trim();
            switch (str)
            {
                case "int":
                    return typeof(int);
                case "float":
                    return typeof(float);
                case "string":
                    return typeof(string);
                case "double":
                    return typeof(double);
                case "byte":
                    return typeof(byte);
                case "bool":
                    return typeof(bool);
                case "short":
                    return typeof(short);
                case "uint":
                    return typeof(uint);
                case "ushort":
                    return typeof(ushort);
                case "sbyte":
                    return typeof(sbyte);
            }

            if (str.StartsWith("List"))
            {
                Type genType = str.Substring(str.IndexOf('<') + 1, str.IndexOf('>') - str.LastIndexOf('<') - 1).GetTypeByString();
                return Type.GetType("System.Collections.Generic.List`1[[" + genType.FullName + ", " + genType.Assembly.FullName + "]], " + typeof(List<>).Assembly.FullName);
            }

            if (str.StartsWith("Dictionary"))
            {
                string[] typeNames = str.Substring(str.IndexOf('<') + 1, str.IndexOf('>') - str.LastIndexOf('<') - 1).Split(',');
                Type type1 = typeNames[0].Trim().GetTypeByString();
                Type type2 = typeNames[1].Trim().GetTypeByString();
                string typeStr = "System.Collections.Generic.Dictionary`2[[" + type1.FullName + ", " + type1.Assembly.FullName + "]" +
                    ",[" + type2.FullName + ", " + type2.Assembly.FullName + "]], " +
                    typeof(Dictionary<,>).Assembly.FullName;
                return Type.GetType(typeStr);
            }
            //仅支持内置类型,支持多维数组
            if (str.Contains("[") && str.Contains("]"))
            {
                string itemTypeStr = str.Substring(0, str.IndexOf('['));
                string bracketStr = str.Substring(str.IndexOf('['), str.LastIndexOf(']') - str.IndexOf('[') + 1);
                Type itemType = itemTypeStr.GetTypeByString();
                string typeStr = itemType.FullName + bracketStr;
                return Type.GetType(typeStr);
            }
            return Type.GetType(str);
        }

        /// <summary>
        /// 是否为可转换字符串的类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsConvertableType(this Type type)
        {
            return CanConvertFromString(type) && CanConvertToString(type) || convertableTypes.Contains(type);
        }

        /// <summary>
        /// 是否可以从String中转换出来
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool CanConvertFromString(this Type type)
        {
            var methodInfos = type.GetMethods();
            foreach (var method in methodInfos)
            {
                var attrs = method.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    if (attr is FromString)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 是否可以转换为String
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool CanConvertToString(this Type type)
        {
            var methodInfos = type.GetMethods();
            foreach (var method in methodInfos)
            {
                var attrs = method.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    if (attr is ToString)
                    {
                        return true;
                    }
                }
            }
            return false;
        }



        /// <summary>
        /// 替换第一个匹配值
        /// </summary>
        /// <param name="input"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <param name="startAt"></param>
        /// <returns></returns>
        public static string ReplaceFirst(this string input, string oldValue, string newValue, int startAt = 0)
        {
            int index = input.IndexOf(oldValue, startAt);
            if (index < 0)
            {
                return input;
            }
            return (input.Substring(0, index) + newValue + input.Substring(index + oldValue.Length));
        }

        /// <summary>
        /// 是否存在中文字符
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool HasChinese(this string input)
        {
            return Regex.IsMatch(input, @"[\u4e00-\u9fa5]");
        }

        /// <summary>
        /// 是否存在空格
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool HasSpace(this string input)
        {
            return input.Contains(" ");
        }

        /// <summary>
        /// 将一系列的对象转换为字符串并且以符号分割
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="sp"></param>
        /// <returns></returns>
        public static string Join<T>(this IEnumerable<T> source, string sp)
        {
            var result = new StringBuilder();
            var first = true;
            foreach (T item in source)
            {
                if (first)
                {
                    first = false;
                    result.Append(item.ConvertToString());
                }
                else
                {
                    result.Append(sp).Append(item.ConvertToString());
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// 扩展方法来判断字符串是否为空
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsNullOrEmptyR(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// 删除特定字符
        /// </summary>
        /// <param name="str"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string RemoveString(this string str, params string[] targets)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                str = str.Replace(targets[i], string.Empty);
            }
            return str;
        }

        /// <summary>
        /// 拆分并去除空格
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string[] SplitAndTrim(this string str, params char[] separator)
        {
            var res = str.Split(separator);
            for (var i = 0; i < res.Length; i++)
            {
                res[i] = res[i].Trim();
            }
            return res;
        }

        public static string Indent(this string str, string indent)
        {
            StringBuilder builder = new StringBuilder();

            var reader = new StringReader(str);
            var line = string.Empty;
            while (line != null)
            {
                line = reader.ReadLine();
                builder.AppendLine($"{indent}{line}");
            }

            return builder.ToString();
        }
        

        /// <summary>
        /// 查找在两个字符串中间的字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="front"></param>
        /// <param name="behined"></param>
        /// <returns></returns>
        public static string FindBetween(this string str, string front, string behined)
        {
            var startIndex = str.IndexOf(front) + front.Length;
            var endIndex = str.IndexOf(behined);
            if (startIndex < 0 || endIndex < 0)
                return str;
            return str.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// 查找在字符后面的字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="front"></param>
        /// <returns></returns>
        public static string FindAfter(this string str, string front)
        {
            var startIndex = str.IndexOf(front) + front.Length;
            if (startIndex < 0)
                return str;
            return str.Substring(startIndex);
        }

        /// <summary>
        /// 连接所有字符串
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="join"></param>
        /// <returns></returns>
        public static string ConcatAll(this string[] origin, string join = "")
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < origin.Length; i++)
            {
                if (i != 0 && !join.IsNullOrEmptyR())
                {
                    result.Append(join);
                }

                result.Append(origin[i]);
            }

            return result.ToString();
        }

        /// <summary>
        /// 获取所有符合条件的子行
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="beginRegex"></param>
        /// <param name="endRegex"></param>
        /// <returns></returns>
        public static List<string> GetSubLines(string origin, string beginRegex, string endRegex = null, bool containLastLine = true)
        {
            return endRegex != null ? 
                GetSubLines(origin, str => Regex.IsMatch(str, beginRegex), str => Regex.IsMatch(str, endRegex), containLastLine) : 
                GetSubLines(origin, str => Regex.IsMatch(str, beginRegex), null, containLastLine);
        }

        /// <summary>
        /// 获取所有符合Condition的子行
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="beginCondition">开始条件</param>
        /// <param name="endCondition">结束条件</param>
        /// <param name="containLastLine">是否包含符合End正则的行</param>
        /// <returns></returns>
        public static List<string> GetSubLines(string origin, Func<string, bool> beginCondition, Func<string, bool> endCondition = null, bool containLastLine = true)
        {
            List<string> result = new List<string>();
            StringBuilder subLines = new StringBuilder();
            StringReader reader = new StringReader(origin);
            bool isCollecting = false;
            do
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }
                if (!isCollecting)
                {
                    if (beginCondition(line))
                    {
                        subLines.AppendLine(line);
                        isCollecting = true;
                    }
                }
                else
                {
                    if ((endCondition != null && endCondition(line)) 
                        || (endCondition == null && string.IsNullOrEmpty(line)))
                    {
                        if (containLastLine)
                        {
                            subLines.AppendLine(line);
                        }
                        isCollecting = false;
                        result.Add(subLines.ToString());
                        subLines.Clear();
                    }
                    else
                    {
                        subLines.AppendLine(line);
                    }
                }            
                
            } while (true);

            return result;
        }

    }

}