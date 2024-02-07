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

        public static void GenWrapper(List<Type> targetTypeList, string outputPath)
        {

            foreach (var type in targetTypeList)
            {
                if (type.IsEnum)
                {
                    GenEnum(type, outputPath);
                }
                else if (type.IsInterface)
                {
                    continue;
                }
                else if (type.IsSealed && type.IsAbstract)
                {
                    GenStaticLib(type, outputPath);
                }
                else
                {
                    var extensionMethods = FindExtensionMethods(type, targetTypeList, out var extensionTypes);
                    
                    GenClass(type, extensionMethods, extensionTypes, outputPath);
                }
            }
        }

    }
}