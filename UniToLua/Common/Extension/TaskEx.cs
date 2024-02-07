using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Extension
{
    public class TaskEx
    {
        public static CancellationTokenSource CreateCancelableTask(Action action, out Task task)
        {
            var ts = new CancellationTokenSource();
            CancellationToken ct = ts.Token;
            task = new Task(action, ct);
            return ts;
        }

        public static CancellationTokenSource CreateCancelableTask<T>(Func<T> func, out Task<T> task)
        {
            var ts = new CancellationTokenSource();
            CancellationToken ct = ts.Token;
            task = new Task<T>(func, ct);
            return ts;
        }

    }
}
