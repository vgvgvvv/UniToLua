using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ResetCore.CodeDom;
using UniLua;

namespace UniToLuaGener
{
    public class ExportToLua
    {
        public string outputPath;
        public string dllPath;

        public void GenAll()
        {
            var target = Assembly.LoadFile(Path.GetFullPath(dllPath));
            List<Type> targetTypeList = GetTargetType(target);
            GenWrapper(targetTypeList);
            GenBinder(targetTypeList);
        }

        private List<Type> GetTargetType(Assembly target)
        {
            var types = target.GetTypes().Where((t) =>
            {
                var toluaAttr = t.GetCustomAttribute<ToLuaAttribute>();
                return toluaAttr != null;
            });
            return types.ToList();
        }

        #region GenBinder

        public void GenBinder(List<Type> targetTypeList)
        {
            CodeGener gener = new CodeGener("UniToLua", "LuaBinder");
            Hashtable GlobalTable = CreateGlobalTable(targetTypeList);

            List<CodeStatement> bindStatements = new List<CodeStatement>();

            bindStatements.Add(new CodeSnippetStatement("\t\t\tL.BeginModule(null);"));
            GenBindWithTable(bindStatements, GlobalTable);
            bindStatements.Add(new CodeSnippetStatement("\t\t\tL.EndModule();"));

            gener.AddMemberMethod(typeof(void), "Bind",
                new Dictionary<string, Type>() {{"L", typeof(LuaState)}},
                MemberAttributes.Public | MemberAttributes.Static, bindStatements.ToArray());

            gener.GenCSharp(outputPath);
        }

        private void GenBindWithTable(List<CodeStatement> bindStatements, Hashtable currentTable)
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
                    bindStatements.Add(new CodeSnippetStatement($"\t\t\t{GetClassFileName((Type)currentTable[key])}.Register(L);"));
                }
            }
        }

        private Hashtable CreateGlobalTable(List<Type> targetTypeList)
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
                        Logger.Error($"namespace {type.Namespace} type error");
                        break;
                    }
                }
                currentNSTable.Add(type.Name, type);
            }
            return GlobalTable;
        }

        #endregion


        #region GenWrapper

        public void GenWrapper(List<Type> targetTypeList)
        {
            foreach (var type in targetTypeList)
            {
                if (type.IsEnum)
                {
                    GenEnum(type);
                }
                else if (type.IsInterface)
                {
                    Logger.Error("Cannot Gen Interface Wrap");
                }
                else if (type.IsSealed && type.IsAbstract)
                {
                    GenStaticLib(type);
                }
                else
                {
                    GenClass(type);
                }
            }
        }

        #region Enum

        private void GenEnum(Type enumType)
        {
            if (enumType == null)
                return;
            var className = GetClassFileName(enumType);
            var enumNames = enumType.GetEnumNames();

            CodeGener gener = new CodeGener("UniToLua", className);

            List<CodeStatement> registerMethodStatement = new List<CodeStatement>();
            registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.BeginEnum(\"{enumType.Name}\");"));
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

        private void GenRegEnum(CodeGener gener, Type enumType, string enumName)
        {
            gener.AddMemberMethod(typeof(int), $"get_{enumName}", new Dictionary<string, Type>() { { "L", typeof(ILuaState) } },
                MemberAttributes.Private | MemberAttributes.Static, new CodeSnippetStatement[]
                {
                    new CodeSnippetStatement($"\t\t\tL.PushLightUserData({enumType.FullName}.{enumName});"),
                    new CodeSnippetStatement("\t\t\treturn 1;"),
                });
        }

        #endregion

        #region StaticLib

        private void GenStaticLib(Type libType)
        {
            if (libType == null)
                return;
            var className = GetClassFileName(libType);
            CodeGener gener = new CodeGener("UniToLua", className);

            var fields = libType.GetFields(BindingFlags.Public | BindingFlags.Static);
            var propertys = libType.GetProperties(BindingFlags.Public | BindingFlags.Static);
            var methodGroups = libType.GetMethods(BindingFlags.Public | BindingFlags.Static).Where((method) =>
            {
                if (propertys.Count(prop => prop.GetMethod == method || prop.SetMethod == method) != 0)
                {
                    return false;
                }
                if (IsObsolete(method))
                {
                    return false;
                }
                return true;
            }).GroupBy(mi=>mi.Name).ToArray();

            List<CodeStatement> registerMethodStatement = new List<CodeStatement>();
            registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.BeginStaticLib(\"{libType.Name}\");"));

            foreach (var fieldInfo in fields)
            {
                registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegVar(\"{fieldInfo.Name}\", get_{fieldInfo.Name}, set_{fieldInfo.Name});"));
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

            foreach (var methodGroup in methodGroups)
            {
                GenRegStaticFunction(gener, libType, methodGroup.ToArray());
            }

            gener.GenCSharp(outputPath);
        }

        #endregion

        #region Class

        private void GenClass(Type classType)
        {
            if (classType == null)
                return;
            var className = GetClassFileName(classType);
            CodeGener gener = new CodeGener("UniToLua", className);

            var baseClassName = classType.BaseType == typeof(System.Object) || classType.BaseType == null
                ? "null"
                : classType.BaseType.FullName;
            var fields = classType.GetFields();
            var propertys = classType.GetProperties();
            var methodGroups = classType.GetMethods().Where((method) =>
            {
                if (propertys.Count(prop => prop.GetMethod == method || prop.SetMethod == method) != 0)
                {
                    return false;
                }
                if (IsObsolete(method))
                {
                    return false;
                }
                return true;
            }).GroupBy(mi => mi.Name).ToArray();

            List<CodeStatement> registerMethodStatement = new List<CodeStatement>();

            registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.BeginClass(typeof({classType.FullName}), {baseClassName});"));

            registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"New\", _Create{classType.Name});"));

            foreach (var fieldInfo in fields)
            {
                registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegVar(\"{fieldInfo.Name}\", get_{fieldInfo.Name}, set_{fieldInfo.Name});"));
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

            foreach (var methodGroup in methodGroups)
            {
                registerMethodStatement.Add(new CodeSnippetStatement($"\t\t\tL.RegFunction(\"{methodGroup.Key}\", {methodGroup.Key});"));
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

            foreach (var methodGroup in methodGroups)
            {
                if (!methodGroup.Any())
                {
                    continue;
                }
                if (methodGroup.First().IsStatic)
                {
                    GenRegStaticFunction(gener, classType, methodGroup.ToArray());
                }
                else
                {
                    GenRegMemberFunction(gener, classType, methodGroup.ToArray());
                }

            }

            gener.GenCSharp(outputPath);

        }

        #endregion

        #region GenMember

        private void GenConstructor(CodeGener gener, Type type)
        {
            //TODO
            var constructorInfos = type.GetConstructors();

            var temp = new List<CodeStatement>();

            int count = 0;
            foreach (var constructorInfo in constructorInfos)
            {
                var args = constructorInfo.GetParameters();

                //检查类型的方法
                StringBuilder checkStringBuilder = new StringBuilder();
                checkStringBuilder.Append($"L.CheckNum({args.Length})");
                if (args.Length > 0)
                {
                    checkStringBuilder.Append($"&& L.CheckType<");
                    StringBuilder typeArgs = new StringBuilder();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i != 0)
                        {
                            typeArgs.Append(", ");
                        }
                        typeArgs.Append(args[i].ParameterType.FullName);
                    }
                    checkStringBuilder.Append(typeArgs);
                    checkStringBuilder.Append($">(0)");
                }

                if (count == 0)
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\tif({checkStringBuilder})\n\t\t\t{{"));
                }
                else
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\telse if({checkStringBuilder})\n\t\t\t{{"));
                }

                for (int i = 1; i <= args.Length; i++)
                {
                    var paramInfo = args[i - 1];
                    temp.Add(new CodeSnippetStatement($"\t\t\t\tvar arg{i} = L.{GetCheckString(paramInfo.ParameterType)}({i});"));
                }

                var paramBuilder = new StringBuilder();
                for (int i = 1; i <= args.Length; i++)
                {
                    if (i != 1)
                    {
                        paramBuilder.Append(", ");
                    }
                    paramBuilder.Append($"arg{i}");
                }

                temp.Add(new CodeSnippetStatement($"\t\t\t\tL.{GetPushString(type)}(new {type.FullName}({paramBuilder}));"));
                temp.Add(new CodeSnippetStatement("\t\t\t\treturn 1;"));

                temp.Add(new CodeSnippetStatement($"\t\t\t}}"));

                count++;
            }

            temp.Add(new CodeSnippetStatement("\t\t\tL.L_Error(\"call function args is error\");"));
            temp.Add(new CodeSnippetStatement("\t\t\treturn 1;"));

            gener.AddMemberMethod(typeof(int), $"_Create{type.Name}",
                new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());
        }

        private void GenRegStaticField(CodeGener gener, Type type, FieldInfo fieldInfo)
        {
            var temp = new List<CodeStatement>();
            temp.AddRange(new List<CodeStatement>()
            {
                new CodeSnippetStatement($"\t\t\tL.{GetPushString(fieldInfo.FieldType)}({type.FullName}.{fieldInfo.Name});"),
                new CodeSnippetStatement($"\t\t\treturn 1;")
            });

            gener.AddMemberMethod(typeof(int), $"get_{fieldInfo.Name}",
                new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());

            temp.Clear();
            temp.AddRange(new List<CodeStatement>()
            {
                new CodeSnippetStatement($"\t\t\tvar value = L.{GetCheckString(fieldInfo.FieldType)}(1);"),
                new CodeSnippetStatement($"\t\t\t{type.FullName}.{fieldInfo.Name} = value;"),
                new CodeSnippetStatement($"\t\t\treturn 0;")
            });

            gener.AddMemberMethod(typeof(int), $"set_{fieldInfo.Name}",
                new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());
        }

        private void GenRegStaticProperty(CodeGener gener, Type type, PropertyInfo propertyInfo)
        {
            var temp = new List<CodeStatement>();
            if (propertyInfo.GetMethod != null && propertyInfo.GetMethod.IsPublic)
            {
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tL.{GetPushString(propertyInfo.PropertyType)}({type.FullName}.{propertyInfo.Name});"),
                    new CodeSnippetStatement($"\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"get_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static,
                    temp.ToArray());
            }

            if (propertyInfo.GetMethod != null && propertyInfo.GetMethod.IsPublic)
            {
                temp.Clear();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tvar value = L.{GetCheckString(propertyInfo.PropertyType)}(1);"),
                    new CodeSnippetStatement($"\t\t\t{type.FullName}.{propertyInfo.Name} = value;"),
                    new CodeSnippetStatement($"\t\t\treturn 0;")
                });

                gener.AddMemberMethod(typeof(int), $"set_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static,
                    temp.ToArray());
            }
        }

        private void GenRegStaticFunction(CodeGener gener, Type type, MethodInfo[] methodGroup)
        {
            var temp = new List<CodeStatement>();

            int count = 0;
            foreach (var methodInfo in methodGroup)
            {
                var args = methodInfo.GetParameters();

                //检查类型的方法
                StringBuilder checkStringBuilder = new StringBuilder();
                checkStringBuilder.Append($"L.CheckNum({args.Length})");
                if (args.Length > 0)
                {
                    checkStringBuilder.Append($" && L.CheckType<");
                    StringBuilder typeArgs = new StringBuilder();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i != 0)
                        {
                            typeArgs.Append(", ");
                        }
                        typeArgs.Append(args[i].ParameterType.FullName);
                    }
                    checkStringBuilder.Append(typeArgs);
                    checkStringBuilder.Append($">(1)");
                }

                if (count == 0)
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\tif({checkStringBuilder})\n\t\t\t{{"));
                }
                else
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\telse if({checkStringBuilder})\n\t\t\t{{"));
                }

                for (int i = 1; i <= args.Length; i++)
                {
                    var paramInfo = args[i - 1];
                    temp.Add(new CodeSnippetStatement($"\t\t\t\tvar arg{i} = L.{GetCheckString(paramInfo.ParameterType)}({i});"));
                }

                var paramBuilder = new StringBuilder();
                for (int i = 1; i <= args.Length; i++)
                {
                    if (i != 1)
                    {
                        paramBuilder.Append(", ");
                    }
                    paramBuilder.Append($"arg{i}");
                }

                if (methodInfo.ReturnType == typeof(void))
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\t\t{type.FullName}.{methodInfo.Name}({paramBuilder});"));
                    temp.Add(new CodeSnippetStatement("\t\t\t\treturn 0;"));
                }
                else
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\t\tvar result = {type.FullName}.{methodInfo.Name}({paramBuilder});"));
                    temp.Add(new CodeSnippetStatement($"\t\t\t\tL.{GetPushString(methodInfo.ReturnType)}(result);"));
                    temp.Add(new CodeSnippetStatement("\t\t\t\treturn 1;"));
                }

                temp.Add(new CodeSnippetStatement($"\t\t\t}}"));
                count++;
            }

            temp.Add(new CodeSnippetStatement("\t\t\tL.L_Error(\"call function args is error\");"));
            temp.Add(new CodeSnippetStatement("\t\t\treturn 1;"));

            gener.AddMemberMethod(typeof(int), methodGroup[0].Name,
                new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static,
                temp.ToArray());
        }

        private void GenRegMemberField(CodeGener gener, Type type, FieldInfo fieldInfo)
        {
            var temp = new List<CodeStatement>();
            temp.AddRange(new List<CodeStatement>()
            {
                new CodeSnippetStatement($"\t\t\tvar obj = ({type.FullName}) L.ToObject(1);"),
                new CodeSnippetStatement($"\t\t\tL.{GetPushString(fieldInfo.FieldType)}(obj.{fieldInfo.Name});"),
                new CodeSnippetStatement($"\t\t\treturn 1;")
            });

            gener.AddMemberMethod(typeof(int), $"get_{fieldInfo.Name}",
                new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());

            temp.Clear();
            temp.AddRange(new List<CodeStatement>()
            {
                new CodeSnippetStatement($"\t\t\tvar obj = ({type.FullName}) L.ToObject(1);"),
                new CodeSnippetStatement($"\t\t\tvar value = L.{GetCheckString(fieldInfo.FieldType)}(2);"),
                new CodeSnippetStatement($"\t\t\tobj.{fieldInfo.Name} = value;"),
                new CodeSnippetStatement($"\t\t\treturn 0;")
            });

            gener.AddMemberMethod(typeof(int), $"set_{fieldInfo.Name}",
                new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static, temp.ToArray());
        }

        private void GenRegMemberProperty(CodeGener gener, Type type, PropertyInfo propertyInfo)
        {
            var temp = new List<CodeStatement>();
            if (propertyInfo.GetMethod != null && propertyInfo.GetMethod.IsPublic)
            {
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tvar obj = ({type.FullName}) L.ToObject(1);"),
                    new CodeSnippetStatement($"\t\t\tL.{GetPushString(propertyInfo.PropertyType)}(obj.{propertyInfo.Name});"),
                    new CodeSnippetStatement($"\t\t\treturn 1;")
                });

                gener.AddMemberMethod(typeof(int), $"get_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static,
                    temp.ToArray());
            }

            if (propertyInfo.GetMethod != null && propertyInfo.GetMethod.IsPublic)
            {
                temp.Clear();
                temp.AddRange(new List<CodeStatement>()
                {
                    new CodeSnippetStatement($"\t\t\tvar obj = ({type.FullName}) L.ToObject(1);"),
                    new CodeSnippetStatement($"\t\t\tvar value = L.{GetCheckString(propertyInfo.PropertyType)}(2);"),
                    new CodeSnippetStatement($"\t\t\tobj.{propertyInfo.Name} = value;"),
                    new CodeSnippetStatement($"\t\t\treturn 0;")
                });

                gener.AddMemberMethod(typeof(int), $"set_{propertyInfo.Name}",
                    new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static,
                    temp.ToArray());
            }
        }

        private void GenRegMemberFunction(CodeGener gener, Type type, MethodInfo[] methodGroup)
        {
            var temp = new List<CodeStatement>();

            int count = 0;
            foreach (var methodInfo in methodGroup)
            {
                var args = methodInfo.GetParameters();

                StringBuilder checkStringBuilder = new StringBuilder();
                checkStringBuilder.Append($"L.CheckNum({args.Length + 1})");
                if (args.Length > 0)
                {
                    checkStringBuilder.Append($" && L.CheckType<");
                    StringBuilder typeArgs = new StringBuilder();
                    typeArgs.Append(type.FullName);
                    for (int i = 0; i < args.Length; i++)
                    {
                        typeArgs.Append(", ");
                        typeArgs.Append(args[i].ParameterType.FullName);
                    }
                    checkStringBuilder.Append(typeArgs);
                    checkStringBuilder.Append($">(1)");
                }

                if (count == 0)
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\tif({checkStringBuilder})\n\t\t\t{{"));
                }
                else
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\telse if({checkStringBuilder})\n\t\t\t{{"));
                }

                temp.Add(new CodeSnippetStatement($"\t\t\t\tvar obj = ({type.FullName}) L.ToObject(1);"));
                for (int i = 1; i <= args.Length; i++)
                {
                    var paramInfo = args[i - 1];
                    temp.Add(new CodeSnippetStatement(
                        $"\t\t\t\tvar arg{i} = L.{GetCheckString(paramInfo.ParameterType)}({i + 1});"));
                }

                var paramBuilder = new StringBuilder();
                for (int i = 1; i <= args.Length; i++)
                {
                    var paramInfo = args[i - 1];
                    if (i != 1)
                    {
                        paramBuilder.Append(", ");
                    }
                    paramBuilder.Append($"arg{i}");
                }

                if (methodInfo.ReturnType == typeof(void))
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\t\tobj.{methodInfo.Name}({paramBuilder});"));
                    temp.Add(new CodeSnippetStatement("\t\t\t\treturn 0;"));
                }
                else
                {
                    temp.Add(new CodeSnippetStatement($"\t\t\t\tvar result = obj.{methodInfo.Name}({paramBuilder});"));
                    temp.Add(new CodeSnippetStatement($"\t\t\t\tL.{GetPushString(methodInfo.ReturnType)}(result);"));
                    temp.Add(new CodeSnippetStatement("\t\t\t\treturn 1;"));
                }

                temp.Add(new CodeSnippetStatement($"\t\t\t}}"));
                count++;
            }

            temp.Add(new CodeSnippetStatement("\t\t\tL.L_Error(\"call function args is error\");"));
            temp.Add(new CodeSnippetStatement("\t\t\treturn 1;"));

            gener.AddMemberMethod(typeof(int), methodGroup[0].Name,
                new Dictionary<string, Type>() { { "L", typeof(ILuaState) } }, MemberAttributes.Private | MemberAttributes.Static,
                temp.ToArray());
        }

        #endregion

        #region Util

        /// <summary>
        /// 获取Push方法对应的String
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string GetPushString(Type type)
        {
            return $"PushValue<{type.FullName}>";
        }

        /// <summary>
        /// 获取Check方法对应的String
        /// </summary>
        /// <returns></returns>
        private string GetCheckString(Type type)
        {
            return $"CheckValue<{type.FullName}>";
        }


        private bool IsObsolete(MemberInfo mb)
        {
            object[] attrs = mb.GetCustomAttributes(true);

            for (int j = 0; j < attrs.Length; j++)
            {
                Type t = attrs[j].GetType();

                if (t == typeof(System.ObsoleteAttribute) || t == typeof(NoToLuaAttribute) ||
                    t.Name == "MonoNotSupportedAttribute" || t.Name == "MonoTODOAttribute") // || t.ToString() == "UnityEngine.WrapperlessIcall")
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #endregion

        private string GetClassFileName(Type type)
        {
            if (type == null)
                return null;
            var className = type.FullName?.Replace(".", "_").Replace("+", "_") + "Wrap";
            return className;
        }

    }
}