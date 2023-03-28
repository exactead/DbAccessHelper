using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DbAccessHelper.Internal
{
    internal sealed class DynamicRecord : DynamicObject
    {
        IDictionary<string, object> dictionary;

        public DynamicRecord(IDictionary<string, object> record) => this.dictionary = record;

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            // フィールド名称で来た場合
            result = dictionary[binder.Name];
            if (result.Equals(DBNull.Value)) result = null;
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            // インデクサで来た場合
            var index = indexes[0];
            result = (index is string) ? dictionary[(string)index] :
                (index is int) ? dictionary.ElementAtOrDefault((int)index).Value : null;
            if (result.Equals(DBNull.Value)) result = null;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            // フィールド名称をそのまま使えるように設定
            foreach (var item in dictionary)
            {
                yield return item.Key;
            }
        }
    }
}
