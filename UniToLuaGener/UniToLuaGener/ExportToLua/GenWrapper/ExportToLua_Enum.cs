using System;
using System.CodeDom;
using System.Collections.Generic;
using UniToLua.Common;
using UniLua;

namespace UniToLuaGener
{
    public partial class ExportToLua
    {

        private static void GenEnum(Type enumType, string outputPath)
        {
            if (enumType == null)
                return;
            var className = GetClassFileName(enumType);
            var enumNames = enumType.GetEnumNames();
            
            if (string.IsNullOrEmpty(className))
            {
                Log.Warning($"{enumType.FullName} is not a valid enum to export lua wrapper");
                return;
            }

            CodeGener gener = new CodeGener("UniToLua", className);

            GetAllNeedNamespace(enumType).ForEach(ns => { gener.AddImport(ns); });

            List<CodeStatement> registerMethodStatement = new List<CodeStatement>();
            registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.BeginEnum(typeof({enumType.GetTypeNameFromCodeDom()}));"));
            foreach (var enumName in enumNames)
            {
                registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegVar(\"{enumName}\", get_{enumName}, null);"));
            }
            registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.EndEnum();"));

            gener.AddMemberMethod(typeof(void), "Register", new Dictionary<string, Type>() { { "L", typeof(ILuaState) } },
                MemberAttributes.Public | MemberAttributes.Static, registerMethodStatement.ToArray());

            foreach (var enumName in enumNames)
            {
                GenRegEnum(gener, enumType, enumName);
            }

            gener.GenCSharp(outputPath);
        }

        private static void GenRegEnum(CodeGener gener, Type enumType, string enumName)
        {
            gener.AddMemberMethod(typeof(int), $"get_{enumName}", new Dictionary<string, Type>() { { "L", typeof(ILuaState) } },
                MemberAttributes.Private | MemberAttributes.Static, new CodeSnippetStatement[]
                {
                    new CodeSnippetStatement($"\t\t\tL.PushLightUserData({enumType.GetTypeNameFromCodeDom()}.{enumName});"),
                    new CodeSnippetStatement("\t\t\treturn 1;"),
                });
        }

    }
}