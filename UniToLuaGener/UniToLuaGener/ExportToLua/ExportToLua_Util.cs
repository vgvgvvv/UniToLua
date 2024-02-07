using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common;
using UniLua;

namespace UniToLuaGener
{
    public partial class ExportToLua
    {

        internal static int MaxCheckTypeArgNum = 9;
        
        /// <summary>
        /// 获取Push方法对应的String
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static string GetPushString(Type type, CodeGener gener)
        {
            return $"PushAny<{GetSafeClassFriendlyFullName(type, gener)}>";
        }

        /// <summary>
        /// 获取Check方法对应的String
        /// </summary>
        /// <returns></returns>
        internal static string GetCheckString(Type type, CodeGener gener)
        {
            return $"CheckAny<{GetSafeClassFriendlyFullName(type, gener)}>";
        }


        private static bool IsObsolete(MemberInfo mb)
        {
            object[] attrs = mb.GetCustomAttributes(true);

            for (int j = 0; j < attrs.Length; j++)
            {
                Type t = attrs[j].GetType();

                if (t == typeof(System.ObsoleteAttribute) || t == typeof(NoToLuaAttribute) ||
                    t.Name == "MonoNotSupportedAttribute" || t.Name == "MonoTODOAttribute") // || t.ToString() == "UnityEngine.WrapperlessIcall")
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetSafeClassFriendlyFullName(Type type, CodeGener gener)
        {
            if (gener != null)
            {
                GetAllNeedNamespace(type).ForEach(ns => gener.AddImport(ns));
            }
            return type.GetTypeNameFromCodeDom();
        }

        internal static string GetSafeTypeFullName(Type type, CodeGener gener)
        {
            return GetSafeClassFriendlyFullName(type, gener)
                .Replace('`', '0')
                .Replace('.', '1')
                .Replace('+', '2')
                .Replace('<', '3')
                .Replace('>', '4')
                .Replace('[', '5')
                .Replace(']', '6')
                .Replace(',', '7')
                .RemoveString(" ");
        }

        internal static List<string> GetAllNeedNamespace(params Type[] types)
        {
            List<string> result = new List<string>();
            foreach (var type in types)
            {
                result.Add(type.Namespace);
                if (type.IsGenericType)
                {
                    var genericTypes = type.GetGenericArguments();
                    result.AddRange(GetAllNeedNamespace(genericTypes));
                }
            }

            return result.Distinct().ToList();
        }

        internal static List<MethodInfo> FindExtensionMethods(Type owner, List<Type> searchTypes, out List<Type> extensionType)
        {
            List<MethodInfo> result = new List<MethodInfo>();
            extensionType = new List<Type>();
            foreach (var type in searchTypes)
            {
                if (type.IsSealed && type.IsAbstract)
                {
                    var extensionMethods = type.GetMethods().Where(method =>
                    {
                        var pars = method.GetParameters();
                        return method.IsPublic &&
                               method.IsStatic &&
                               !IsObsolete(method) &&
                               pars.Length > 0 &&
                               pars[0].ParameterType.IsAssignableFrom(owner);
                    });
                    result.AddRange(extensionMethods);
                    if (result.Count > 0)
                    {
                        extensionType.Add(type);
                    }
                }
            }

            return result;
        }

        internal static string GetClassFileName(Type type)
        {
            if (type == null)
                return null;
            var className = GetSafeTypeFullName(type, null) + "Wrap";

            if (className.Contains("__"))
            {
                return string.Empty;
            }
            
            if (className.Contains("@"))
            {
                return string.Empty;
            }
            return className;
        }

    }
}