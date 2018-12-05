using System;

namespace UniLua
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Constructor)]
    public class NoToLuaAttribute : Attribute
    {
        
    }
}