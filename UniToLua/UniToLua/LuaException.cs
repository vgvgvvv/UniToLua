using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UniToLua
{
    public class LuaException : Exception
    {
        public static Exception luaStack = null;
        public static IntPtr L = IntPtr.Zero;

        public override string StackTrace
        {
            get
            {
                return _stack;
            }
        }

        protected string _stack = string.Empty;

        public LuaException(string msg, Exception e = null, int skip = 1)
            : base(msg)
        {
            if (e != null)
            {
                if (e is LuaException)
                {
                    _stack = e.StackTrace;
                }
                else
                {
                    StackTrace trace = new StackTrace(e, true);
                    StringBuilder sb = new StringBuilder();
                    ExtractFormattedStackTrace(trace, sb);
                    StackTrace self = new StackTrace(skip, true);
                    ExtractFormattedStackTrace(self, sb, trace);
                    _stack = sb.ToString();
                }
            }
            else
            {
                StackTrace self = new StackTrace(skip, true);
                StringBuilder sb = new StringBuilder();
                ExtractFormattedStackTrace(self, sb);
                _stack = sb.ToString();
            }
        }

        public static Exception GetLastError()
        {
            Exception last = luaStack;
            luaStack = null;
            return last;
        }

        public static void ExtractFormattedStackTrace(StackTrace trace, StringBuilder sb, StackTrace skip = null)
        {
            int begin = 0;

            if (skip != null && skip.FrameCount > 0)
            {
                MethodBase m0 = skip.GetFrame(skip.FrameCount - 1).GetMethod();

                for (int i = 0; i < trace.FrameCount; i++)
                {
                    StackFrame frame = trace.GetFrame(i);
                    MethodBase method = frame.GetMethod();

                    if (method == m0)
                    {
                        begin = i + 1;
                        break;
                    }
                }

                sb.AppendLine();
            }

            for (int i = begin; i < trace.FrameCount; i++)
            {
                StackFrame frame = trace.GetFrame(i);
                MethodBase method = frame.GetMethod();

                if (method == null || method.DeclaringType == null)
                {
                    continue;
                }

                Type declaringType = method.DeclaringType;
                string str = declaringType.Namespace;

                if ((str != null) && (str.Length != 0))
                {
                    sb.Append(str);
                    sb.Append(".");
                }

                sb.Append(declaringType.Name);
                sb.Append(":");
                sb.Append(method.Name);
                sb.Append("(");
                int index = 0;
                ParameterInfo[] parameters = method.GetParameters();
                bool flag = true;

                while (index < parameters.Length)
                {
                    if (!flag)
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        flag = false;
                    }

                    sb.Append(parameters[index].ParameterType.Name);
                    index++;
                }

                sb.Append(")");
                string fileName = frame.GetFileName();

                if (fileName != null)
                {
                    fileName = fileName.Replace('\\', '/');
                    sb.Append(" (at ");

                    sb.Append(fileName);
                    sb.Append(":");
                    sb.Append(frame.GetFileLineNumber().ToString());
                    sb.Append(")");
                }

                if (i != trace.FrameCount - 1)
                {
                    sb.Append("\n");
                }
            }
        }

    }
}
