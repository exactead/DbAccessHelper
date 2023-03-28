using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DbAccessHelper.Extensions
{
    public static class Extensions
    {
        public static IEnumerable<TData> RawDataMapping<TRaw, TData>(this IEnumerable<TRaw> source)
        {
            // Invoke時、out値はTDataとなるので、TRawデータを引数としてreturn
            return source.Select(v => Internal.ExpressionsEx.CreateInstances<TRaw, TData>().Invoke(v));
        }
    }
}
