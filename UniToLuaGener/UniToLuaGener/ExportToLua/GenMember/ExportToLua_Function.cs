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

        /// <summary>
        /// 生成函数
        /// </summary>
        /// <param name="gener"></param>
        /// <param name="type"></param>
        /// <param name="methodGroup"></param>
        /// <param name="isIndexer">是否为索引器的函数</param>
        private static void GenRegFunction(CodeGener gener, Type type, MethodInfo[] methodGroup, bool isIndexer = false)
        {
            var codes = new List<CodeStatement>();

            int count = 0;
            foreach (var methodInfo in methodGroup)
            {
                if (methodInfo.IsStatic)
                {
                    if (methodInfo.DeclaringType == type)
                    {
                        GenSingleStaticFunction(gener, type, codes, methodInfo, ref count);
                    }
                    else
                    {
                        GenSingleExtensionFunction(gener, type, codes, methodInfo, ref count);
                    }
                }
                else
                {
                    GenSingleMemberFunction(gener, type, codes, methodInfo, isIndexer, ref count);
                }
            }

            codes.Add(new CodeSnippetStatement($"\t\t\tL.L_Error(\"call function {methodGroup[0].Name} args is error\");"));
            codes.Add(new CodeSnippetStatement("\t\t\treturn 1;"));

            gener.AddMemberMethod(typeof(int), methodGroup[0].Name,
                new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static,
                codes.ToArray());
        }

        private static void GenSingleMemberFunction(CodeGener gener, Type type, List<CodeStatement> codes, MethodInfo methodInfo, bool isIndexer, ref int count)
        {
            var parameterInfos = methodInfo.GetParameters();

            var notOptionalArgNum = parameterInfos.Count(arg => !arg.IsOptional);
            bool hasOptional = notOptionalArgNum != parameterInfos.Length;

            StringBuilder checkStringBuilder = new StringBuilder();
            if (hasOptional)
            {
                checkStringBuilder.Append($"L.CheckRange({notOptionalArgNum + 1}, {parameterInfos.Length + 1})");
            }
            else
            {
                checkStringBuilder.Append($"L.CheckNum({parameterInfos.Length + 1})");
            }
            if (notOptionalArgNum > 0 && !parameterInfos[0].IsOptional)
            {
                checkStringBuilder.Append($" && L.CheckType<");
                StringBuilder typeArgs = new StringBuilder();
                typeArgs.Append(GetSafeClassFriendlyFullName(type, gener));
                for (int i = 0; i < Math.Min(parameterInfos.Length, MaxCheckTypeArgNum); i++)
                {
                    if (parameterInfos[i].IsOptional)
                    {
                        break;
                    }
                    typeArgs.Append(", ");
                    typeArgs.Append(GetSafeClassFriendlyFullName(parameterInfos[i].ParameterType, gener));
                }
                checkStringBuilder.Append(typeArgs);
                checkStringBuilder.Append($">(1)");
            }

            if (count == 0)
            {
                codes.Add(new CodeSnippetStatement($"\t\t\tif({checkStringBuilder})\n\t\t\t{{"));
            }
            else
            {
                codes.Add(new CodeSnippetStatement($"\t\t\telse if({checkStringBuilder})\n\t\t\t{{"));
            }

            if (hasOptional)
            {
                codes.Add(new CodeSnippetStatement($"\t\t\t\tvar top = L.GetTop();"));
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                codes.Add(new CodeSnippetStatement($"\t\t\t\t{GetSafeClassFriendlyFullName(methodInfo.ReturnType, gener)} result;"));
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

                
                if (methodInfo.ReturnType == typeof(void))
                {
                    if (isIndexer)
                    {
                        // index Set Method
                        var paramString = paramBuilder.ToString();
                        var last = paramString.LastIndexOf(",");
                        var left = paramString.Substring(0, last);
                        var right = paramString.Substring(last + 2);
                        codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\tobj[{left}] = {right};"));
                    }
                    else if (OperationInfo.IsOperation(methodInfo.Name))
                    {
                        var format = OperationInfo.OperationInfos[methodInfo.Name].InvokeStringFormat;
                        for (var i = 0; i < parameterInfos.Length; i++)
                        {
                            format = format.Replace(string.Format("{{{0}}}", i), $"arg{i+1}");
                        }
                        format = format.Replace("{type}", type.FullName);
                        codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\t{format};"));
                    }
                    else
                    {
                        codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\tobj.{methodInfo.Name}({paramBuilder});"));
                    }
                    codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\treturn 0;"));
                }
                else
                {
                    if (isIndexer)
                    {
                        // index Get Method
                        codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\tresult = obj[{paramBuilder}];"));
                    }
                    else if (OperationInfo.IsOperation(methodInfo.Name))
                    {
                        var format = OperationInfo.OperationInfos[methodInfo.Name].InvokeStringFormat;
                        for (var i = 0; i < parameterInfos.Length; i++)
                        {
                            format = format.Replace(string.Format("{{{0}}}", i), $"arg{i+1}");
                        }
                        format = format.Replace("{type}", type.FullName);
                        codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\tresult = {format};"));
                    }
                    else
                    {
                        codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\tresult = obj.{methodInfo.Name}({paramBuilder});"));
                    }

                    codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\tL.{GetPushString(methodInfo.ReturnType, gener)}(result);"));
                    codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\treturn 1;"));
                }



            };

            codes.Add(new CodeSnippetStatement($"\t\t\t\tvar obj = ({GetSafeClassFriendlyFullName(type, gener)}) L.ToUserData(1);"));
            for (int curArgIndex = 1; curArgIndex <= parameterInfos.Length; curArgIndex++)
            {
                var paramInfo = parameterInfos[curArgIndex - 1];

                if (!paramInfo.HasDefaultValue)
                {
                    codes.Add(new CodeSnippetStatement($"\t\t\t\tvar arg{curArgIndex} = L.{GetCheckString(paramInfo.ParameterType, gener)}({curArgIndex + 1});"));
                }
                else
                {
                    codes.Add(new CodeSnippetStatement($"\t\t\t\tvar arg{curArgIndex} = default({GetSafeClassFriendlyFullName(paramInfo.ParameterType, gener)});\t\t\t\t"));
                    codes.Add(new CodeSnippetStatement($"\t\t\t\tif({curArgIndex + 2} > top)\n\t\t\t\t{{"));

                    for (int argIndexInIf = notOptionalArgNum + 1; argIndexInIf <= curArgIndex; argIndexInIf++)
                    {
                        var paramInfoInIf = parameterInfos[argIndexInIf - 1];
                        codes.Add(new CodeSnippetStatement($"\t\t\t\t\targ{argIndexInIf} = L.{GetCheckString(paramInfoInIf.ParameterType, gener)}({argIndexInIf + 1});"));
                    }

                    addCallWithArgNum(curArgIndex, "\t");
                    codes.Add(new CodeSnippetStatement($"\t\t\t\t}}"));
                }

            }

            addCallWithArgNum(notOptionalArgNum, "");

            codes.Add(new CodeSnippetStatement($"\t\t\t}}"));
            count++;
        }

        private static void GenSingleStaticFunction(CodeGener gener, Type type, List<CodeStatement> codes, MethodInfo methodInfo, ref int count)
        {
            var parameterInfos = methodInfo.GetParameters();

            var notOptionalArgNum = parameterInfos.Count(arg => !arg.IsOptional);
            bool hasOptional = notOptionalArgNum != parameterInfos.Length;

            //检查类型的方法
            StringBuilder checkStringBuilder = new StringBuilder();

            if (hasOptional)
            {
                checkStringBuilder.Append($"L.CheckRange({notOptionalArgNum}, {parameterInfos.Length})");
            }
            else
            {
                checkStringBuilder.Append($"L.CheckNum({parameterInfos.Length})");
            }

            if (parameterInfos.Length > 0)
            {
                checkStringBuilder.Append($" && L.CheckType<");
                StringBuilder typeArgs = new StringBuilder();
                for (int i = 0; i < Math.Min(parameterInfos.Length, MaxCheckTypeArgNum); i++)
                {
                    if (i != 0)
                    {
                        if (parameterInfos[i].IsOptional)
                        {
                            break;
                        }
                        typeArgs.Append(", ");
                    }
                    typeArgs.Append(GetSafeClassFriendlyFullName(parameterInfos[i].ParameterType, gener));
                }
                checkStringBuilder.Append(typeArgs);
                checkStringBuilder.Append($">(1)");
            }

            if (count == 0)
            {
                codes.Add(new CodeSnippetStatement($"\t\t\tif({checkStringBuilder})\n\t\t\t{{"));
            }
            else
            {
                codes.Add(new CodeSnippetStatement($"\t\t\telse if({checkStringBuilder})\n\t\t\t{{"));
            }

            if (hasOptional)
            {
                codes.Add(new CodeSnippetStatement($"\t\t\t\tvar top = L.GetTop();"));
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                codes.Add(new CodeSnippetStatement($"\t\t\t\t{GetSafeClassFriendlyFullName(methodInfo.ReturnType, gener)} result;"));
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

                if (methodInfo.ReturnType == typeof(void))
                {
                    if (OperationInfo.IsOperation(methodInfo.Name))
                    {
                        var format = OperationInfo.OperationInfos[methodInfo.Name].InvokeStringFormat;
                        for (var i = 0; i < parameterInfos.Length; i++)
                        {
                            format = format.Replace(string.Format("{{{0}}}", i), $"arg{i+1}");
                        }
                        format = format.Replace("{type}", type.FullName);
                        codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\t{format};"));
                    }
                    else
                    {
                        codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\t{GetSafeClassFriendlyFullName(type, gener)}.{methodInfo.Name}({paramBuilder});"));
                    }
                 
                    codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\treturn 0;"));
                }
                else
                {
                    if (OperationInfo.IsOperation(methodInfo.Name))
                    {
                        var format = OperationInfo.OperationInfos[methodInfo.Name].InvokeStringFormat;
                        for (var i = 0; i < parameterInfos.Length; i++)
                        {
                            format = format.Replace(string.Format("{{{0}}}", i), $"arg{i+1}");
                        }
                        format = format.Replace("{type}", type.FullName);
                        codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\tresult = {format};"));
                    }
                    else
                    {
                        codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\tresult = {GetSafeClassFriendlyFullName(type, gener)}.{methodInfo.Name}({paramBuilder});"));
                    }
                    codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\tL.{GetPushString(methodInfo.ReturnType, gener)}(result);"));
                    codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\treturn 1;"));
                }

            };

            for (int curArgIndex = 1; curArgIndex <= parameterInfos.Length; curArgIndex++)
            {
                var paramInfo = parameterInfos[curArgIndex - 1];

                if (!paramInfo.HasDefaultValue)
                {
                    codes.Add(new CodeSnippetStatement($"\t\t\t\tvar arg{curArgIndex} = L.{GetCheckString(paramInfo.ParameterType, gener)}({curArgIndex});"));
                }
                else
                {
                    codes.Add(new CodeSnippetStatement($"\t\t\t\tvar arg{curArgIndex} = default({GetSafeClassFriendlyFullName(paramInfo.ParameterType, gener)});\t\t\t\t"));
                    codes.Add(new CodeSnippetStatement($"\t\t\t\tif({curArgIndex} + 1 > top)\n\t\t\t\t{{"));
                    for (int curParamInIf = notOptionalArgNum + 1; curParamInIf <= curArgIndex; curParamInIf++)
                    {
                        var paramInfoInIf = parameterInfos[curParamInIf - 1];
                        codes.Add(new CodeSnippetStatement($"\t\t\t\t\targ{curParamInIf} = L.{GetCheckString(paramInfoInIf.ParameterType, gener)}({curParamInIf});"));
                    }

                    addCallWithArgNum(curArgIndex, "\t");
                    codes.Add(new CodeSnippetStatement($"\t\t\t\t}}"));
                }

            }

            addCallWithArgNum(notOptionalArgNum, "");

            codes.Add(new CodeSnippetStatement($"\t\t\t}}"));
            count++;
        }

        private static void GenSingleExtensionFunction(CodeGener gener, Type type, List<CodeStatement> codes,
            MethodInfo methodInfo, ref int count)
        {
            var parameterInfos = methodInfo.GetParameters();

            var notOptionalArgNum = parameterInfos.Count(arg => !arg.IsOptional);
            bool hasOptional = notOptionalArgNum != parameterInfos.Length;

            StringBuilder checkStringBuilder = new StringBuilder();
            if (hasOptional)
            {
                checkStringBuilder.Append($"L.CheckRange({notOptionalArgNum}, {parameterInfos.Length})");
            }
            else
            {
                checkStringBuilder.Append($"L.CheckNum({parameterInfos.Length})");
            }
            if (notOptionalArgNum > 0 && !parameterInfos[0].IsOptional)
            {
                checkStringBuilder.Append($" && L.CheckType<");
                StringBuilder typeArgs = new StringBuilder();
                typeArgs.Append(GetSafeClassFriendlyFullName(type, gener));
                for (int i = 1; i < Math.Min(parameterInfos.Length, MaxCheckTypeArgNum); i++)
                {
                    if (parameterInfos[i].IsOptional)
                    {
                        break;
                    }
                    typeArgs.Append(", ");
                    typeArgs.Append(GetSafeClassFriendlyFullName(parameterInfos[i].ParameterType, gener));
                }
                checkStringBuilder.Append(typeArgs);
                checkStringBuilder.Append($">(1)");
            }

            if (count == 0)
            {
                codes.Add(new CodeSnippetStatement($"\t\t\tif({checkStringBuilder})\n\t\t\t{{"));
            }
            else
            {
                codes.Add(new CodeSnippetStatement($"\t\t\telse if({checkStringBuilder})\n\t\t\t{{"));
            }

            if (hasOptional)
            {
                codes.Add(new CodeSnippetStatement($"\t\t\t\tvar top = L.GetTop();"));
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                codes.Add(new CodeSnippetStatement($"\t\t\t\t{GetSafeClassFriendlyFullName(methodInfo.ReturnType, gener)} result;"));
            }

            Action<int, string> addCallWithArgNum = (int argNum, string offset) =>
            {
                var paramBuilder = new StringBuilder();
                paramBuilder.Append("obj");
                for (int i = 1; i <= argNum; i++)
                {
                    paramBuilder.Append(", ");
                    paramBuilder.Append($"arg{i}");
                }

                if (methodInfo.ReturnType == typeof(void))
                {
                    codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\t{methodInfo.DeclaringType.GetTypeNameFromCodeDom()}.{methodInfo.Name}({paramBuilder});"));
                    codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\treturn 0;"));
                }
                else
                {
                    codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\tresult = {methodInfo.DeclaringType.GetTypeNameFromCodeDom()}.{methodInfo.Name}({paramBuilder});"));


                    codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\tL.{GetPushString(methodInfo.ReturnType, gener)}(result);"));
                    codes.Add(new CodeSnippetStatement($"{offset}\t\t\t\treturn 1;"));
                }



            };

            codes.Add(new CodeSnippetStatement($"\t\t\t\tvar obj = ({GetSafeClassFriendlyFullName(type, gener)}) L.ToUserData(1);"));
            for (int curArgIndex = 1; curArgIndex < parameterInfos.Length; curArgIndex++)
            {
                var paramInfo = parameterInfos[curArgIndex];

                if (!paramInfo.HasDefaultValue)
                {
                    codes.Add(new CodeSnippetStatement($"\t\t\t\tvar arg{curArgIndex} = L.{GetCheckString(paramInfo.ParameterType, gener)}({curArgIndex + 1});"));
                }
                else
                {
                    codes.Add(new CodeSnippetStatement($"\t\t\t\tvar arg{curArgIndex} = default({GetSafeClassFriendlyFullName(paramInfo.ParameterType, gener)});\t\t\t\t"));
                    codes.Add(new CodeSnippetStatement($"\t\t\t\tif({curArgIndex + 1} > top)\n\t\t\t\t{{"));

                    for (int argIndexInIf = notOptionalArgNum; argIndexInIf <= curArgIndex; argIndexInIf++)
                    {
                        var paramInfoInIf = parameterInfos[argIndexInIf];
                        codes.Add(new CodeSnippetStatement($"\t\t\t\t\targ{argIndexInIf} = L.{GetCheckString(paramInfoInIf.ParameterType, gener)}({argIndexInIf + 1});"));
                    }

                    addCallWithArgNum(curArgIndex, "\t");
                    codes.Add(new CodeSnippetStatement($"\t\t\t\t}}"));
                }

            }

            addCallWithArgNum(notOptionalArgNum-1, "");

            codes.Add(new CodeSnippetStatement($"\t\t\t}}"));
            count++;
        }

    }
}