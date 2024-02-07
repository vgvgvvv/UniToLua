using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniLua;

namespace UniToLua
{
    public interface IArg
    {
        int count
        { get; }
        T Get<T>(int index);
        void PushLua(ILuaState lua);
        void PushLuaTable(ILuaState lua);
    }

    public struct Arg0 : IArg
    {
        public int count
        { get { return 0; } }

        public T Get<T>(int index)
        {
            return default(T);
        }

        public void PushLua(ILuaState lua)
        { }

        public void PushLuaTable(ILuaState lua)
        {
            lua.NewTable();
        }
    }

    public struct Arg1<A1> : IArg
    {
        public A1 a1;

        public Arg1(A1 a1)
        {
            this.a1 = a1;
        }

        public int count
        { get { return 1; } }

        public T Get<T>(int index)
        {
            switch (index)
            {
                case 0:
                    return (T)(object)a1;
            }
            return default(T);
        }

        public void PushLua(ILuaState lua)
        {
            lua.PushAny(a1);
        }

        public void PushLuaTable(ILuaState lua)
        {
            lua.NewTable();
            lua.PushTableItem(1, a1);
        }
    }

    public struct Arg2<A1, A2> : IArg
    {
        public A1 a1;
        public A2 a2;

        public Arg2(A1 a1, A2 a2)
        {
            this.a1 = a1;
            this.a2 = a2;
        }

        public int count
        { get { return 2; } }

        public T Get<T>(int index)
        {
            switch (index)
            {
                case 0:
                    return (T)(object)a1;
                case 1:
                    return (T)(object)a2;
            }
            return default(T);
        }

        public void PushLua(ILuaState lua)
        {
            lua.PushAny(a1);
            lua.PushAny(a2);
        }

        public void PushLuaTable(ILuaState lua)
        {
            lua.NewTable();
            lua.PushTableItem(1, a1);
            lua.PushTableItem(2, a2);
        }
    }

    public struct Arg3<A1, A2, A3> : IArg
    {
        public A1 a1;
        public A2 a2;
        public A3 a3;

        public Arg3(A1 a1, A2 a2, A3 a3)
        {
            this.a1 = a1;
            this.a2 = a2;
            this.a3 = a3;
        }

        public int count
        { get { return 3; } }

        public T Get<T>(int index)
        {
            switch (index)
            {
                case 0:
                    return (T)(object)a1;
                case 1:
                    return (T)(object)a2;
                case 2:
                    return (T)(object)a3;
            }
            return default(T);
        }

        public void PushLua(ILuaState lua)
        {
            lua.PushAny(a1);
            lua.PushAny(a2);
            lua.PushAny(a3);
        }

        public void PushLuaTable(ILuaState lua)
        {
            lua.NewTable();
            lua.PushTableItem(1, a1);
            lua.PushTableItem(2, a2);
            lua.PushTableItem(3, a3);
        }
    }

    public struct Arg4<A1, A2, A3, A4> : IArg
    {
        public A1 a1;
        public A2 a2;
        public A3 a3;
        public A4 a4;

        public Arg4(A1 a1, A2 a2, A3 a3, A4 a4)
        {
            this.a1 = a1;
            this.a2 = a2;
            this.a3 = a3;
            this.a4 = a4;
        }

        public int count
        { get { return 4; } }

        public T Get<T>(int index)
        {
            switch (index)
            {
                case 0:
                    return (T)(object)a1;
                case 1:
                    return (T)(object)a2;
                case 2:
                    return (T)(object)a3;
                case 3:
                    return (T)(object)a4;
            }
            return default(T);
        }

        public void PushLua(ILuaState lua)
        {
            lua.PushAny(a1);
            lua.PushAny(a2);
            lua.PushAny(a3);
            lua.PushAny(a4);
        }

        public void PushLuaTable(ILuaState lua)
        {
            lua.NewTable();
            lua.PushTableItem(1, a1);
            lua.PushTableItem(2, a2);
            lua.PushTableItem(3, a3);
            lua.PushTableItem(4, a4);
        }
    }

    public struct Arg5<A1, A2, A3, A4, A5> : IArg
    {
        public A1 a1;
        public A2 a2;
        public A3 a3;
        public A4 a4;
        public A5 a5;

        public Arg5(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5)
        {
            this.a1 = a1;
            this.a2 = a2;
            this.a3 = a3;
            this.a4 = a4;
            this.a5 = a5;
        }

        public int count
        { get { return 5; } }

        public T Get<T>(int index)
        {
            switch (index)
            {
                case 0:
                    return (T)(object)a1;
                case 1:
                    return (T)(object)a2;
                case 2:
                    return (T)(object)a3;
                case 3:
                    return (T)(object)a4;
                case 4:
                    return (T)(object)a5;
            }
            return default(T);
        }

        public void PushLua(ILuaState lua)
        {
            lua.PushAny(a1);
            lua.PushAny(a2);
            lua.PushAny(a3);
            lua.PushAny(a4);
            lua.PushAny(a5);
        }

        public void PushLuaTable(ILuaState lua)
        {
            lua.NewTable();
            lua.PushTableItem(1, a1);
            lua.PushTableItem(2, a2);
            lua.PushTableItem(3, a3);
            lua.PushTableItem(4, a4);
            lua.PushTableItem(5, a5);
        }
    }

    public struct Arg6<A1, A2, A3, A4, A5, A6> : IArg
    {
        public A1 a1;
        public A2 a2;
        public A3 a3;
        public A4 a4;
        public A5 a5;
        public A6 a6;

        public Arg6(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6)
        {
            this.a1 = a1;
            this.a2 = a2;
            this.a3 = a3;
            this.a4 = a4;
            this.a5 = a5;
            this.a6 = a6;
        }

        public int count
        { get { return 6; } }

        public T Get<T>(int index)
        {
            switch (index)
            {
                case 0:
                    return (T)(object)a1;
                case 1:
                    return (T)(object)a2;
                case 2:
                    return (T)(object)a3;
                case 3:
                    return (T)(object)a4;
                case 4:
                    return (T)(object)a5;
                case 5:
                    return (T)(object)a6;
            }
            return default(T);
        }

        public void PushLua(ILuaState lua)
        {
            lua.PushAny(a1);
            lua.PushAny(a2);
            lua.PushAny(a3);
            lua.PushAny(a4);
            lua.PushAny(a5);
            lua.PushAny(a6);
        }

        public void PushLuaTable(ILuaState lua)
        {
            lua.NewTable();
            lua.PushTableItem(1, a1);
            lua.PushTableItem(2, a2);
            lua.PushTableItem(3, a3);
            lua.PushTableItem(4, a4);
            lua.PushTableItem(5, a5);
            lua.PushTableItem(6, a6);
        }
    }

    public struct Arg7<A1, A2, A3, A4, A5, A6, A7> : IArg
    {
        public A1 a1;
        public A2 a2;
        public A3 a3;
        public A4 a4;
        public A5 a5;
        public A6 a6;
        public A7 a7;

        public Arg7(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7)
        {
            this.a1 = a1;
            this.a2 = a2;
            this.a3 = a3;
            this.a4 = a4;
            this.a5 = a5;
            this.a6 = a6;
            this.a7 = a7;
        }

        public int count
        { get { return 7; } }

        public T Get<T>(int index)
        {
            switch (index)
            {
                case 0:
                    return (T)(object)a1;
                case 1:
                    return (T)(object)a2;
                case 2:
                    return (T)(object)a3;
                case 3:
                    return (T)(object)a4;
                case 4:
                    return (T)(object)a5;
                case 5:
                    return (T)(object)a6;
                case 6:
                    return (T)(object)a7;
            }
            return default(T);
        }

        public void PushLua(ILuaState lua)
        {
            lua.PushAny(a1);
            lua.PushAny(a2);
            lua.PushAny(a3);
            lua.PushAny(a4);
            lua.PushAny(a5);
            lua.PushAny(a6);
            lua.PushAny(a7);
        }

        public void PushLuaTable(ILuaState lua)
        {
            lua.NewTable();
            lua.PushTableItem(1, a1);
            lua.PushTableItem(2, a2);
            lua.PushTableItem(3, a3);
            lua.PushTableItem(4, a4);
            lua.PushTableItem(5, a5);
            lua.PushTableItem(6, a6);
            lua.PushTableItem(7, a7);
        }
    }

    public struct Arg8<A1, A2, A3, A4, A5, A6, A7, A8> : IArg
    {
        public A1 a1;
        public A2 a2;
        public A3 a3;
        public A4 a4;
        public A5 a5;
        public A6 a6;
        public A7 a7;
        public A8 a8;

        public Arg8(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7, A8 a8)
        {
            this.a1 = a1;
            this.a2 = a2;
            this.a3 = a3;
            this.a4 = a4;
            this.a5 = a5;
            this.a6 = a6;
            this.a7 = a7;
            this.a8 = a8;
        }

        public int count
        { get { return 8; } }

        public T Get<T>(int index)
        {
            switch (index)
            {
                case 0:
                    return (T)(object)a1;
                case 1:
                    return (T)(object)a2;
                case 2:
                    return (T)(object)a3;
                case 3:
                    return (T)(object)a4;
                case 4:
                    return (T)(object)a5;
                case 5:
                    return (T)(object)a6;
                case 6:
                    return (T)(object)a7;
                case 7:
                    return (T)(object)a8;
            }
            return default(T);
        }

        public void PushLua(ILuaState lua)
        {
            lua.PushAny(a1);
            lua.PushAny(a2);
            lua.PushAny(a3);
            lua.PushAny(a4);
            lua.PushAny(a5);
            lua.PushAny(a6);
            lua.PushAny(a7);
            lua.PushAny(a8);
        }

        public void PushLuaTable(ILuaState lua)
        {
            lua.NewTable();
            lua.PushTableItem(1, a1);
            lua.PushTableItem(2, a2);
            lua.PushTableItem(3, a3);
            lua.PushTableItem(4, a4);
            lua.PushTableItem(5, a5);
            lua.PushTableItem(6, a6);
            lua.PushTableItem(7, a7);
            lua.PushTableItem(8, a8);
        }
    }

    public struct Arg9<A1, A2, A3, A4, A5, A6, A7, A8, A9> : IArg
    {
        public A1 a1;
        public A2 a2;
        public A3 a3;
        public A4 a4;
        public A5 a5;
        public A6 a6;
        public A7 a7;
        public A8 a8;
        public A9 a9;

        public Arg9(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7, A8 a8, A9 a9)
        {
            this.a1 = a1;
            this.a2 = a2;
            this.a3 = a3;
            this.a4 = a4;
            this.a5 = a5;
            this.a6 = a6;
            this.a7 = a7;
            this.a8 = a8;
            this.a9 = a9;
        }

        public int count
        { get { return 9; } }

        public T Get<T>(int index)
        {
            switch (index)
            {
                case 0:
                    return (T)(object)a1;
                case 1:
                    return (T)(object)a2;
                case 2:
                    return (T)(object)a3;
                case 3:
                    return (T)(object)a4;
                case 4:
                    return (T)(object)a5;
                case 5:
                    return (T)(object)a6;
                case 6:
                    return (T)(object)a7;
                case 7:
                    return (T)(object)a8;
                case 8:
                    return (T)(object)a9;
            }
            return default(T);
        }

        public void PushLua(ILuaState lua)
        {
            lua.PushAny(a1);
            lua.PushAny(a2);
            lua.PushAny(a3);
            lua.PushAny(a4);
            lua.PushAny(a5);
            lua.PushAny(a6);
            lua.PushAny(a7);
            lua.PushAny(a8);
            lua.PushAny(a9);
        }

        public void PushLuaTable(ILuaState lua)
        {
            lua.NewTable();
            lua.PushTableItem(1, a1);
            lua.PushTableItem(2, a2);
            lua.PushTableItem(3, a3);
            lua.PushTableItem(4, a4);
            lua.PushTableItem(5, a5);
            lua.PushTableItem(6, a6);
            lua.PushTableItem(7, a7);
            lua.PushTableItem(8, a8);
            lua.PushTableItem(9, a9);
        }
    }

    public struct ArgN<T> : IArg
    {
        public T[] an;

        public ArgN(params T[] an)
        {
            this.an = an;
        }

        public int count
        { get { return an != null ? an.Length : 0; } }

        public T1 Get<T1>(int index)
        {
            if (an != null && index >= 0 && index < an.Length)
                return (T1)(object)(an[index]);
            return default(T1);
        }

        public void PushLua(ILuaState lua)
        {
            if (an != null)
            {
                foreach (var a in an)
                {
                    lua.PushAny(a);
                }
            }
        }

        public void PushLuaTable(ILuaState lua)
        {
            lua.NewTable();
            if (an != null)
            {
                for (int i = 0; i < an.Length; ++i)
                {
                    lua.PushTableItem(i + 1, an[i]);
                }
            }
        }
    }

}
