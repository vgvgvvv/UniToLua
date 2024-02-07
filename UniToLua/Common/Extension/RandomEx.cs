using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class RandomEx
    {
        private static Random r = new Random();

        /// <summary>
        /// 创建随机
        /// </summary>
        /// <returns></returns>
        public static System.Random CreateRandom()
        {
            long ticks = DateTime.Now.Ticks;
            return new System.Random(((int)(((ulong)ticks) & 0xffffffffL)) | ((int)(ticks >> 0x20)));
        }

        public static int GetRandomInt()
        {
            return r.Next();
        }
    }

}
