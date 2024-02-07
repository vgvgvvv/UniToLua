using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UniLua;

namespace UniToLua.Utility
{
    public class JsonLuaHelper
    {
        public static void PushJObject(IToLua L, JObject jObject)
        {
            var dict = jObject.ToObject<Dictionary<string, object>>();
            L.PushAny(dict);
        }
    }
}