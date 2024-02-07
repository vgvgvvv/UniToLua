using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class ReflectEx
    {
        /// <summary>
        /// 通过反射方式调用函数
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName">方法名</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static object InvokeByReflect(this object obj, string methodName, params object[] args)
        {
            MethodInfo methodInfo = obj.GetType().GetMethod(methodName);
            if (methodInfo == null) return null;
            return methodInfo.Invoke(obj, args);
        }

        /// <summary>
        /// 通过反射方式获取域值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fieldName">域名</param>
        /// <returns></returns>
        public static object GetFieldByReflect(this object obj, string fieldName)
        {
            FieldInfo fieldInfo = obj.GetType().GetField(fieldName);
            if (fieldInfo == null) return null;
            return fieldInfo.GetValue(obj);
        }

        /// <summary>
        /// 通过反射方式获取属性
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fieldName">属性名</param>
        /// <returns></returns>
        public static object GetPropertyByReflect(this object obj, string propertyName, object[] index = null)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName);
            if (propertyInfo == null) return null;
            return propertyInfo.GetValue(obj, index);
        }

        /// <summary>
        /// 拥有特性
        /// </summary>
        /// <returns></returns>
        public static bool HasAttribute(this PropertyInfo prop, Type attributeType, bool inherit = true)
        {
            return prop.GetCustomAttributes(attributeType, inherit).Length > 0;
        }

        /// <summary>
        /// 拥有特性
        /// </summary>
        /// <returns></returns>
        public static bool HasAttribute(this FieldInfo field, Type attributeType, bool inherit = true)
        {
            return field.GetCustomAttributes(attributeType, inherit).Length > 0;
        }

        /// <summary>
        /// 拥有特性
        /// </summary>
        /// <returns></returns>
        public static bool HasAttribute(this Type type, Type attributeType, bool inherit = true)
        {
            return type.GetCustomAttributes(attributeType, inherit).Length > 0;
        }

        /// <summary>
        /// 拥有特性
        /// </summary>
        /// <returns></returns>
        public static bool HasAttribute(this MethodInfo method, Type attributeType, bool inherit = true)
        {
            return method.GetCustomAttributes(attributeType, inherit).Length > 0;
        }


        /// <summary>
        /// 获取第一个特性
        /// </summary>
        public static T GetFirstAttribute<T>(this MethodInfo method, bool inherit = true) where T : Attribute
        {
            var attrs = (T[])method.GetCustomAttributes(typeof(T), inherit);
            if (attrs != null && attrs.Length > 0)
                return attrs[0];
            return null;
        }

        /// <summary>
        /// 获取第一个特性
        /// </summary>
        public static T GetFirstAttribute<T>(this FieldInfo field, bool inherit) where T : Attribute
        {
            var attrs = (T[])field.GetCustomAttributes(typeof(T), inherit);
            if (attrs != null && attrs.Length > 0)
                return attrs[0];
            return null;
        }

        /// <summary>
        /// 获取第一个特性
        /// </summary>
        public static T GetFirstAttribute<T>(this PropertyInfo prop, bool inherit) where T : Attribute
        {
            var attrs = (T[])prop.GetCustomAttributes(typeof(T), inherit);
            if (attrs != null && attrs.Length > 0)
                return attrs[0];
            return null;
        }

        /// <summary>
        /// 获取第一个特性
        /// </summary>
        public static T GetFirstAttribute<T>(this Type type, bool inherit) where T : Attribute
        {
            var attrs = (T[])type.GetCustomAttributes(typeof(T), inherit);
            if (attrs != null && attrs.Length > 0)
                return attrs[0];
            return null;
        }

        /// <summary>
        /// 通过反射进行new
        /// </summary>
        /// <param name="type"></param>
        /// <param name="argTypes"></param>
        /// <param name="argObjects"></param>
        /// <returns></returns>
        public static object NewByReflect(this Type type, Type[] argTypes = null, object[] argObjects = null)
        {
            argTypes = argTypes ?? Array.Empty<Type>();
            argObjects = argObjects ?? Array.Empty<object>();

            var cons = type.GetConstructor(argTypes);
            if (cons == null)
            {
                return null;
            }

            return cons.Invoke(argObjects);
        }

        /// <summary>
        /// 通过反射进行深比较, 只处理Field
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="fieldCondition"></param>
        /// <param name="extraCondition"></param>
        /// <returns></returns>
        public static bool IsSameByReflect(this object a, object b, Func<FieldInfo, bool> fieldCondition = null, Func<object, object, bool> extraCondition = null)
        {
            if (extraCondition != null && !extraCondition(a, b))
            {
                return true;
            }
            
            if (a == null || b == null)
            {
                if (a == b)
                {
                    return true;
                }
                return false;
            }
            
            var aType = a.GetType();
            var bType = b.GetType();

            if (aType != bType)
            {
                return false;
            }

            if (aType.IsPrimitive || a is string)
            {
                return a.Equals(b);
            }

            if (a is ICollection aCollection && b is ICollection bCollection)
            {
                if (a is IDictionary aDict && b is IDictionary bDict)
                {
                    if (aDict.Count != bDict.Count)
                    {
                        return false;
                    }
            
                    var aKeys = aDict.Keys.Cast<object>().ToList();
                    var bKeys = bDict.Keys.Cast<object>().ToList();

                    var isSameKey = IsSameCollection(aKeys, bKeys);
                    if (!isSameKey)
                    {
                        return false;
                    }
            
                    foreach (var aKey in aKeys)
                    {
                        if (!aDict[aKey].IsSameByReflect(bDict[aKey]))
                        {
                            return false;
                        }
                    }
            
                    return true;
                }
                
                return IsSameCollection(aCollection, bCollection);
            }
            
            var fields = aType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            if (fields.Length == 0)
            {
                return a.Equals(b);
            }
            
            if (fieldCondition != null)
            {
                fields = fields.Where(f => fieldCondition(f)).ToArray();
            }
           
            foreach (var field in fields)
            {
                var aValue = field.GetValue(a);
                var bValue = field.GetValue(b);
                if (!aValue.IsSameByReflect(bValue))
                {
                    return false;
                }
            }

            return true;

        }

        public static bool IsSameCollection(this ICollection a, ICollection b)
        {
            var aList = a.Cast<object>().ToList();
            var bList = b.Cast<object>().ToList();

            while (aList.Count > 0)
            {
                bool find = false;
                for (int i = 0; i < aList.Count; i++)
                {
                    for (int n = 0; n < bList.Count; n++)
                    {
                        if (aList[i].IsSameByReflect(bList[n]))
                        {
                            aList.RemoveAt(i);
                            bList.RemoveAt(n);
                            find = true;
                            break;
                        }
                    }
                }

                if (find)
                {
                    if (aList.Count == 0)
                    {
                        return true;
                    }
                    continue;
                }

                return false;
            }

            return true;

        }

        // public static bool IsUnsafe(this Type type)
        // {
        //     if (type.GetMethods().Any(m => m.IsUnsafe()))
        //     {
        //         return true;
        //     }
        //
        //     if (type.GetFields().Any(f => f.FieldType.Name.Contains("FixedBuffer") || f.FieldType.IsPointer))
        //     {
        //         return true;
        //     }
        //     
        //     if (type.GetProperties().Any(f => f.PropertyType.Name.Contains("FixedBuffer") || f.PropertyType.IsPointer))
        //     {
        //         return true;
        //     }
        //
        //     return false;
        // }
        
        public static bool IsUnsafe(this MethodInfo methodInfo)
        {
            if (HasUnsafeParameters(methodInfo))
            {
                return true;
            }

            return methodInfo.ReturnType.IsPointer;
        }

        public static bool IsUnsafe(this FieldInfo fieldInfo)
        {
            if (fieldInfo.FieldType.Name.Contains("FixedBuffer") || fieldInfo.FieldType.IsPointer)
            {
                return true;
            }

            return false;
        }
        
        public static bool IsUnsafe(this PropertyInfo property)
        {
            if (property.PropertyType.Name.Contains("FixedBuffer") || property.PropertyType.IsPointer)
            {
                return true;
            }

            return false;
        }
        
        public static bool HasUnsafeParameters(MethodBase methodBase)
        {
            var parameters = methodBase.GetParameters();
            bool hasUnsafe = parameters.Any(p => p.ParameterType.IsPointer);

            return hasUnsafe;
        }

    }
}
