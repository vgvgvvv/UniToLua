using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniToLua.Common;
using UniLua;

namespace UniToLuaGener
{
    public partial class ExportToLua
    {

        public static void GenDelegateFactory(List<Type> targetTypeList, string outputPath)
        {
            List<Type> delegateTypes = new List<Type>();
            List<Type> checkedTypes = new List<Type>();
            GetAllDelegateType(targetTypeList, ref delegateTypes, ref checkedTypes);

            delegateTypes = delegateTypes.Where(type => type.FullName != null).Distinct().ToList();

            List<string> addedTypeNames = new List<string>(); 
            List<Type> finalTypeList = new List<Type>();
            foreach (var type in delegateTypes)
            {
                if (!addedTypeNames.Contains(type.FullName))
                {
                    addedTypeNames.Add(type.FullName);
                    finalTypeList.Add(type);
                }
            }
            
            CodeGener gener = new CodeGener("UniToLua", "DelegateFactory");
            
            GetAllNeedNamespace(delegateTypes.ToArray()).ForEach(ns => { gener.AddImport(ns); });
            
            gener.AddMemberMethod(typeof(void), "Init", new Dictionary<string, Type>(){ ["L"] = typeof(LuaState) },
                MemberAttributes.Public | MemberAttributes.Static, 
                new CodeSnippetStatement("\t\t\tRegister(L);"));
            
            GenRegister(gener, finalTypeList);
            GenCSharpFunctionDelegate(gener, finalTypeList);
            GenLuaDelegate(gener, finalTypeList);
            
            gener.GenCSharp(outputPath);
        }

        private static void GenRegister(CodeGener gener, List<Type> targetTypeList)
        {
            var code = new List<string>();

            code.Add("\t\t\tL.createLuaDelegateDict.Clear();");
            code.Add("\t\t\tL.csFunctionDelegateDict.Clear();");

            
            foreach (var type in targetTypeList)
            {
                var typeCreateLuaDelegateFnucName = GetGetLuaDelegateFuncName(type, gener);
                code.Add($"\t\t\tL.createLuaDelegateDict.Add(typeof({type.GetTypeNameFromCodeDom()}), {typeCreateLuaDelegateFnucName});");

                var typeCSFuncDelegateName = GetCSFunctionDelegateName(type, gener);
                code.Add($"\t\t\tL.csFunctionDelegateDict.Add(typeof({type.GetTypeNameFromCodeDom()}), {typeCSFuncDelegateName});");
            }

            var statements = code
                .Select(str => new CodeSnippetStatement(str))
                .Cast<CodeStatement>()
                .ToArray();

            gener.AddMemberMethod(typeof(void), "Register", new Dictionary<string, Type>() { ["L"] = typeof(LuaState) },
                MemberAttributes.Public | MemberAttributes.Static,
                statements
            );
        }

        private static void GenCSharpFunctionDelegate(CodeGener gener, List<Type> targetTypeList)
        {
            foreach (var type in targetTypeList)
            {
                if (!type.IsDelegate())
                {
                    return;
                }

                List<string> codes = new List<string>();
                var invokeMethod = type.GetMethod("Invoke");
                var funcName = GetCSFunctionDelegateName(type, gener);

                var parameters = invokeMethod.GetParameters();

                codes.Add("\t\t\treturn (UniLua.ILuaState L)=>{");

                StringBuilder checkSb = new StringBuilder();
                checkSb.Append($"L.CheckNum({parameters.Length})");

                if (parameters.Length > 0)
                {
                    checkSb.Append($" && L.CheckType<");
                    StringBuilder typeArgs = new StringBuilder();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (i != 0)
                        {
                            typeArgs.Append(", ");
                        }
                        typeArgs.Append(GetSafeClassFriendlyFullName(parameters[i].ParameterType, gener));
                    }
                    checkSb.Append(typeArgs);
                    checkSb.Append($">(1)");
                }

                codes.Add($"\t\t\t\tif({checkSb})\n\t\t\t\t{{");

                if (invokeMethod.ReturnType != typeof(void))
                {
                    codes.Add($"\t\t\t\t\t{GetSafeClassFriendlyFullName(invokeMethod.ReturnType, gener)} result;");
                }

                Action<int, string> addCallWithArgNum = (int argNum, string offset) =>
                {
                    var paramBuilder = new StringBuilder();
                    for (int i = 1; i <= argNum; i++)
                    {
                        if (i != 1)
                        {
                            paramBuilder.Append(", ");
                        }
                        paramBuilder.Append($"arg{i}");
                    }

                    codes.Add($"{offset}\t\t\t\t\tvar dele = ({type.GetTypeNameFromCodeDom()})deleArg;");
                    if (invokeMethod.ReturnType == typeof(void))
                    {
                        codes.Add($"{offset}\t\t\t\t\tdele.Invoke({paramBuilder});");
                        codes.Add($"{offset}\t\t\t\t\treturn 0;");
                    }
                    else
                    {
                        codes.Add($"{offset}\t\t\t\t\tresult = dele.Invoke({paramBuilder});");
                        codes.Add($"{offset}\t\t\t\t\tL.{GetPushString(invokeMethod.ReturnType, gener)}(result);");
                        codes.Add($"{offset}\t\t\t\t\treturn 1;");
                    }

                };

                for (int curArgIndex = 1; curArgIndex <= parameters.Length; curArgIndex++)
                {
                    var paramInfo = parameters[curArgIndex - 1];
                    codes.Add($"\t\t\t\t\tvar arg{curArgIndex} = L.{GetCheckString(paramInfo.ParameterType, gener)}({curArgIndex});");
                }

                addCallWithArgNum(parameters.Length, "");

                codes.Add("\t\t\t\t}");
                codes.Add("\t\t\t\tL.L_Error(\"Invoke delegate args is error\");");
                codes.Add("\t\t\t\treturn 1;");

                codes.Add("\t\t\t};");
                var statements = codes
                    .Select(str => new CodeSnippetStatement(str))
                    .Cast<CodeStatement>()
                    .ToArray();

                gener.AddMemberMethod(typeof(CSharpFunctionDelegate), funcName,
                    new Dictionary<string, Type>() { { "deleArg", typeof(Delegate) } }, MemberAttributes.Private | MemberAttributes.Static,
                    statements);
            }
        }

        private static void GenLuaDelegate(CodeGener gener, List<Type> targetTypeList)
        {
            foreach (var type in targetTypeList)
            {
                if (!type.IsDelegate())
                {
                    return;
                }

                List<string> codes = new List<string>();
                var invokeMethod = type.GetMethod("Invoke");
                var funcName = GetGetLuaDelegateFuncName(type, gener);

                var parameters = invokeMethod.GetParameters();

                StringBuilder typeArgs = new StringBuilder();
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i != 0)
                    {
                        typeArgs.Append(", ");
                    }
                    typeArgs.Append(GetSafeClassFriendlyFullName(parameters[i].ParameterType, gener)).Append(" ").Append($"arg{i+1}");
                }

                codes.Add($"\t\t\treturn ({typeArgs}) => \n\t\t\t{{");

                int returnNum = invokeMethod.ReturnType == typeof(void) ? 0 : 1;

                codes.Add($"\t\t\t\tL.{GetPushString(typeof(LuaLClosureValue), gener)}(closure);");
                for (int i = 0; i < parameters.Length; i++)
                {
                    codes.Add($"\t\t\t\tL.{GetPushString(parameters[i].ParameterType, gener)}(arg{i+1});");
                }
                codes.Add($"\t\t\t\tL.Call({parameters.Length}, {returnNum});");

                if (returnNum != 0)
                {
                    codes.Add($"\t\t\t\treturn L.{GetCheckString(invokeMethod.ReturnType, gener)}(-1);");
                }

                codes.Add($"\t\t\t}};");


                var statements = codes
                    .Select(str => new CodeSnippetStatement(str))
                    .Cast<CodeStatement>()
                    .ToArray();
                gener.AddMemberMethod(type, funcName,
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) }, { "closure", typeof(LuaLClosureValue) } }, MemberAttributes.Private | MemberAttributes.Static,
                    statements);
            }
        }

        private static string GetGetLuaDelegateFuncName(Type t, CodeGener gener)
        {
            return $"GetLuaDelegate_{GetSafeTypeFullName(t, gener)}";
        }

        private static string GetCSFunctionDelegateName(Type t, CodeGener gener)
        {
            return $"CSFuncDelegate_{GetSafeTypeFullName(t, gener)}";
        }

        /// <summary>
        /// 获取类型中关联的所有的Delegate类型用于导出
        /// </summary>
        /// <param name="targetTypeList"></param>
        /// <returns></returns>
        public static void GetAllDelegateType(List<Type> targetTypeList, ref List<Type> result, ref List<Type> checkedTypes)
        {
            foreach (var type in targetTypeList)
            {
                if (checkedTypes.Contains(type))
                {
                    continue;
                }
                checkedTypes.Add(type);
                if (type.IsSubclassOf(typeof(Delegate)))
                {
                    result.Add(type);
                }
                else if (!type.IsEnum && !type.IsInterface && !type.IsPrimitive)
                {
                    var eventInfos = type.GetEvents();
                    var eventTypes = eventInfos.Select(eve => eve.EventHandlerType).ToList();
                    GetAllDelegateType(eventTypes, ref result, ref checkedTypes);

                    var constructors = type.GetConstructors();
                    foreach (var constructorInfo in constructors)
                    {
                        var parameterTypes = constructorInfo.GetParameters().Select(param=>param.ParameterType).ToList();
                        foreach (var parameterType in parameterTypes)
                        {
                            if (parameterType.IsSubclassOf(typeof(Delegate)))
                            {
                                GetAllDelegateType(parameterTypes, ref result, ref checkedTypes);
                            }
                        }
                    }
                    
                    var methods = type.GetMethods();
                    foreach (var methodInfo in methods)
                    {
                       
                        if (methodInfo.ReturnType.IsSubclassOf(typeof(Delegate)))
                        {
                            GetAllDelegateType(new List<Type>() {methodInfo.ReturnType}, ref result, ref checkedTypes);
                        }

                        var parameterTypes = methodInfo.GetParameters().Select(param=>param.ParameterType).ToList();
                        foreach (var parameterType in parameterTypes)
                        {
                            if (parameterType.IsSubclassOf(typeof(Delegate)))
                            {
                                GetAllDelegateType(parameterTypes, ref result, ref checkedTypes);
                            }
                        }

                    }

                }

                if (type.IsGenericType)
                {
                    var genericArguments= type.GenericTypeArguments;
                    foreach (var genericArgument in genericArguments)
                    {
                        if (genericArgument.IsSubclassOf(typeof(Delegate)))
                        {
                            GetAllDelegateType(genericArguments.ToList(), ref result, ref checkedTypes);
                        }
                    }
                }
            }

        }

    }
}