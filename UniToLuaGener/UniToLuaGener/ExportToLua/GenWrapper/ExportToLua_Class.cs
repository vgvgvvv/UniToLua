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

        private static void GenClass(Type classType, List<MethodInfo> extensionMethodInfos, List<Type> extensionTypes, string outputPath)
        {
            if (classType == null)
                return;
            var className = GetClassFileName(classType);

            if (string.IsNullOrEmpty(className))
            {
                Log.Warning($"{classType.FullName} is not a valid class to export lua wrapper");
                return;
            }
            
            CodeGener gener = new CodeGener("UniToLua", className);


            GetAllNeedNamespace(classType).ForEach(ns => { gener.AddImport(ns); });
            foreach (var extensionType in extensionTypes)
            {
                GetAllNeedNamespace(extensionType).ForEach(ns => { gener.AddImport(ns); });
            }

            var baseClassName = classType.BaseType == typeof(System.Object) || classType.BaseType == null
                ? "null"
                : $"typeof({GetSafeClassFriendlyFullName(classType.BaseType, gener)})";
            
            var fields = classType.GetFields().Where(field => !IsObsolete(field) && !field.IsUnsafe()).ToList();
            
            var propertys = classType.GetProperties()
                .Where(prop=>prop.GetIndexParameters().Length == 0 && !IsObsolete(prop) && !prop.IsUnsafe()).ToList();

            var events = classType.GetEvents().Where(eve=>!IsObsolete(eve)).ToList();
            
            var indexers = classType.GetProperties()
                .Where(prop => prop.GetIndexParameters().Length > 0 && !IsObsolete(prop)).ToList();
            
            var methodInfos = classType.GetMethods().ToList();
            methodInfos.AddRange(extensionMethodInfos);
            var methodGroups = methodInfos.Where((method) =>
            {
                if (classType.GetProperties().Count(prop => prop.GetMethod == method || prop.SetMethod == method) != 0)
                {
                    return false;
                }

                if (classType.GetEvents()
                    .Count(eve => eve.GetAddMethod() == method || eve.GetRemoveMethod() == method) != 0)
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

                if (method.Name == "get_Item" || method.Name == "set_Item")
                {
                    if(indexers.Count > 0)
                    {
                        return false;
                    }
                }
                
                if(method.GetParameters().Any(par =>par.IsOut || par.ParameterType.IsByRef))
                {
                    //TODO 暂时不支持out、ref类型
                    return false;
                }

                if (method.IsUnsafe())
                {
                    return false;
                }

                return true;
            }).GroupBy(mi => mi.Name).ToArray();

            List<CodeStatement> registerMethodStatement = new List<CodeStatement>();

            registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.BeginClass(typeof({GetSafeClassFriendlyFullName(classType, gener)}), {baseClassName});"));

            registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"New\", _Create{GetSafeTypeFullName(classType, gener)});"));

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

                if (fieldInfo.FieldType.IsSubclassOf(typeof(Delegate)))
                {
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{fieldInfo.Name}_Invoke\", invoke_{fieldInfo.Name});"));
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{fieldInfo.Name}_Add\", add_{fieldInfo.Name});"));
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{fieldInfo.Name}_Remove\", remove_{fieldInfo.Name});"));
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
                
                if (propertyInfo.PropertyType.IsSubclassOf(typeof(Delegate)))
                {
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{propertyInfo.Name}_Invoke\", invoke_{propertyInfo.Name});"));
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{propertyInfo.Name}_Add\", add_{propertyInfo.Name});"));
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{propertyInfo.Name}_Remove\", remove_{propertyInfo.Name});"));
                }
                
            }

            foreach (var eventInfo in events)
            {
                
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
                if (OperationInfo.IsOperation(methodGroup.Key))
                {
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{OperationInfo.OperationInfos[methodGroup.Key].LuaFuncName}\", {methodGroup.Key});"));
                }
                else
                {
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{methodGroup.Key}\", {methodGroup.Key});"));
                }
            }

            //Indexers
            {
                bool NeedGet = indexers
                    .Any(indexer => indexer.GetMethod != null && indexer.GetMethod.IsPublic);
                bool NeedSet = indexers
                    .Any(indexer => indexer.SetMethod != null && indexer.SetMethod.IsPublic);
                if (NeedGet)
                {
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"GetItem\", get_Item);"));
                }
            
                if (NeedSet)
                {
                    registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"SetItem\", set_Item);"));
                }
            }

            registerMethodStatement.Add(new CodeSnippetStatement("\t\t\tL.EndClass();"));

            gener.AddMemberMethod(typeof(void), "Register", new Dictionary<string, Type>() { { "L", typeof(ILuaState) } },
                MemberAttributes.Public | MemberAttributes.Static, registerMethodStatement.ToArray());

            GenConstructor(gener, classType);

            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.IsStatic)
                {
                    GenRegStaticField(gener, classType, fieldInfo);
                }
                else
                {
                    GenRegMemberField(gener, classType, fieldInfo);
                }
            }

            foreach (var propertyInfo in propertys)
            {
                if ((propertyInfo.GetMethod != null && propertyInfo.GetMethod.IsStatic) ||
                    (propertyInfo.SetMethod != null && propertyInfo.SetMethod.IsStatic))
                {
                    GenRegStaticProperty(gener, classType, propertyInfo);
                }
                else
                {
                    GenRegMemberProperty(gener, classType, propertyInfo);
                }

            }

            foreach (var eventInfo in events)
            {
                if ((eventInfo.AddMethod != null && eventInfo.AddMethod.IsStatic) ||
                    (eventInfo.RemoveMethod != null && eventInfo.RemoveMethod.IsStatic) || 
                    (eventInfo.RaiseMethod != null && eventInfo.RaiseMethod.IsStatic))
                {
                    GenRegStaticEvent(gener, classType, eventInfo);
                }
                else
                {
                    GenRegMemberEvent(gener, classType, eventInfo);
                }
            }

            foreach (var methodGroup in methodGroups)
            {
                if (!methodGroup.Any())
                {
                    continue;
                }

                GenRegFunction(gener, classType, methodGroup.ToArray(), false);

            }


            // Indexer
            {
                var getMethodGroup = indexers
                    .Select(indexer => indexer.GetMethod)
                    .Where(method => method != null && method.IsPublic)
                    .ToArray();
                if (getMethodGroup.Length > 0)
                {
                    GenRegFunction(gener, classType, getMethodGroup.ToArray(), true);
                }
                var setMethodGroup = indexers
                    .Select(indexer => indexer.SetMethod)
                    .Where(method => method != null && method.IsPublic)
                    .ToArray();
                if (setMethodGroup.Length > 0)
                {
                    GenRegFunction(gener, classType, setMethodGroup.ToArray(), true);
                }
            }

            gener.GenCSharp(outputPath);

        }

    }
}