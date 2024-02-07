using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
    public static class JsonEx
    {
        public readonly static HashSet<JsonConverter> CustomJsonConverter = new HashSet<JsonConverter>();
        
        public static Dictionary<string, object> ToDictionary(this JObject jObject)
        {
            Dictionary<string, object> result = jObject.ToObject<Dictionary<string, object>>();
            var keys = result.Keys.ToList();
            foreach (var key in keys)
            {
                if (result[key] is JObject jObjValue)
                {
                    result[key] = jObjValue.ToDictionary();
                }
            }

            return result;
        }
    }
}