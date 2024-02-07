using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UniToLua.Common;
using UniLua;

namespace UniToLuaGener
{
    public partial class ExportToLua
    {

        private static void GenStaticLib(Type libType, string outputPath)
        {
            if (libType == null)
                return;
            var className = GetClassFileName(libType);

            if (string.IsNullOrEmpty(className))
            {
                Log.Warning($"{libType.FullName} is not a valid static library to export lua wrapper");
                return;
            }
            
            CodeGener gener = new CodeGener("UniToLua", className);

            GetAllNeedNamespace(libType).ForEach(ns => { gener.AddImport(ns); });

            // Fields
            var fields = libType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => !IsObsolete(field) && !field.IsUnsafe())
                .ToList();
            
            // Properties
            var propertys = libType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                // IndexParameters > 0 is indexer
                .Where(prop=>prop.GetIndexParameters().Length == 0 && !IsObsolete(prop) && !prop.IsUnsafe())
                .ToList();

            var events = libType.GetEvents(BindingFlags.Public | BindingFlags.Static)
                .Where(eve => !IsObsolete(eve))
                .ToList();
            
            //Methods
            var methodGroups = libType.GetMethods(BindingFlags.Public | BindingFlags.Static).Where((method) =>
            {
                if (libType.GetProperties().Count(prop => prop.GetMethod == method || prop.SetMethod == method) != 0)
                {
                    return false;
                }

                if (libType.GetEvents().Count(eve => eve.AddMethod == method || eve.RemoveMethod == method) != 0)
                {
                    return false;
                }
                
                foreach (var ignoreParamType in IgnoreParamTypes)
                {
                    var paramType = ignoreParamType;
                    if (method
                        .GetParameters()
                        .Any(param =>
                        {
                            if (paramType.IsGenericType && param.ParameterType.IsGenericType)
                            {
                                return param.ParameterType.GetGenericTypeDefinition() == paramType;
                            }

                            return param.ParameterType == paramType;
                        }))
                    {
                        return false;
                    }

                }
                
                if (IsObsolete(method))
                {
                    return false;
                }
                if (method.ContainsGenericParameters)
                {
                    return false;
                }
                if (method.GetParameters().Any(par => par.IsOut || par.ParameterType.IsByRef))
                {
                    //TODO 暂时不支持out、ref类型
                    return false;
                }
                
                if (method.IsUnsafe())
                {
                    return false;
                }
                
                //static lib不可能有indexer 不做相应判断
                return true;
            }).GroupBy(mi=>mi.Name).ToArray();

            List<CodeStatement> registerMethodStatement = new List<CodeStatement>();
            registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.BeginStaticLib(\"{libType.GetFriendlyName()}\");"));

            foreach (var fieldInfo in fields)
            {
                bool canSet = !fieldInfo.IsLiteral && !fieldInfo.IsInitOnly;
                if (canSet)
                {
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegVar(\"{fieldInfo.Name}\", get_{fieldInfo.Name}, set_{fieldInfo.Name});"));
                }
                else
                {
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegVar(\"{fieldInfo.Name}\", get_{fieldInfo.Name}, null);"));
                }
                
            }

            foreach (var propertyInfo in propertys)
            {
                StringBuilder builder = new StringBuilder($"\t\t\tL.RegVar(\"{propertyInfo.Name}\", ");
                if (propertyInfo.GetMethod != null && propertyInfo.GetMethod.IsPublic)
                {
                    builder.Append($"get_{propertyInfo.Name}, ");
                }
                else
                {
                    builder.Append("null, ");
                }
                if (propertyInfo.SetMethod != null && propertyInfo.SetMethod.IsPublic)
                {
                    builder.Append($"set_{propertyInfo.Name}");
                }
                else
                {
                    builder.Append("null");
                }
                builder.Append(");");
                registerMethodStatement.Add(new CodeSnippetStatement(builder.ToString()));
            }

            foreach (var eventInfo in events)
            {
                // registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{eventInfo.Name}_Invoke\", invoke_{eventInfo.Name});"));
                
                var addMethod = eventInfo.AddMethod;
                if ( addMethod != null && addMethod.IsPublic)
                {
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{eventInfo.Name}_Add\", add_{eventInfo.Name});"));
                }

                var removeMethod = eventInfo.RemoveMethod;
                if (removeMethod != null && removeMethod.IsPublic)
                {
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{eventInfo.Name}_Remove\", remove_{eventInfo.Name});"));
                }
            }
            
            foreach (var methodGroup in methodGroups)
            {
                registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{methodGroup.Key}\", {methodGroup.Key});"));
            }

            registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.EndStaticLib();"));

            gener.AddMemberMethod(typeof(void), "Register", new Dictionary<string, Type>() { { "L", typeof(ILuaState) } },
                MemberAttributes.Public | MemberAttributes.Static, registerMethodStatement.ToArray());

            foreach (var fieldInfo in fields)
            {
                GenRegStaticField(gener, libType, fieldInfo);
            }

            foreach (var propertyInfo in propertys)
            {
                GenRegStaticProperty(gener, libType, propertyInfo);
            }

            foreach (var eventInfo in events)
            {
                GenRegStaticEvent(gener, libType, eventInfo);
            }

            foreach (var methodGroup in methodGroups)
            {
                GenRegFunction(gener, libType, methodGroup.ToArray());
            }

            gener.GenCSharp(outputPath);
        }

    }
}