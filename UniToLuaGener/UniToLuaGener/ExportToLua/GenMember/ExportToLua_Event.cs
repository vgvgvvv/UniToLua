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
        private static void GenRegStaticEvent(CodeGener gener, Type type, EventInfo eventInfo)
        {
            
            if (eventInfo.AddMethod != null && eventInfo.AddMethod.IsPublic)
            {
                
                var temp = new List<CodeStatement>();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tif(L.CheckNum(1) && L.CheckType<{GetSafeClassFriendlyFullName(eventInfo.EventHandlerType, gener)}>(1))"),
                    new CodeSnippetStatement("\t\t\t{"),
                    new CodeSnippetStatement($"\t\t\t\tvar value = L.{GetCheckString(eventInfo.EventHandlerType, gener)}(1);"),
                    new CodeSnippetStatement($"\t\t\t\t{type.FullName}.{eventInfo.Name} += value;"),
                    new CodeSnippetStatement($"\t\t\t\treturn 0;"),
                    new CodeSnippetStatement("\t\t\t}"),
                    new CodeSnippetStatement("\t\t\tL.L_Error(\"add method args is error\");"),
                    new CodeSnippetStatement("\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"add_{eventInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());

                temp.Clear();
            }

            if (eventInfo.RemoveMethod != null && eventInfo.RemoveMethod.IsPublic)
            {
                var temp = new List<CodeStatement>();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tif(L.CheckNum(1) && L.CheckType<{GetSafeClassFriendlyFullName(eventInfo.EventHandlerType, gener)}>(1))"),
                    new CodeSnippetStatement("\t\t\t{"),
                    new CodeSnippetStatement($"\t\t\t\tvar value = L.{GetCheckString(eventInfo.EventHandlerType, gener)}(1);"),
                    new CodeSnippetStatement($"\t\t\t\t{type.FullName}.{eventInfo.Name} -= value;"),
                    new CodeSnippetStatement($"\t\t\t\treturn 0;"),
                    new CodeSnippetStatement("\t\t\t}"),
                    new CodeSnippetStatement("\t\t\tL.L_Error(\"add method args is error\");"),
                    new CodeSnippetStatement("\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"remove_{eventInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());

                temp.Clear();
            }
           

        }

        private static void GenRegMemberEvent(CodeGener gener, Type type, EventInfo eventInfo)
        {
            
            if (eventInfo.AddMethod != null && eventInfo.AddMethod.IsPublic)
            {
                var temp = new List<CodeStatement>();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tif(L.CheckNum(2) && L.CheckType<{GetSafeClassFriendlyFullName(type, gener)}, {GetSafeClassFriendlyFullName(eventInfo.EventHandlerType, gener)}>(1))"),
                    new CodeSnippetStatement("\t\t\t{"),
                    new CodeSnippetStatement($"\t\t\t\tvar obj = L.{GetCheckString(type, gener)}(1);"),
                    new CodeSnippetStatement($"\t\t\t\tvar value = L.{GetCheckString(eventInfo.EventHandlerType, gener)}(2);"),
                    new CodeSnippetStatement($"\t\t\t\tobj.{eventInfo.Name} += value;"),
                    new CodeSnippetStatement($"\t\t\t\tL.{GetPushString(type, gener)}(obj);"),
                    new CodeSnippetStatement($"\t\t\t\treturn 1;"),
                    new CodeSnippetStatement("\t\t\t}"),
                    new CodeSnippetStatement("\t\t\tL.L_Error(\"add method args is error\");"),
                    new CodeSnippetStatement("\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"add_{eventInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());

                temp.Clear();
            }

            if (eventInfo.RemoveMethod != null && eventInfo.RemoveMethod.IsPublic)
            {
                var temp = new List<CodeStatement>();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tif(L.CheckNum(2) && L.CheckType<{GetSafeClassFriendlyFullName(type, gener)}, {GetSafeClassFriendlyFullName(eventInfo.EventHandlerType, gener)}>(1))"),
                    new CodeSnippetStatement("\t\t\t{"),
                    new CodeSnippetStatement($"\t\t\t\tvar obj = L.{GetCheckString(type, gener)}(1);"),
                    new CodeSnippetStatement($"\t\t\t\tvar value = L.{GetCheckString(eventInfo.EventHandlerType, gener)}(2);"),
                    new CodeSnippetStatement($"\t\t\t\tobj.{eventInfo.Name} -= value;"),
                    new CodeSnippetStatement($"\t\t\t\tL.{GetPushString(type, gener)}(obj);"),
                    new CodeSnippetStatement($"\t\t\t\treturn 1;"),
                    new CodeSnippetStatement("\t\t\t}"),
                    new CodeSnippetStatement("\t\t\tL.L_Error(\"add method args is error\");"),
                    new CodeSnippetStatement("\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"remove_{eventInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());

                temp.Clear();
            }
        }
    }
}