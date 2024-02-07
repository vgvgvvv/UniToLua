using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using UniLua;

namespace UniToLuaGener
{
    public partial class ExportToLua
    {

        private static void GenConstructor(CodeGener gener, Type type)
        {
            //TODO
            var constructorInfos = type.GetConstructors().Where(cons => !IsObsolete(cons)).ToList();

            var temp = new List<CodeStatement>();

            int index = 0;
            foreach (var constructorInfo in constructorInfos)
            {
                var args = constructorInfo.GetParameters();

                var notOptionalArgNum = args.Count(arg => !arg.IsOptional);
                bool hasOptional = notOptionalArgNum != args.Length;
                //检查类型的方法
                StringBuilder checkStringBuilder = new StringBuilder();
                if(hasOptional)
                {
                    checkStringBuilder.Append($"L.CheckRange({notOptionalArgNum}, {args.Length})");
                }
                else
                {
                    checkStringBuilder.Append($"L.CheckNum({args.Length})");
                }
                if (notOptionalArgNum > 0 && !args[0].IsOptional)
                {
                    checkStringBuilder.Append($"&& L.CheckType<");
                    StringBuilder typeArgs = new StringBuilder();
                    for (int i = 0; i < Math.Min(args.Length, MaxCheckTypeArgNum); i++)
                    {
                        if (i != 0)
                        {
                            if (args[i].IsOptional)
                            {
                                break;
                            }
                            typeArgs.Append(", ");
                        }
                        typeArgs.Append(GetSafeClassFriendlyFullName(args[i].ParameterType, gener));
                    }
                    checkStringBuilder.Append(typeArgs);
                    checkStringBuilder.Append($">(1)");
                }

                if (index == 0)
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\tif({checkStringBuilder})\n\t\t\t{{"));
                }
                else
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\telse if({checkStringBuilder})\n\t\t\t{{"));
                }

                if (hasOptional)
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\t\tvar top = L.GetTop();"));
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

                    temp.Add(new CodeSnippetStatement(
                        $"{offset}\t\t\t\tL.{GetPushString(type, gener)}(new {GetSafeClassFriendlyFullName(type, gener)}({paramBuilder}));"));
                    temp.Add(new CodeSnippetStatement($"{offset}\t\t\t\treturn 1;"));
                };

                for (int curArgIndex = 1; curArgIndex <= args.Length; curArgIndex++)
                {
                    var paramInfo = args[curArgIndex - 1];
                    if (!paramInfo.HasDefaultValue)
                    {
                        temp.Add(new CodeSnippetStatement($"\t\t\t\tvar arg{curArgIndex} = L.{GetCheckString(paramInfo.ParameterType, gener)}({curArgIndex});"));
                    }
                    else
                    {
                        temp.Add(new CodeSnippetStatement($"\t\t\t\tvar arg{curArgIndex} = default({GetSafeClassFriendlyFullName(paramInfo.ParameterType, gener)});\t\t\t\t"));
                        temp.Add(new CodeSnippetStatement($"\t\t\t\tif({curArgIndex} > top)\n\t\t\t\t{{"));
                        temp.Add(new CodeSnippetStatement($"\t\t\t\t\targ{curArgIndex} = L.{GetCheckString(paramInfo.ParameterType, gener)}({curArgIndex});"));
                        addCallWithArgNum(curArgIndex, "\t");
                        temp.Add(new CodeSnippetStatement($"\t\t\t\t}}"));
                    }
                    
                }

                addCallWithArgNum(notOptionalArgNum, "");

                temp.Add(new CodeSnippetStatement($"\t\t\t}}"));

                index++;
            }

            if (constructorInfos.Count == 0 && type.IsValueType)
            {
                temp.Add(new CodeSnippetStatement($"\t\t\tif(L.CheckNum(0)) {{"));
                temp.Add(new CodeSnippetStatement(
                    $"\t\t\t\tL.{GetPushString(type, gener)}(default({GetSafeClassFriendlyFullName(type, gener)}));"));
                temp.Add(new CodeSnippetStatement("\t\t\t\treturn 1;"));
                temp.Add(new CodeSnippetStatement($"\t\t\t}}"));
            }
            
            temp.Add(new CodeSnippetStatement($"\t\t\tL.L_Error(\"call {type.Name} constructor args is error\");"));
            temp.Add(new CodeSnippetStatement("\t\t\treturn 1;"));

            gener.AddMemberMethod(typeof(int), $"_Create{GetSafeTypeFullName(type, gener)}",
                new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());
        }

    }
}