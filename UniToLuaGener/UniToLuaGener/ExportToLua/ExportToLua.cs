using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UniToLua.Common;
using UniLua;

namespace UniToLuaGener
{
    public class TypeBinder
    {
        public Type TargetType;
        public List<Type> ExtensionTypes;
    }

    public static partial class ExportToLua
    {
        /// <summary>
        /// error CS0306: 类型“ReadOnlySpan<char>”不能用作类型参数
        /// 部分类型不能用作泛型
        /// </summary>
        private static List<Type> IgnoreParamTypes = new List<Type>()
        {
#if DOTNET_CORE
            typeof(System.ReadOnlySpan<>)
#endif
        };

        public static void GenAll(string dllPath, string outputPath)
        {
            var target = Assembly.LoadFile(Path.GetFullPath(dllPath));
            
            // 以双下划线开头的类往往有特殊作用
            List<Type> targetTypeList = GetTargetType(target);
            targetTypeList = FilterSaveTypes(targetTypeList);
            GenWrapper(targetTypeList, outputPath);
            GenBinder(targetTypeList, outputPath);
            GenDelegateFactory(targetTypeList, outputPath);
        }

        public static void GenWithTypeToExport(string dllPath, List<Type> typeToExport, string outputPath)
        {
            if (typeToExport == null)
            {
                GenAll(dllPath, outputPath);
                return;
            }

            List<Type> targetTypeList = new List<Type>();
            foreach (var type in typeToExport)
            {
                if (type != null)
                {
                    targetTypeList.Add(type);
                }
            }

            targetTypeList = FilterSaveTypes(targetTypeList);
            
            GenWrapper(targetTypeList, outputPath);
            GenBinder(targetTypeList, outputPath);
            GenDelegateFactory(targetTypeList, outputPath);
        }

        private static List<Type> GetTargetType(Assembly target)
        {
            var types = target.GetTypes().Where((t) =>
            {
                var toluaAttr = t.GetCustomAttribute<ToLuaAttribute>();
                return toluaAttr != null;
            });
            return types.ToList();
        }

        private static List<Type> FilterSaveTypes(List<Type> sourceTypes)
        {
            return sourceTypes
                .Where(t => 
                    !t.Name.Split('.').Any(s => s.StartsWith("__")) &&
                    !t.IsNestedPrivate)
                .ToList();;
        }

    }
}