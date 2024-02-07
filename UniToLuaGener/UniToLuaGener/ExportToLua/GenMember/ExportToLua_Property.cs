using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Common;
using UniLua;

namespace UniToLuaGener
{
    public partial class ExportToLua
    {

        private static void GenRegStaticProperty(CodeGener gener, Type type, PropertyInfo propertyInfo)
        {
            var temp = new List<CodeStatement>();
            if (propertyInfo.GetMethod != null && propertyInfo.GetMethod.IsPublic)
            {
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tL.{GetPushString(propertyInfo.PropertyType, gener)}({GetSafeClassFriendlyFullName(type, gener)}.{propertyInfo.Name});"),
                    new CodeSnippetStatement($"\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"get_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static,
                    temp.ToArray());
            }

            if (propertyInfo.SetMethod != null && propertyInfo.SetMethod.IsPublic)
            {
                temp.Clear();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tvar value = L.{GetCheckString(propertyInfo.PropertyType, gener)}(1);"),
                    new CodeSnippetStatement($"\t\t\t{GetSafeClassFriendlyFullName(type, gener)}.{propertyInfo.Name} = value;"),
                    new CodeSnippetStatement($"\t\t\treturn 0;")
                });

                gener.AddMemberMethod(typeof(int), $"set_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static,
                    temp.ToArray());
            }

            GenRegStaticDelegateProperty(gener, type, propertyInfo);
        }

        private static void GenRegMemberProperty(CodeGener gener, Type type, PropertyInfo propertyInfo)
        {
            var temp = new List<CodeStatement>();
            if (propertyInfo.GetMethod != null && propertyInfo.GetMethod.IsPublic)
            {
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tvar obj = ({GetSafeClassFriendlyFullName(type, gener)}) L.ToUserData(1);"),
                    new CodeSnippetStatement($"\t\t\tL.{GetPushString(propertyInfo.PropertyType, gener)}(obj.{propertyInfo.Name});"),
                    new CodeSnippetStatement($"\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"get_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static,
                    temp.ToArray());
            }

            if (propertyInfo.SetMethod != null && propertyInfo.SetMethod.IsPublic)
            {
                temp.Clear();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tvar obj = ({GetSafeClassFriendlyFullName(type, gener)}) L.ToUserData(1);"),
                    new CodeSnippetStatement($"\t\t\tvar value = L.{GetCheckString(propertyInfo.PropertyType, gener)}(2);"),
                    new CodeSnippetStatement($"\t\t\tobj.{propertyInfo.Name} = value;"),
                    new CodeSnippetStatement($"\t\t\treturn 0;")
                });

                gener.AddMemberMethod(typeof(int), $"set_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static,
                    temp.ToArray());
            }

            GenRegMemberDelegateProperty(gener, type, propertyInfo);
        }

        private static void GenRegStaticDelegateProperty(CodeGener gener, Type type, PropertyInfo propertyInfo)
        {
            if (!propertyInfo.PropertyType.IsSubclassOf(typeof(Delegate)))
            {
                return;
            }
            if(true)
            {
                var delegateType = propertyInfo.PropertyType;
                var deleMethod = delegateType.GetMethod("Invoke");
                var parameterInfos = deleMethod.GetParameters();
                var returnParam = deleMethod.ReturnParameter;
                bool hasReturn = returnParam.ParameterType != typeof(void);
                
                StringBuilder checkBuilder = new StringBuilder();
                checkBuilder.Append($"\t\t\tif(L.CheckNum({parameterInfos.Length}) && ");
                checkBuilder.Append("L.CheckType<");
                for (var i = 0; i < parameterInfos.Length; i++)
                {
                    var param = parameterInfos[i];
                    if (i != 0)
                    {
                        checkBuilder.Append(", ");
                    }
                    checkBuilder.Append($"{GetSafeClassFriendlyFullName(param.ParameterType, gener)}");
                }
                checkBuilder.Append(">(1))");
                
                var temp = new List<string>();
                temp.Add(checkBuilder.ToString());
                temp.Add("\t\t\t{");
                var invokeBuilder = new StringBuilder();
                
                temp.Add($"\t\t\t\tvar obj = L.{GetCheckString(type, gener)}(1);");
                if(!hasReturn)
                {
                    invokeBuilder.Append($"\t\t\t\t{GetSafeClassFriendlyFullName(type, gener)}.{propertyInfo.Name}(");
                }
                else
                {
                    invokeBuilder.Append($"\t\t\t\tvar result = ({GetSafeClassFriendlyFullName(returnParam.ParameterType, gener)}){GetSafeClassFriendlyFullName(type, gener)}.{propertyInfo.Name}(");
                }

                for (var i = 0; i < parameterInfos.Length; i++)
                {
                    var param = parameterInfos[i];
                    temp.Add($"\t\t\t\tvar arg{i} = L.{GetCheckString(param.ParameterType, gener)}({i+1});");
                    if (i != 0)
                    {
                        invokeBuilder.Append(", ");
                    }
                    invokeBuilder.Append($"arg{i}");
                }
                invokeBuilder.Append(");");
                temp.Add(invokeBuilder.ToString());
                if (hasReturn)
                {
                    temp.Add($"\t\t\t\tL.{GetPushString(returnParam.ParameterType, gener)}(result);");
                    temp.Add("\t\t\t\treturn 1;");
                }
                else
                {
                    temp.Add("\t\t\t\treturn 0;");
                }
                temp.Add("\t\t\t}");
                temp.Add("\t\t\tL.L_Error(\"add method args is error\");");
                temp.Add("\t\t\treturn 1;");
                
                gener.AddMemberMethod(typeof(int), $"invoke_{propertyInfo.Name}",
                    new Dictionary<string, Type>()
                    {
                        { "L", typeof(ILuaState) }
                    }, MemberAttributes.Private | MemberAttributes.Static, 
                    temp.Select(s=>new CodeSnippetStatement(s)).Cast<CodeStatement>().ToArray());

                temp.Clear();
            }
            
            if (true)
            {
                var temp = new List<CodeStatement>();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tif(L.CheckNum(1) && L.CheckType<{GetSafeClassFriendlyFullName(propertyInfo.PropertyType, gener)}>(1))"),
                    new CodeSnippetStatement("\t\t\t{"),
                    new CodeSnippetStatement($"\t\t\t\tvar value = L.{GetCheckString(propertyInfo.PropertyType, gener)}(1);"),
                    new CodeSnippetStatement($"\t\t\t\t{type.FullName}.{propertyInfo.Name} += value;"),
                    new CodeSnippetStatement($"\t\t\t\treturn 0;"),
                    new CodeSnippetStatement("\t\t\t}"),
                    new CodeSnippetStatement("\t\t\tL.L_Error(\"add method args is error\");"),
                    new CodeSnippetStatement("\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"add_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());

                temp.Clear();
            }

            if (true)
            {
                var temp = new List<CodeStatement>();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tif(L.CheckNum(1) && L.CheckType<{GetSafeClassFriendlyFullName(propertyInfo.PropertyType, gener)}>(1))"),
                    new CodeSnippetStatement("\t\t\t{"),
                    new CodeSnippetStatement($"\t\t\t\tvar value = L.{GetCheckString(propertyInfo.PropertyType, gener)}(1);"),
                    new CodeSnippetStatement($"\t\t\t\t{type.FullName}.{propertyInfo.Name} -= value;"),
                    new CodeSnippetStatement($"\t\t\t\treturn 0;"),
                    new CodeSnippetStatement("\t\t\t}"),
                    new CodeSnippetStatement("\t\t\tL.L_Error(\"add method args is error\");"),
                    new CodeSnippetStatement("\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"remove_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());

                temp.Clear();
            }
            
        }
                
        private static void GenRegMemberDelegateProperty(CodeGener gener, Type type, PropertyInfo propertyInfo)
        {
            if (!propertyInfo.PropertyType.IsSubclassOf(typeof(Delegate)))
            {
                return;
            }
            
            if(true)
            {
                var delegateType = propertyInfo.PropertyType;
                var deleMethod = delegateType.GetMethod("Invoke");
                var parameterInfos = deleMethod.GetParameters();
                var returnParam = deleMethod.ReturnParameter;
                bool hasReturn = returnParam.ParameterType != typeof(void);
                
                StringBuilder checkBuilder = new StringBuilder();
                checkBuilder.Append($"\t\t\tif(L.CheckNum({parameterInfos.Length+1}) && ");
                checkBuilder.Append("L.CheckType<");
                checkBuilder.Append($"{GetSafeClassFriendlyFullName(type, gener)}");
                for (var i = 0; i < parameterInfos.Length; i++)
                {
                    var param = parameterInfos[i];
                    checkBuilder.Append(", ");
                    checkBuilder.Append($"{GetSafeClassFriendlyFullName(param.ParameterType, gener)}");
                }
                checkBuilder.Append(">(1))");
                
                var temp = new List<string>();
                temp.Add(checkBuilder.ToString());
                temp.Add("\t\t\t{");
                var invokeBuilder = new StringBuilder();
                
                temp.Add($"\t\t\t\tvar obj = L.{GetCheckString(type, gener)}(1);");
                if(!hasReturn)
                {
                    invokeBuilder.Append($"\t\t\t\tobj.{propertyInfo.Name}(");
                }
                else
                {
                    invokeBuilder.Append($"\t\t\t\tvar result = ({GetSafeClassFriendlyFullName(returnParam.ParameterType, gener)})obj.{propertyInfo.Name}(");
                }

                for (var i = 0; i < parameterInfos.Length; i++)
                {
                    var param = parameterInfos[i];
                    temp.Add($"\t\t\t\tvar arg{i} = L.{GetCheckString(param.ParameterType, gener)}({i+2});");
                    if (i != 0)
                    {
                        invokeBuilder.Append(", ");
                    }
                    invokeBuilder.Append($"arg{i}");
                }
                invokeBuilder.Append(");");
                temp.Add(invokeBuilder.ToString());
                if (hasReturn)
                {
                    temp.Add($"\t\t\t\tL.{GetPushString(returnParam.ParameterType, gener)}(result);");
                    temp.Add("\t\t\t\treturn 1;");
                }
                else
                {
                    temp.Add("\t\t\t\treturn 0;");
                }
                temp.Add("\t\t\t}");
                temp.Add("\t\t\tL.L_Error(\"add method args is error\");");
                temp.Add("\t\t\treturn 1;");
                
                gener.AddMemberMethod(typeof(int), $"invoke_{propertyInfo.Name}",
                    new Dictionary<string, Type>()
                    {
                        { "L", typeof(ILuaState) }
                    }, MemberAttributes.Private | MemberAttributes.Static, 
                    temp.Select(s=>new CodeSnippetStatement(s)).Cast<CodeStatement>().ToArray());

                temp.Clear();
            }
            
            if (true)
            {
                var temp = new List<CodeStatement>();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tif(L.CheckNum(2) && L.CheckType<{GetSafeClassFriendlyFullName(type, gener)}, {GetSafeClassFriendlyFullName(propertyInfo.PropertyType, gener)}>(1))"),
                    new CodeSnippetStatement("\t\t\t{"),
                    new CodeSnippetStatement($"\t\t\t\tvar obj = L.{GetCheckString(type, gener)}(1);"),
                    new CodeSnippetStatement($"\t\t\t\tvar value = L.{GetCheckString(propertyInfo.PropertyType, gener)}(2);"),
                    new CodeSnippetStatement($"\t\t\t\tobj.{propertyInfo.Name} += value;"),
                    new CodeSnippetStatement($"\t\t\t\treturn 0;"),
                    new CodeSnippetStatement("\t\t\t}"),
                    new CodeSnippetStatement("\t\t\tL.L_Error(\"add method args is error\");"),
                    new CodeSnippetStatement("\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"add_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());

                temp.Clear();
            }

            if (true)
            {
                var temp = new List<CodeStatement>();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tif(L.CheckNum(2) && L.CheckType<{GetSafeClassFriendlyFullName(type, gener)}, {GetSafeClassFriendlyFullName(propertyInfo.PropertyType, gener)}>(1))"),
                    new CodeSnippetStatement("\t\t\t{"),
                    new CodeSnippetStatement($"\t\t\t\tvar obj = L.{GetCheckString(type, gener)}(1);"),
                    new CodeSnippetStatement($"\t\t\t\tvar value = L.{GetCheckString(propertyInfo.PropertyType, gener)}(2);"),
                    new CodeSnippetStatement($"\t\t\t\tobj.{propertyInfo.Name} -= value;"),
                    new CodeSnippetStatement($"\t\t\t\treturn 0;"),
                    new CodeSnippetStatement("\t\t\t}"),
                    new CodeSnippetStatement("\t\t\tL.L_Error(\"add method args is error\");"),
                    new CodeSnippetStatement("\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"remove_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());

                temp.Clear();
            }
        }
        
    }
}