using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class EnumEx
    {
        /// <summary>
        /// int转enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static T GetValue<T>(int id)
        {
            return (T)Enum.ToObject(typeof(T), id);
        }

        /// <summary>
        /// string转enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetValue<T>(string name)
        {
            return (T)Enum.Parse(typeof(T), name);
        }

        /// <summary>
        /// string转enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGetValue<T>(string name, out T result, bool ignoreCase = true) where T : struct
        {
            return Enum.TryParse(name, false, out result);
        }
    }
}
