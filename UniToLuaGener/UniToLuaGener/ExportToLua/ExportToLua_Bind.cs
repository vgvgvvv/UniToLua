using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using Common;
using UniLua;

namespace UniToLuaGener
{
    public partial class ExportToLua
    {

        public static void GenBinder(List<Type> targetTypeList, string outputPath)
        {
            CodeGener gener = new CodeGener("UniToLua", "LuaBinder");
            Hashtable GlobalTable = CreateGlobalTable(targetTypeList);

            GetAllNeedNamespace(targetTypeList.ToArray()).ForEach(ns => { gener.AddImport(ns); });
            

            List<CodeStatement> bindStatements = new List<CodeStatement>();

            bindStatements.Add(new CodeSnippetStatement("\t\t\tL.BeginModule(null);"));
            GenBindWithTable(bindStatements, GlobalTable);
            bindStatements.Add(new CodeSnippetStatement("\t\t\tL.EndModule();"));

            gener.AddMemberMethod(typeof(void), "Bind",
                new Dictionary<string, Type>() {{"L", typeof(LuaState)}},
                MemberAttributes.Public | MemberAttributes.Static, bindStatements.ToArray());

            gener.GenCSharp(outputPath);
        }

        private static void GenBindWithTable(List<CodeStatement> bindStatements, Hashtable currentTable)
        {
            foreach (var key in currentTable.Keys)
            {
                if (currentTable[key] is Hashtable)
                {
                    bindStatements.Add(new CodeSnippetStatement($"\t\t\tL.BeginModule(\"{key}\");"));
                    GenBindWithTable(bindStatements, (Hashtable) currentTable[key]);
                    bindStatements.Add(new CodeSnippetStatement($"\t\t\tL.EndModule();"));
                }
                else if (currentTable[key] is Type)
                {
                    var className = GetClassFileName((Type) currentTable[key]);
                    if (!string.IsNullOrEmpty(className))
                    {
                        bindStatements.Add(new CodeSnippetStatement($"\t\t\t{className}.Register(L);"));
                    }
                }
            }
        }

        internal static Hashtable CreateGlobalTable(List<Type> targetTypeList)
        {
            Hashtable GlobalTable = new Hashtable();
            foreach (var type in targetTypeList)
            {
                if (type == null || type.IsInterface)
                    continue;
                Hashtable currentNSTable = GlobalTable;
                var nsList = type.Namespace.Split('.');
                foreach (var ns in nsList)
                {
                    if (!currentNSTable.Contains(ns))
                    {
                        currentNSTable.Add(ns, new Hashtable());
                    }
                    currentNSTable = currentNSTable[ns] as Hashtable;
                    if (currentNSTable == null)
                    {
                        Log.Error($"namespace {type.Namespace} type error");
                        break;
                    }
                }

                if (!currentNSTable.Contains(GetSafeTypeFullName(type, null)))
                {
                    currentNSTable.Add(GetSafeTypeFullName(type, null), type);
                }
                else
                {
                    Log.Error($"You have registered the type {type.FullName} dont register repeat");
                }
            }
            return GlobalTable;
        }

    }
}