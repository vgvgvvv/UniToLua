using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UniToLua.Common;
using UniLua;

namespace UniToLuaGener
{
    public static partial class EmmyLuaExport
    {

        public static string ExportTypeDefinesLuaName = "ExportTypeDefines";
        public static string ExportTypeGlobalVariableLuaName = "ExportTypeGlobalVariable";
        public static string GlobalValueBindToLuaState = "GlobalValueBindToLuaState";

        private static HashSet<Type> luaNumberTypeSet = new HashSet<Type>
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double)
        };

        private static HashSet<string> luaKeywordSet = new HashSet<string>
        {
            "and",
            "break",
            "do",
            "else",
            "elseif",
            "end",
            "false",
            "for",
            "function",
            "if",
            "in",
            "local",
            "nil",
            "not",
            "or",
            "repeat",
            "return",
            "then",
            "true",
            "until",
            "while"
        };

        public static void GenAllAndZip(List<Type> targetTypeList, string outputPath)
        {
            var sourcePath = Path.Combine(outputPath, "Source");
            GenLuaTypeDefine(targetTypeList, sourcePath);
            GenLuaClassDefine(targetTypeList, sourcePath);
            var CodeHintZipLibFilePath = Path.Combine(outputPath, "LuaHint.zip"); 
            if (File.Exists(CodeHintZipLibFilePath))
            {
                File.Delete(CodeHintZipLibFilePath);
            }
            ZipFile.CreateFromDirectory(sourcePath, CodeHintZipLibFilePath);
        }

        /// <summary>
        /// 导出EmmyLua类型数据
        /// </summary>
        /// <param name="targetTypeList"></param>
        /// <param name="outputPath"></param>
        public static void GenLuaClassDefine(List<Type> targetTypeList, string outputPath)
        {

            Hashtable GlobalTable = CreateGlobalTableFromType(targetTypeList);
            List<string> codes = new List<string>();
            GenLuaClassWithTable(codes, GlobalTable, "");

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            File.WriteAllLines(Path.Combine(outputPath, $"{ExportTypeGlobalVariableLuaName}.lua"), codes);
        }

        public static void GenLuaTypeDefine(List<Type> targetTypeList, string outputPath)
        {
            List<string> codes = new List<string>();
            foreach (var type in targetTypeList)
            {
                if (type.IsEnum)
                {
                    GenLuaEnumLibTypeDefine(type, codes);
                }
                else
                {
                    var extensionMethods = ExportToLua.FindExtensionMethods(type, targetTypeList, out var extensionTypes);
                    GenLuaClassTypeDefine(type, extensionMethods, codes);
                }
                codes.Add("\n-----------------------------\n");
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            File.WriteAllLines(Path.Combine(outputPath, $"{ExportTypeDefinesLuaName}.lua"), codes);
        }

        public static void GenLuaBindValues(Dictionary<string, Type> bindValues, string outputPath)
        {
            Hashtable GlobalTable= CreateTableFromBindTypes(bindValues);
            List<string> codes = new List<string>();
            GenBindTypesWithTable(codes, GlobalTable, "");
            
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            File.WriteAllLines(Path.Combine(outputPath, $"{GlobalValueBindToLuaState}.lua"), codes);
        }

        private static Hashtable CreateTableFromBindTypes(Dictionary<string, Type> bindValues)
        {
            Hashtable GlobalTable = new Hashtable();
            foreach (var kvp in bindValues)
            {
                Hashtable currentNSTable = GlobalTable;
                var temp = kvp.Key.Split('.');
                var nsList = temp.SubArray(0, temp.Length-1);
                foreach (var ns in nsList)
                {
                    if (!currentNSTable.Contains(ns))
                    {
                        currentNSTable.Add(ns, new Hashtable());
                    }
                    currentNSTable = currentNSTable[ns] as Hashtable;
                    if (currentNSTable == null)
                    {
                        Log.Error($"space {kvp.Key} type error");
                        break;
                    }
                }
                currentNSTable.Add(temp[temp.Length - 1], new Tuple<string, Type>(kvp.Key, kvp.Value));
            }
            return GlobalTable;
        }

        private static void GenBindTypesWithTable(List<string> codes, Hashtable currentTable, string offset)
        {
            int count = 0;
            int num = currentTable.Count;
            foreach (var key in currentTable.Keys)
            {
                string dot = offset.IsNullOrEmptyR() ? "" : (count < num - 1 ? "," : "");
                if (currentTable[key] is Hashtable table)
                {
                    codes.Add($"{offset}{key} = {{");
                    GenBindTypesWithTable(codes, table, offset + "\t");
                    codes.Add($"{offset}}}{dot}");
                }
                else if (currentTable[key] is Tuple<string, Type> kvp)
                {
                    if (!kvp.Item2.IsGenericType)
                    {
                        codes.Add($"{offset}---@type {kvp.Item2}");
                        codes.Add($"{offset}{key} = {kvp.Item1}{dot}");

                    }
                }

                count++;
            }
        }

        #region Private

        private static Hashtable CreateGlobalTableFromType(List<Type> targetTypeList)
        {
            Hashtable GlobalTable = new Hashtable();
            foreach (var type in targetTypeList)
            {
                if (type == null)
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
                currentNSTable.Add(ExportToLua.GetSafeTypeFullName(type, null), type);
            }
            return GlobalTable;
        }

        private static void GenLuaClassWithTable(List<string> codes, Hashtable currentTable, string offset)
        {
            int count = 0;
            int num = currentTable.Count;
            foreach (var key in currentTable.Keys)
            {
                string dot = offset.IsNullOrEmptyR() ? "" : (count < num - 1 ? "," : "");
                if (currentTable[key] is Hashtable table)
                {
                    codes.Add($"{offset}{key} = {{");
                    GenLuaClassWithTable(codes, table, offset + "\t");
                    codes.Add($"{offset}}}{dot}");
                }
                else if (currentTable[key] is Type type)
                {
                    if (!type.IsGenericType)
                    {
                        codes.Add($"{offset}---@type {type.GetLuaCommentTypeName()}");
                        codes.Add($"{offset}{type.GetFriendlyName()} = {GetClassName(type, dot)}");

                    }
                }

                count++;
            }
        }

        private static StringBuilder GetClassName(Type exportType, string dot)
        {
            StringBuilder sbForGetClassType = new StringBuilder();
            var namespaceArr = exportType.Namespace?.Split('.');
            if (namespaceArr == null)
            {
                sbForGetClassType.Append("_G[\"");
                sbForGetClassType.Append(exportType.GetFriendlyName());
                sbForGetClassType.Append($"\"]{dot}");
            }
            else
            {
                sbForGetClassType.Append(namespaceArr[0]);
                for (int namespaceIndex = 1; namespaceIndex < namespaceArr.Length; namespaceIndex++)
                {
                    sbForGetClassType.Append("[\"");
                    sbForGetClassType.Append(namespaceArr[namespaceIndex]);
                    sbForGetClassType.Append("\"]");
                }
                sbForGetClassType.Append("[\"");
                sbForGetClassType.Append(exportType.GetFriendlyName());
                sbForGetClassType.Append($"\"]{dot}");
            }

            return sbForGetClassType;
        }

        private static void GenLuaClassTypeDefine(Type type, List<MethodInfo> extensionMethods, List<string> codes)
        {
            if (type.GetFriendlyName().Contains("<"))
            {
                return;
            }
            string baseType = type.BaseType != null ? $"{type.BaseType.GetLuaCommentTypeName()}" : "";
            codes.Add($"---@class {type.GetLuaCommentTypeName()} : {baseType}");

            GenClassFieldString(codes, type);

            GenClassPropertyString(codes, type);

            codes.Add($"{type.GetLuaCommentTypeName()} = {{}}");

            GenClassConstructString(codes, type);

            GenClassEventString(codes, type);
            
            GenClassMethodString(codes, extensionMethods, type);

        }

        private static void GenClassFieldString(List<string> codes, Type type)
        {
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                if (!field.IsPublic)
                    continue;
                codes.Add($"---@field {field.Name} {field.FieldType.GetLuaCommentTypeName()}");
                
                if (field.FieldType.IsSubclassOf(typeof(Delegate)))
                {
                    GenClassDelegateField(codes, type, field);
                }
            }
        }
        
        private static void GenClassDelegateField(List<string> codes, Type type, FieldInfo fieldInfo)
        {
            var invoke = fieldInfo.FieldType.GetMethod("Invoke");
            var parameters = invoke.GetParameters();
            var returnParam = invoke.ReturnParameter;
            var dot = fieldInfo.IsStatic ? "." : ":";

            StringBuilder functionBuilder = new StringBuilder();
            functionBuilder.Append($"function {type.GetLuaCommentTypeName()}{dot}{fieldInfo.Name}_Invoke(");
            for (int i = 0; i < parameters.Length; i ++)
            {
                var parameterInfo = parameters[i];
                codes.Add($"---@param {parameterInfo.Name.SafeName()} {parameterInfo.ParameterType.GetLuaCommentTypeName()}");
                if (i != 0)
                {
                    functionBuilder.Append(", ");
                }

                functionBuilder.Append(parameterInfo.Name.SafeName());
            }
            functionBuilder.Append(") end");

            if (returnParam.ParameterType != typeof(void))
            {
                codes.Add($"---@return {returnParam.ParameterType}");
            }
           
            codes.Add(functionBuilder.ToString());    
            
            codes.Add($"---@param handler {fieldInfo.FieldType.GetLuaCommentTypeName()}");
            codes.Add($"function {type.GetLuaCommentTypeName()}{dot}{fieldInfo.Name}_Add(handler) end");
            
            codes.Add($"---@param handler {fieldInfo.FieldType.GetLuaCommentTypeName()}");
            codes.Add($"function {type.GetLuaCommentTypeName()}{dot}{fieldInfo.Name}_Remove(handler) end");
        }
        

        private static void GenClassPropertyString(List<string> codes, Type type)
        {
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                if (property.GetMethod == null || !property.GetMethod.IsPublic)
                {
                    if (property.SetMethod == null || !property.SetMethod.IsPublic)
                        continue;
                }
                
                codes.Add($"---@field {property.Name} {property.PropertyType.GetLuaCommentTypeName()}");

                if (property.PropertyType.IsSubclassOf(typeof(Delegate)))
                {
                    GenClassDelegateProperty(codes, type, property);
                }
            }
        }

        private static void GenClassDelegateProperty(List<string> codes, Type type, PropertyInfo property)
        {
            var invoke = property.PropertyType.GetMethod("Invoke");
            var parameters = invoke.GetParameters();
            var returnParam = invoke.ReturnParameter;

            var dot = property.GetMethod.IsStatic ? "." : ":";
            
            StringBuilder functionBuilder = new StringBuilder();
            functionBuilder.Append($"function {type.GetLuaCommentTypeName()}{dot}{property.Name}_Invoke(");
            for (int i = 0; i < parameters.Length; i ++)
            {
                var parameterInfo = parameters[i];
                codes.Add($"---@param {parameterInfo.Name.SafeName()} {parameterInfo.ParameterType.GetLuaCommentTypeName()}");
                if (i != 0)
                {
                    functionBuilder.Append(", ");
                }

                functionBuilder.Append(parameterInfo.Name.SafeName());
            }
            functionBuilder.Append(") end");

            if (returnParam.ParameterType != typeof(void))
            {
                codes.Add($"---@return {returnParam.ParameterType}");
            }
           
            codes.Add(functionBuilder.ToString());    
            
            codes.Add($"---@param handler {property.PropertyType.GetLuaCommentTypeName()}");
            codes.Add($"function {type.GetLuaCommentTypeName()}{dot}{property.Name}_Add(handler) end");
            
            codes.Add($"---@param handler {property.PropertyType.GetLuaCommentTypeName()}");
            codes.Add($"function {type.GetLuaCommentTypeName()}{dot}{property.Name}_Remove(handler) end");
        }
        
        private static void GenClassEventString(List<string> codes, Type type)
        {
            var events = type.GetEvents();
            foreach (var eventInfo in events)
            {
                if (eventInfo.AddMethod != null && eventInfo.AddMethod.IsPublic)
                {
                    codes.Add($"---@param handler {eventInfo.EventHandlerType.GetLuaCommentTypeName()}");
                    if (!eventInfo.AddMethod.IsStatic)
                    {
                        codes.Add($"---@return {type.GetLuaCommentTypeName()}");
                    }
                    var dot = eventInfo.AddMethod.IsStatic ? "." : ":";
                    codes.Add($"function {type.GetLuaCommentTypeName()}{dot}{eventInfo.Name}_Add(handler) end");
                }
               
                if (eventInfo.RemoveMethod != null && eventInfo.RemoveMethod.IsPublic)
                {
                    codes.Add($"---@param handler {eventInfo.EventHandlerType.GetLuaCommentTypeName()}");
                    if (!eventInfo.RemoveMethod.IsStatic)
                    {
                        codes.Add($"---@return {type.GetLuaCommentTypeName()}");
                    }
                    var dot = eventInfo.RemoveMethod.IsStatic ? "." : ":";
                    codes.Add($"function {type.GetLuaCommentTypeName()}{dot}{eventInfo.Name}_Remove(handler) end");
                }
            }
        }
        
        private static void GenClassConstructString(List<string> codes, Type type)
        {
            var constructors = type.GetConstructors().Where(con => con.IsPublic).ToList();

            for (int i = 0; i < constructors.Count; i++)
            {
                var cons = constructors[i];
                if (i == constructors.Count - 1)
                {
                    StringBuilder paramBuilder = new StringBuilder();
                    var parameters = cons.GetParameters();
                    for (var i1 = 0; i1 < parameters.Length; i1++)
                    {
                        var par = parameters[i1];
                        codes.Add($"---@param {par.Name.SafeName()} {par.ParameterType.GetLuaCommentTypeName()}");
                        if (i1 != 0)
                        {
                            paramBuilder.Append(", ");
                        }
                        paramBuilder.Append(par.Name.SafeName());
                    }
                    codes.Add($"---@return {type.GetLuaCommentTypeName()}");
                    codes.Add($"function {type.GetLuaCommentTypeName()}.New({paramBuilder}) end");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("---@overload fun(");
                    var parameters = cons.GetParameters();
                    for (var i1 = 0; i1 < parameters.Length; i1++)
                    {
                        if (i1 != 0)
                        {
                            sb.Append(", ");
                        }
                        var par = parameters[i1];
                        sb.Append($"{par.Name.SafeName()} : {par.ParameterType.GetLuaCommentTypeName()}");
                    }

                    sb.Append(")");
                    sb.Append($" : {type.GetLuaCommentTypeName()}");
                    codes.Add(sb.ToString());
                }
            }
        }

        private static void GenClassMethodString(List<string> codes, List<MethodInfo> extensionMethods, Type type)
        {
            var properties = type.GetProperties();
            var events = type.GetEvents();
            var methods = type.GetMethods().ToList();
            methods.AddRange(extensionMethods);
            var methodGroups = methods
                .Where(method =>
                {
                    if (properties.Count(prop => prop.GetMethod == method || prop.SetMethod == method) != 0)
                    {
                        return false;
                    }

                    if (events.Count(eve => eve.AddMethod == method || eve.RemoveMethod == method) != 0)
                    {
                        return false;
                    }
                    return method.IsPublic && !method.ContainsGenericParameters && !IsObsolete(method);
                })
                .GroupBy(method => method.Name.SafeName()).ToList();

            for (var i = 0; i < methodGroups.Count; i++)
            {
                var group = methodGroups[i].ToList();

                for (var i1 = 0; i1 < group.Count; i1++)
                {
                    var method = group[i1];
                    
                    bool isExtension = method.IsStatic && method.GetParameters().Length > 0 &&
                                       method.GetParameters()[0].GetType() == type && 
                                       method.DeclaringType != type && !type.IsSubclassOf(method.DeclaringType);
                    if (i1 == group.Count - 1)
                    {
                        StringBuilder paramBuilder = new StringBuilder();
                        var parameters = method.GetParameters();
                        if (isExtension)
                        {
                            for (var i2 = 1; i2 < parameters.Length; i2++)
                            {
                                var par = parameters[i2];
                                codes.Add($"---@param {par.Name.SafeName()} {par.ParameterType.GetLuaCommentTypeName()}");
                                if (i2 != 1)
                                {
                                    paramBuilder.Append(", ");
                                }
                                paramBuilder.Append(par.Name.SafeName());
                            }

                            if (method.ReturnType != typeof(void))
                            {
                                codes.Add($"---@return {method.ReturnType.GetLuaCommentTypeName()}");
                            }

                            codes.Add($"function {type.GetLuaCommentTypeName()}:{method.Name.SafeName()}({paramBuilder}) end");
                        }
                        else
                        {
                            for (var i2 = 0; i2 < parameters.Length; i2++)
                            {
                                var par = parameters[i2];
                                codes.Add($"---@param {par.Name.SafeName()} {par.ParameterType.GetLuaCommentTypeName()}");
                                if (i2 != 0)
                                {
                                    paramBuilder.Append(", ");
                                }
                                paramBuilder.Append(par.Name.SafeName());
                            }

                            if (method.ReturnType != typeof(void))
                            {
                                codes.Add($"---@return {method.ReturnType.GetLuaCommentTypeName()}");
                            }

                            var dot = method.IsStatic ? "." : ":";
                            codes.Add($"function {type.GetLuaCommentTypeName()}{dot}{method.Name.SafeName()}({paramBuilder}) end");
                        }
                            
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("---@overload fun(");
                        if (isExtension)
                        {
                            var parameters = method.GetParameters();
                            for (var i2 = 1; i2 < parameters.Length; i2++)
                            {
                                if (i2 != 1)
                                {
                                    sb.Append(", ");
                                }
                                var par = parameters[i2];
                                sb.Append($"{par.Name.SafeName()} : {par.ParameterType.GetLuaCommentTypeName()}");
                            }
                        }
                        else
                        {
                            var parameters = method.GetParameters();
                            for (var i2 = 0; i2 < parameters.Length; i2++)
                            {
                                if (i2 != 0)
                                {
                                    sb.Append(", ");
                                }
                                var par = parameters[i2];
                                sb.Append($"{par.Name.SafeName()} : {par.ParameterType.GetLuaCommentTypeName()}");
                            }
                            
                        }
                        sb.Append(")");
                        if (method.ReturnType != typeof(void))
                        {
                            sb.Append($" : {method.ReturnType.GetLuaCommentTypeName()}");
                        }

                        codes.Add(sb.ToString());
                    }
                }

            }
        }

        private static void GenLuaEnumLibTypeDefine(Type type, List<string> codes)
        {
            codes.Add($"---@class {type.GetLuaCommentTypeName()}");

            var enumNames = Enum.GetNames(type);
            for (var i = 0; i < enumNames.Length; i++)
            {
                var enumName = enumNames[i];
                codes.Add($"---@field {enumName} {type.GetLuaCommentTypeName()}");
            }

            codes.Add($"{type.GetLuaCommentTypeName()} = {{}}");
        }


        #region Util

        private static string GetLuaCommentTypeName(this Type type)
        {
            if (type == typeof(string))
            {
                return "string";
            }

            if (type == typeof(bool))
            {
                return "bool";
            }

            if (type.IsNumber())
            {
                return "number";
            }

            if (type.IsEnum)
            {
                return type.FullName.Replace("+", ".");
            }

            if (type.IsArray)
            {
                return $"{type.GetElementType().GetLuaCommentTypeName()}[]";
            }

            return type.GetTypeNameFromCodeDom()
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace(',', '_')
                .Replace(" ", "");
        }

        private static string SafeName(this string name)
        {
            if (luaKeywordSet.Contains(name))
            {
                return $"{name}_";
            }

            return name;
        }

        private static bool IsNumber(this Type type)
        {
            return luaNumberTypeSet.Contains(type);
        }

        private static bool IsObsolete(MemberInfo mb)
        {
            object[] attrs = mb.GetCustomAttributes(true);

            for (int j = 0; j < attrs.Length; j++)
            {
                Type t = attrs[j].GetType();

                if (t == typeof(System.ObsoleteAttribute) || t == typeof(NoToLuaAttribute))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion



        #endregion

    }
}
