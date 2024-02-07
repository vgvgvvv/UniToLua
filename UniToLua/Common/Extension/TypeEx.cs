using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Common
{
    public static class TypeEx
    {
        /// <summary>
        /// 获取默认值
        /// </summary>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static object DefaultForType(this Type targetType)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        public static bool IsGeneratedClass(this Type type)
        {
            var attr = Attribute.GetCustomAttribute(type, typeof(CompilerGeneratedAttribute));
            return attr != null;
        }

        /// <summary>
        /// 获取友好的Type名
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetFriendlyFullName(this Type type)
        {
            string fullName = type.FullName;

            return GetFriendlyFullName(fullName);
        }

        /// <summary>
        /// 纯字符串计算，不支持泛型嵌套
        /// </summary>
        /// <param name="originFullName"></param>
        /// <returns></returns>
        private static string GetFriendlyFullName(string originFullName)
        {
            if (originFullName == null)
            {
                return null;
            }
            int beginOfGeneric = originFullName.IndexOf('[');
            if (beginOfGeneric > 0)
            {

                // xx`1+xx`2+xx`3
                var allTypeString = originFullName.Substring(0, beginOfGeneric);
                // {xx`1, xx`2, xx`3}
                var allTypeStringArray = allTypeString.Split('+');

                // [],[],[]
                var allGenericTypeString = originFullName.Substring(beginOfGeneric + 1, originFullName.Length - beginOfGeneric - 2);

                List<string> typeInfoList = new List<string>();
                {
                    var temp = allGenericTypeString;
                    while (temp.Length > 0)
                    {
                        int left = temp.IndexOf('[');
                        int right = temp.IndexOf(']');
                        var typeInfo = temp.Substring(left + 1, right - left - 1);
                        var typeName = typeInfo.Substring(0, typeInfo.IndexOf(','));
                        typeInfoList.Add(typeName);
                        temp = temp.Substring(right + 1);
                    }
                }

                int currentGenericIndex = 0;
                int currentType = 0;
                StringBuilder friendlyName = new StringBuilder();
                foreach (var typeString in allTypeStringArray)
                {
                    if (currentType != 0)
                    {
                        friendlyName.Append(".");
                    }
                    currentType++;
                    var temp = typeString.Split('`');
                    var typeName = temp[0];
                    friendlyName.Append(typeName);
                    if (temp.Length > 1)
                    {
                        friendlyName.Append("<");

                        var genericNum = int.Parse(temp[1]);
                        for (int i = 0; i < genericNum; i++)
                        {
                            if (i != 0)
                            {
                                friendlyName.Append(",");
                            }

                            friendlyName.Append(typeInfoList[currentGenericIndex]);
                            currentGenericIndex++;
                        }

                        friendlyName.Append(">");
                    }
                }

                return friendlyName.ToString();
            }
            else
            {
                return originFullName.Replace('+', '.');
            }
        }

        /// <summary>
        /// 获取友好的Type名
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetFriendlyName(this Type type)
        {
            string friendlyName = type.Name;
            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyFullName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName;
        }

        /// <summary>
        /// 通过CodeDom获取，支持任何类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTypeNameFromCodeDom(this Type type)
        {
            var codeDomProvider = CodeDomProvider.CreateProvider("C#");
            var typeReferenceExpression = new CodeTypeReferenceExpression(new CodeTypeReference(type));
            using (var writer = new StringWriter())
            {
                codeDomProvider.GenerateCodeFromExpression(typeReferenceExpression, writer, new CodeGeneratorOptions());
                return writer.GetStringBuilder().ToString();
            }
        }

        public static bool IsDelegate(this Type type)
        {
            return type.IsSubclassOf(typeof(Delegate));
        }

        public static bool IsNumber(this Type type)
        {
            return type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double);
        }

    }
}